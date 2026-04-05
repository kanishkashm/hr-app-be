using Microsoft.AspNetCore.Identity;

namespace TravelPax.Workforce.Domain.Entities;

public sealed class AppUser : IdentityUser<Guid>
{
    public string EmployeeId { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? MobileNumber { get; set; }
    public string? Department { get; set; }
    public string? Designation { get; set; }
    public string? EmploymentType { get; set; }
    public DateOnly? DateJoined { get; set; }
    public Guid? ReportingManagerId { get; set; }
    public AppUser? ReportingManager { get; set; }
    public Guid? BranchId { get; set; }
    public OfficeBranch? Branch { get; set; }
    public string Status { get; set; } = "Active";
    public string? ProfilePhotoUrl { get; set; }
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactPhone { get; set; }
    public bool MustChangePassword { get; set; }
    public DateTimeOffset? LastLoginAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }

    public ICollection<UserPermission> UserPermissions { get; set; } = new List<UserPermission>();
    public ICollection<AttendanceRecord> AttendanceRecords { get; set; } = new List<AttendanceRecord>();
}
