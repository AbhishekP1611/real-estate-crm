using System.ComponentModel.DataAnnotations;

namespace CrmApi.Models.Dtos;

/* ---------------- Auth ---------------- */

public record LoginRequest
{
    [Required] public string Username { get; init; } = "";
    [Required] public string Password { get; init; } = "";
}

public record ChangePasswordRequest
{
    [Required] public string CurrentPassword { get; init; } = "";
    [Required, MinLength(6)] public string NewPassword { get; init; } = "";
}

public record ModulePermissionDto
{
    public int ModuleId { get; init; }
    public string ModuleKey { get; init; } = "";
    public string ModuleName { get; init; } = "";
    public int SortOrder { get; init; }
    public bool CanView { get; init; }
    public bool CanCreate { get; init; }
    public bool CanEdit { get; init; }
    public bool CanDelete { get; init; }
}

public record AuthUserDto
{
    public int UserId { get; init; }
    public string FullName { get; init; } = "";
    public string Username { get; init; } = "";
    public string Email { get; init; } = "";
    public int RoleId { get; init; }
    public string RoleName { get; init; } = "";
    public List<ModulePermissionDto> Permissions { get; init; } = [];
}

public record LoginResponse
{
    public string Token { get; init; } = "";
    public DateTime ExpiresAt { get; init; }
    public AuthUserDto User { get; init; } = new();
}

/* ---------------- Users ---------------- */

public record UserDto
{
    public int UserId { get; init; }
    public string FullName { get; init; } = "";
    public string Email { get; init; } = "";
    public string Username { get; init; } = "";
    public string? Phone { get; init; }
    public int RoleId { get; init; }
    public string RoleName { get; init; } = "";
    public bool IsActive { get; init; }
    public DateTime? LastLoginAt { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record CreateUserRequest
{
    [Required, MaxLength(100)] public string FullName { get; init; } = "";
    [Required, EmailAddress, MaxLength(150)] public string Email { get; init; } = "";
    [Required, MaxLength(50)] public string Username { get; init; } = "";
    [Required, MinLength(6)] public string Password { get; init; } = "";
    [MaxLength(20)] public string? Phone { get; init; }
    [Required] public int RoleId { get; init; }
    public bool IsActive { get; init; } = true;
}

public record UpdateUserRequest
{
    [Required, MaxLength(100)] public string FullName { get; init; } = "";
    [Required, EmailAddress, MaxLength(150)] public string Email { get; init; } = "";
    [MaxLength(20)] public string? Phone { get; init; }
    [Required] public int RoleId { get; init; }
    public bool IsActive { get; init; } = true;
    /// <summary>Optional - when supplied the user's password is reset to this value.</summary>
    public string? NewPassword { get; init; }
}

/* ---------------- Roles & permissions ---------------- */

public record RoleDto
{
    public int RoleId { get; init; }
    public string RoleName { get; init; } = "";
    public string? Description { get; init; }
    public bool IsSystem { get; init; }
    public bool IsActive { get; init; }
    public int UserCount { get; init; }
    public List<ModulePermissionDto> Permissions { get; init; } = [];
}

public record SaveRoleRequest
{
    [Required, MaxLength(50)] public string RoleName { get; init; } = "";
    [MaxLength(200)] public string? Description { get; init; }
    public bool IsActive { get; init; } = true;
    public List<PermissionInput> Permissions { get; init; } = [];
}

public record PermissionInput
{
    public int ModuleId { get; init; }
    public bool CanView { get; init; }
    public bool CanCreate { get; init; }
    public bool CanEdit { get; init; }
    public bool CanDelete { get; init; }
}

/* ---------------- Leads ---------------- */

public record LeadDto
{
    public int LeadId { get; init; }
    public string LeadCode { get; init; } = "";
    public string FullName { get; init; } = "";
    public string Phone { get; init; } = "";
    public string? Email { get; init; }
    public string? City { get; init; }
    public string? Address { get; init; }
    public int? SourceId { get; init; }
    public string? SourceName { get; init; }
    public int? ProjectId { get; init; }
    public string? ProjectName { get; init; }
    public string? PropertyType { get; init; }
    public decimal? Budget { get; init; }
    public decimal? DealValue { get; init; }
    public string Status { get; init; } = "";
    public string? RejectReason { get; init; }
    public string? Notes { get; init; }
    public int? AssignedToUserId { get; init; }
    public string? AssignedToName { get; init; }
    public DateOnly LeadDate { get; init; }
    public DateOnly? ConvertedDate { get; init; }
    public DateOnly? RejectedDate { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record SaveLeadRequest
{
    [Required, MaxLength(100)] public string FullName { get; init; } = "";
    [Required, MaxLength(20)] public string Phone { get; init; } = "";
    [MaxLength(150)] public string? Email { get; init; }
    [MaxLength(60)] public string? City { get; init; }
    [MaxLength(300)] public string? Address { get; init; }
    public int? SourceId { get; init; }
    public int? ProjectId { get; init; }
    [MaxLength(40)] public string? PropertyType { get; init; }
    public decimal? Budget { get; init; }
    public decimal? DealValue { get; init; }
    [Required] public string Status { get; init; } = "New";
    [MaxLength(300)] public string? RejectReason { get; init; }
    [MaxLength(1000)] public string? Notes { get; init; }
    public int? AssignedToUserId { get; init; }
    public DateOnly? LeadDate { get; init; }
}

public record ChangeStatusRequest
{
    [Required] public string Status { get; init; } = "";
    public decimal? DealValue { get; init; }
    [MaxLength(300)] public string? RejectReason { get; init; }
    [MaxLength(300)] public string? Remark { get; init; }
}

public record LeadHistoryDto
{
    public int HistoryId { get; init; }
    public string? FromStatus { get; init; }
    public string ToStatus { get; init; } = "";
    public string? ChangedByName { get; init; }
    public DateTime ChangedAt { get; init; }
    public string? Remark { get; init; }
}

/* ---------------- Common ---------------- */

public record PagedResult<T>
{
    public List<T> Items { get; init; } = [];
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;
}

public record LookupDto
{
    public int Id { get; init; }
    public string Name { get; init; } = "";
}

/// <summary>Payload for creating a lookup value inline from the lead form.</summary>
public record SaveLookupRequest
{
    [Required, MaxLength(100)] public string Name { get; init; } = "";
    [MaxLength(60)] public string? City { get; init; }
}

/* ---------------- Dashboard ---------------- */

public record MonthlyStatDto
{
    public int Year { get; init; }
    public int Month { get; init; }
    public string MonthName { get; init; } = "";
    public string Label { get; init; } = "";
    public int TotalLeads { get; init; }
    public int Clients { get; init; }
    public int Rejected { get; init; }
    public int Pending { get; init; }
    public decimal Revenue { get; init; }
}

public record DashboardSummaryDto
{
    /// <summary>Totals for the selected period.</summary>
    public int TotalLeads { get; init; }
    public int Clients { get; init; }
    public int Rejected { get; init; }
    public int Pending { get; init; }
    public decimal Revenue { get; init; }
    public double ConversionRate { get; init; }

    /// <summary>Percentage change vs the immediately preceding period of equal length.</summary>
    public double LeadsChangePct { get; init; }
    public double ClientsChangePct { get; init; }
    public double RejectedChangePct { get; init; }
    public double RevenueChangePct { get; init; }
}

public record DashboardResponse
{
    public DashboardSummaryDto Summary { get; init; } = new();
    /// <summary>Current month plus the previous 5 months.</summary>
    public List<MonthlyStatDto> Trend { get; init; } = [];
    public List<LookupCountDto> BySource { get; init; } = [];
    public List<LookupCountDto> ByProject { get; init; } = [];
    public List<LookupCountDto> ByStatus { get; init; } = [];
    public List<LeadDto> RecentLeads { get; init; } = [];
}

public record LookupCountDto
{
    public string Name { get; init; } = "";
    public int Count { get; init; }
    public decimal Value { get; init; }
}
