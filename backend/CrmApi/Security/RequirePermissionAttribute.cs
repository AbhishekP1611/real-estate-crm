using System.Security.Claims;
using CrmApi.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace CrmApi.Security;

public enum PermAction { View, Create, Edit, Delete }

/// <summary>
/// Server-side authority check. The frontend hides menus, but every
/// protected endpoint re-checks the role's permission on the module here,
/// so hiding the UI is never the only thing standing in the way.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequirePermissionAttribute(string moduleKey, PermAction action)
    : Attribute, IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;
        if (user.Identity?.IsAuthenticated != true)
        {
            context.Result = new UnauthorizedObjectResult(new { message = "Not authenticated." });
            return;
        }

        var roleIdClaim = user.FindFirst("roleId")?.Value;
        if (!int.TryParse(roleIdClaim, out var roleId))
        {
            context.Result = new ForbidResult();
            return;
        }

        var db = context.HttpContext.RequestServices.GetRequiredService<CrmDbContext>();

        var perm = await db.RolePermissions
            .AsNoTracking()
            .Where(p => p.RoleId == roleId && p.Module!.ModuleKey == moduleKey)
            .Select(p => new { p.CanView, p.CanCreate, p.CanEdit, p.CanDelete })
            .FirstOrDefaultAsync();

        var allowed = action switch
        {
            PermAction.View => perm?.CanView,
            PermAction.Create => perm?.CanCreate,
            PermAction.Edit => perm?.CanEdit,
            PermAction.Delete => perm?.CanDelete,
            _ => false
        } ?? false;

        if (!allowed)
        {
            context.Result = new ObjectResult(new
            {
                message = $"You do not have permission to {action.ToString().ToLower()} in {moduleKey}."
            })
            { StatusCode = StatusCodes.Status403Forbidden };
        }
    }
}

public static class ClaimsPrincipalExtensions
{
    public static int GetUserId(this ClaimsPrincipal principal)
        => int.TryParse(principal.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : 0;

    public static int GetRoleId(this ClaimsPrincipal principal)
        => int.TryParse(principal.FindFirst("roleId")?.Value, out var id) ? id : 0;
}
