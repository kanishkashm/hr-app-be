namespace TravelPax.Workforce.Domain.Constants;

public static class PermissionCodes
{
    public const string AttendanceView = "attendance.view";
    public const string AttendanceClock = "attendance.clock";
    public const string AttendanceManage = "attendance.manage";
    public const string UsersView = "users.view";
    public const string UsersCreate = "users.create";
    public const string UsersEdit = "users.edit";
    public const string UsersDelete = "users.delete";
    public const string RolesView = "roles.view";
    public const string RolesManage = "roles.manage";
    public const string ReportsView = "reports.view";
    public const string LeaveRequest = "leave.request";
    public const string LeaveView = "leave.view";
    public const string LeaveManage = "leave.manage";
    public const string LeaveReviewHr = "leave.review.hr";
    public const string LeaveReviewDirector = "leave.review.director";
    public const string ShiftsView = "shifts.view";
    public const string ShiftsManage = "shifts.manage";
    public const string SettingsManage = "settings.manage";
    public const string AuditView = "audit.view";

    public static readonly string[] All =
    [
        AttendanceView,
        AttendanceClock,
        AttendanceManage,
        UsersView,
        UsersCreate,
        UsersEdit,
        UsersDelete,
        RolesView,
        RolesManage,
        ReportsView,
        LeaveRequest,
        LeaveView,
        LeaveManage,
        LeaveReviewHr,
        LeaveReviewDirector,
        ShiftsView,
        ShiftsManage,
        SettingsManage,
        AuditView
    ];
}
