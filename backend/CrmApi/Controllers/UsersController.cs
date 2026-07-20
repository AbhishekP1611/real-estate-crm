using CrmApi.Data;
using CrmApi.Models;
using CrmApi.Models.Dtos;
using CrmApi.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CrmApi.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController(CrmDbContext db) : ControllerBase
{
    [HttpGet]
    [RequirePermission("users", PermAction.View)]
    public async Task<ActionResult<PagedResult<UserDto>>> Search(
        [FromQuery] string? search = null,
        [FromQuery] int? roleId = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);

        var q = db.Users.AsNoTracking().Include(u => u.Role).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            q = q.Where(u => u.FullName.Contains(s) || u.Username.Contains(s)
                          || u.Email.Contains(s) || (u.Phone != null && u.Phone.Contains(s)));
        }
        if (roleId.HasValue) q = q.Where(u => u.RoleId == roleId);
        if (isActive.HasValue) q = q.Where(u => u.IsActive == isActive);

        var total = await q.CountAsync();
        var items = await q.OrderBy(u => u.FullName)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(u => MapUser(u)).ToListAsync();

        return Ok(new PagedResult<UserDto> { Items = items, Page = page, PageSize = pageSize, TotalCount = total });
    }

    [HttpGet("{id:int}")]
    [RequirePermission("users", PermAction.View)]
    public async Task<ActionResult<UserDto>> GetById(int id)
    {
        var u = await db.Users.AsNoTracking().Include(x => x.Role)
            .Where(x => x.UserId == id).Select(x => MapUser(x)).FirstOrDefaultAsync();
        return u is null ? NotFound(new { message = $"User {id} was not found." }) : Ok(u);
    }

    [HttpPost]
    [RequirePermission("users", PermAction.Create)]
    public async Task<ActionResult<UserDto>> Create(CreateUserRequest req)
    {
        if (await db.Users.AnyAsync(u => u.Username == req.Username))
            return Conflict(new { message = $"Username '{req.Username}' is already taken." });
        if (await db.Users.AnyAsync(u => u.Email == req.Email))
            return Conflict(new { message = $"Email '{req.Email}' is already registered." });
        if (!await db.Roles.AnyAsync(r => r.RoleId == req.RoleId))
            return BadRequest(new { message = "The selected role does not exist." });

        var user = new User
        {
            FullName = req.FullName.Trim(),
            Email = req.Email.Trim(),
            Username = req.Username.Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password),
            Phone = req.Phone,
            RoleId = req.RoleId,
            IsActive = req.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        var dto = await db.Users.AsNoTracking().Include(x => x.Role)
            .Where(x => x.UserId == user.UserId).Select(x => MapUser(x)).FirstAsync();
        return CreatedAtAction(nameof(GetById), new { id = user.UserId }, dto);
    }

    [HttpPut("{id:int}")]
    [RequirePermission("users", PermAction.Edit)]
    public async Task<ActionResult<UserDto>> Update(int id, UpdateUserRequest req)
    {
        var user = await db.Users.FindAsync(id);
        if (user is null) return NotFound(new { message = $"User {id} was not found." });

        if (await db.Users.AnyAsync(u => u.Email == req.Email && u.UserId != id))
            return Conflict(new { message = $"Email '{req.Email}' is already registered to another user." });
        if (!await db.Roles.AnyAsync(r => r.RoleId == req.RoleId))
            return BadRequest(new { message = "The selected role does not exist." });

        // Don't let an admin lock themselves out of the system.
        if (user.UserId == User.GetUserId() && !req.IsActive)
            return BadRequest(new { message = "You cannot deactivate your own account." });

        user.FullName = req.FullName.Trim();
        user.Email = req.Email.Trim();
        user.Phone = req.Phone;
        user.RoleId = req.RoleId;
        user.IsActive = req.IsActive;
        user.UpdatedAt = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(req.NewPassword))
        {
            if (req.NewPassword.Length < 6)
                return BadRequest(new { message = "Password must be at least 6 characters." });
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.NewPassword);
        }

        await db.SaveChangesAsync();

        var dto = await db.Users.AsNoTracking().Include(x => x.Role)
            .Where(x => x.UserId == id).Select(x => MapUser(x)).FirstAsync();
        return Ok(dto);
    }

    [HttpDelete("{id:int}")]
    [RequirePermission("users", PermAction.Delete)]
    public async Task<IActionResult> Delete(int id)
    {
        var user = await db.Users.FindAsync(id);
        if (user is null) return NotFound(new { message = $"User {id} was not found." });

        if (user.UserId == User.GetUserId())
            return BadRequest(new { message = "You cannot delete your own account." });

        // Leads reference users, so deactivate rather than hard delete.
        user.IsActive = false;
        user.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return Ok(new { message = $"User '{user.FullName}' has been deactivated." });
    }

    private static UserDto MapUser(User u) => new()
    {
        UserId = u.UserId, FullName = u.FullName, Email = u.Email, Username = u.Username,
        Phone = u.Phone, RoleId = u.RoleId, RoleName = u.Role != null ? u.Role.RoleName : "",
        IsActive = u.IsActive, LastLoginAt = u.LastLoginAt, CreatedAt = u.CreatedAt
    };
}

/* ================= Roles & authority matrix ================= */

[ApiController]
[Route("api/roles")]
[Authorize]
public class RolesController(CrmDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<RoleDto>>> GetAll()
    {
        var roles = await db.Roles.AsNoTracking()
            .OrderBy(r => r.RoleId)
            .Select(r => new RoleDto
            {
                RoleId = r.RoleId, RoleName = r.RoleName, Description = r.Description,
                IsSystem = r.IsSystem, IsActive = r.IsActive,
                UserCount = db.Users.Count(u => u.RoleId == r.RoleId),
                Permissions = db.RolePermissions.Where(p => p.RoleId == r.RoleId)
                    .OrderBy(p => p.Module!.SortOrder)
                    .Select(p => new ModulePermissionDto
                    {
                        ModuleId = p.ModuleId, ModuleKey = p.Module!.ModuleKey,
                        ModuleName = p.Module.ModuleName, SortOrder = p.Module.SortOrder,
                        CanView = p.CanView, CanCreate = p.CanCreate,
                        CanEdit = p.CanEdit, CanDelete = p.CanDelete
                    }).ToList()
            }).ToListAsync();

        return Ok(roles);
    }

    [HttpGet("modules")]
    public async Task<ActionResult<List<Module>>> Modules()
        => Ok(await db.Modules.AsNoTracking().OrderBy(m => m.SortOrder).ToListAsync());

    [HttpPost]
    [RequirePermission("users", PermAction.Create)]
    public async Task<ActionResult<RoleDto>> Create(SaveRoleRequest req)
    {
        if (await db.Roles.AnyAsync(r => r.RoleName == req.RoleName))
            return Conflict(new { message = $"A role named '{req.RoleName}' already exists." });

        var role = new Role
        {
            RoleName = req.RoleName.Trim(), Description = req.Description,
            IsActive = req.IsActive, IsSystem = false, CreatedAt = DateTime.UtcNow
        };
        db.Roles.Add(role);
        await db.SaveChangesAsync();

        await SavePermissions(role.RoleId, req.Permissions);
        return Ok(new { message = $"Role '{role.RoleName}' created.", roleId = role.RoleId });
    }

    /// <summary>Update a role and rewrite its whole authority matrix.</summary>
    [HttpPut("{id:int}")]
    [RequirePermission("users", PermAction.Edit)]
    public async Task<IActionResult> Update(int id, SaveRoleRequest req)
    {
        var role = await db.Roles.FindAsync(id);
        if (role is null) return NotFound(new { message = $"Role {id} was not found." });

        if (await db.Roles.AnyAsync(r => r.RoleName == req.RoleName && r.RoleId != id))
            return Conflict(new { message = $"Another role named '{req.RoleName}' already exists." });

        if (!role.IsSystem)
        {
            role.RoleName = req.RoleName.Trim();
            role.IsActive = req.IsActive;
        }
        role.Description = req.Description;
        await db.SaveChangesAsync();

        await SavePermissions(id, req.Permissions);
        return Ok(new { message = $"Permissions for '{role.RoleName}' saved." });
    }

    [HttpDelete("{id:int}")]
    [RequirePermission("users", PermAction.Delete)]
    public async Task<IActionResult> Delete(int id)
    {
        var role = await db.Roles.FindAsync(id);
        if (role is null) return NotFound(new { message = $"Role {id} was not found." });
        if (role.IsSystem) return BadRequest(new { message = $"'{role.RoleName}' is a system role and cannot be deleted." });

        var inUse = await db.Users.CountAsync(u => u.RoleId == id);
        if (inUse > 0)
            return BadRequest(new { message = $"{inUse} user(s) are assigned to this role. Reassign them first." });

        db.Roles.Remove(role);
        await db.SaveChangesAsync();
        return Ok(new { message = $"Role '{role.RoleName}' deleted." });
    }

    private async Task SavePermissions(int roleId, List<PermissionInput> perms)
    {
        var existing = await db.RolePermissions.Where(p => p.RoleId == roleId).ToListAsync();
        db.RolePermissions.RemoveRange(existing);
        await db.SaveChangesAsync();

        var validModuleIds = await db.Modules.Select(m => m.ModuleId).ToListAsync();

        var rows = perms
            .Where(p => validModuleIds.Contains(p.ModuleId))
            .Select(p => new RolePermission
            {
                RoleId = roleId, ModuleId = p.ModuleId,
                // A create/edit/delete grant is meaningless without view.
                CanView = p.CanView || p.CanCreate || p.CanEdit || p.CanDelete,
                CanCreate = p.CanCreate, CanEdit = p.CanEdit, CanDelete = p.CanDelete
            });

        db.RolePermissions.AddRange(rows);
        await db.SaveChangesAsync();
    }
}

/* ================= Lookups for dropdowns ================= */

[ApiController]
[Route("api/lookups")]
[Authorize]
public class LookupsController(CrmDbContext db) : ControllerBase
{
    [HttpGet("sources")]
    public async Task<ActionResult<List<LookupDto>>> Sources()
        => Ok(await db.Sources.AsNoTracking().Where(s => s.IsActive)
            .OrderBy(s => s.SourceName)
            .Select(s => new LookupDto { Id = s.SourceId, Name = s.SourceName }).ToListAsync());

    [HttpGet("projects")]
    public async Task<ActionResult<List<LookupDto>>> Projects()
        => Ok(await db.Projects.AsNoTracking().Where(p => p.IsActive)
            .OrderBy(p => p.ProjectName)
            .Select(p => new LookupDto { Id = p.ProjectId, Name = p.ProjectName }).ToListAsync());

    [HttpGet("agents")]
    public async Task<ActionResult<List<LookupDto>>> Agents()
        => Ok(await db.Users.AsNoTracking().Where(u => u.IsActive)
            .OrderBy(u => u.FullName)
            .Select(u => new LookupDto { Id = u.UserId, Name = u.FullName }).ToListAsync());

    [HttpGet("statuses")]
    public ActionResult<List<string>> Statuses() => Ok(LeadStatus.All.ToList());

    /// <summary>
    /// Find-or-create a source by name. The lead form's combo box calls this when
    /// the user types a name that isn't in the list yet, so the master list grows
    /// as they work instead of needing a separate admin screen.
    /// Matching is case-insensitive, so "walk-in" will not duplicate "Walk-in".
    /// </summary>
    [HttpPost("sources")]
    [RequirePermission("leads", PermAction.Create)]
    public async Task<ActionResult<LookupDto>> CreateSource(SaveLookupRequest req)
    {
        var name = req.Name?.Trim();
        if (string.IsNullOrWhiteSpace(name))
            return BadRequest(new { message = "Source name is required." });
        if (name.Length > 50)
            return BadRequest(new { message = "Source name cannot exceed 50 characters." });

        var existing = await db.Sources
            .FirstOrDefaultAsync(s => s.SourceName == name);

        if (existing is not null)
        {
            // Re-activate rather than creating a duplicate.
            if (!existing.IsActive)
            {
                existing.IsActive = true;
                await db.SaveChangesAsync();
            }
            return Ok(new LookupDto { Id = existing.SourceId, Name = existing.SourceName });
        }

        var created = new Source { SourceName = name, IsActive = true };
        db.Sources.Add(created);
        await db.SaveChangesAsync();

        return Ok(new LookupDto { Id = created.SourceId, Name = created.SourceName });
    }

    /// <summary>Find-or-create a project by name. See CreateSource for the rationale.</summary>
    [HttpPost("projects")]
    [RequirePermission("leads", PermAction.Create)]
    public async Task<ActionResult<LookupDto>> CreateProject(SaveLookupRequest req)
    {
        var name = req.Name?.Trim();
        if (string.IsNullOrWhiteSpace(name))
            return BadRequest(new { message = "Project name is required." });
        if (name.Length > 100)
            return BadRequest(new { message = "Project name cannot exceed 100 characters." });

        var existing = await db.Projects
            .FirstOrDefaultAsync(p => p.ProjectName == name);

        if (existing is not null)
        {
            if (!existing.IsActive)
            {
                existing.IsActive = true;
                await db.SaveChangesAsync();
            }
            return Ok(new LookupDto { Id = existing.ProjectId, Name = existing.ProjectName });
        }

        var created = new Project
        {
            ProjectName = name,
            City = string.IsNullOrWhiteSpace(req.City) ? null : req.City.Trim(),
            IsActive = true
        };
        db.Projects.Add(created);
        await db.SaveChangesAsync();

        return Ok(new LookupDto { Id = created.ProjectId, Name = created.ProjectName });
    }
}
