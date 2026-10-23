using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace PeopleHub.Domain;

public sealed class AppUser : IdentityUser
{
    [MaxLength(120)] public string DisplayName { get; set; } = "";
    public int CompanyId { get; set; }
    public int? EmployeeId { get; set; }
}

public abstract class CompanyEntity
{
    public int Id { get; set; }
    public int CompanyId { get; set; }
}

public sealed class Company
{
    public int Id { get; set; }
    [Required, MaxLength(150)] public string Name { get; set; } = "";
    [Required, MaxLength(30)] public string Code { get; set; } = "";
    [MaxLength(70)] public string Country { get; set; } = "Oman";
    [MaxLength(3)] public string Currency { get; set; } = "OMR";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class Department : CompanyEntity
{
    [Required, MaxLength(100)] public string Name { get; set; } = "";
    [MaxLength(20)] public string Code { get; set; } = "";
    [MaxLength(120)] public string Description { get; set; } = "";
}
public sealed class Employee : CompanyEntity
{
    [Required, MaxLength(24)] public string EmployeeCode { get; set; } = "";
    [Required, MaxLength(70)] public string FirstName { get; set; } = "";
    [Required, MaxLength(70)] public string LastName { get; set; } = "";
    [EmailAddress, MaxLength(180)] public string Email { get; set; } = "";
    [MaxLength(30)] public string Phone { get; set; } = "";
    [MaxLength(120)] public string JobTitle { get; set; } = "";
    public int? DepartmentId { get; set; }
    public int? ManagerId { get; set; }
    public DateOnly JoinDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    [MaxLength(30)] public string Status { get; set; } = "Active";
    [MaxLength(100)] public string Location { get; set; } = "Muscat";
}
public sealed class Attendance : CompanyEntity
{
    [Range(1,int.MaxValue)] public int EmployeeId { get; set; }
    public DateOnly WorkDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public TimeOnly? CheckIn { get; set; }
    public TimeOnly? CheckOut { get; set; }
    [MaxLength(20)] public string Status { get; set; } = "Present";
    [MaxLength(240)] public string Note { get; set; } = "";
}
public sealed class LeavePolicy : CompanyEntity
{
    [Required,MaxLength(90)] public string Name { get; set; } = "";
    [Range(0,365)] public int AnnualDays { get; set; } = 30;
    public bool IsPaid { get; set; } = true;
    public bool IsActive { get; set; } = true;
}
public sealed class LeaveRequest : CompanyEntity
{
    [Range(1,int.MaxValue)] public int EmployeeId { get; set; }
    [Range(1,int.MaxValue)] public int LeavePolicyId { get; set; }
    public DateOnly StartDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public DateOnly EndDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    [MaxLength(1000)] public string Reason { get; set; } = "";
    [MaxLength(20)] public string Status { get; set; } = "Pending";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [MaxLength(450)] public string? ReviewedById { get; set; }
}
public sealed class Shift : CompanyEntity
{
    [Required,MaxLength(80)] public string Name { get; set; } = "";
    public TimeOnly StartsAt { get; set; } = new(9,0);
    public TimeOnly EndsAt { get; set; } = new(17,0);
    [Range(0,120)] public int GraceMinutes { get; set; } = 10;
}
public sealed class Holiday : CompanyEntity
{
    [Required,MaxLength(120)] public string Name { get; set; } = "";
    public DateOnly HolidayDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
}
public sealed class PayrollRun : CompanyEntity
{
    public DateOnly Period { get; set; } = new(DateTime.Today.Year,DateTime.Today.Month,1);
    [MaxLength(20)] public string Status { get; set; } = "Draft";
    [MaxLength(200)] public string Notes { get; set; } = "";
}
public sealed class PayrollLine : CompanyEntity
{
    [Range(1,int.MaxValue)] public int PayrollRunId { get; set; }
    [Range(1,int.MaxValue)] public int EmployeeId { get; set; }
    [Range(0,10000000)] public decimal Basic { get; set; }
    [Range(0,10000000)] public decimal Allowances { get; set; }
    [Range(0,10000000)] public decimal Deductions { get; set; }
    public decimal Net => Basic+Allowances-Deductions;
}
public sealed class ExpenseClaim : CompanyEntity
{
    [Range(1,int.MaxValue)] public int EmployeeId { get; set; }
    public DateOnly SubmittedOn { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    [Required,MaxLength(100)] public string Category { get; set; } = "Travel";
    [Range(0,10000000)] public decimal Amount { get; set; }
    [MaxLength(500)] public string Description { get; set; } = "";
    [MaxLength(20)] public string Status { get; set; } = "Pending";
}
public sealed class JobOpening : CompanyEntity
{
    [Required,MaxLength(150)] public string Title { get; set; } = "";
    public int? DepartmentId { get; set; }
    [MaxLength(100)] public string Location { get; set; } = "Muscat";
    [Range(1,500)] public int Vacancies { get; set; } = 1;
    [MaxLength(20)] public string Status { get; set; } = "Open";
}
public sealed class Candidate : CompanyEntity
{
    [Range(1,int.MaxValue)] public int JobOpeningId { get; set; }
    [Required,MaxLength(140)] public string Name { get; set; } = "";
    [Required,EmailAddress,MaxLength(180)] public string Email { get; set; } = "";
    [MaxLength(25)] public string Stage { get; set; } = "Applied";
    [MaxLength(1000)] public string Notes { get; set; } = "";
}
public sealed class PerformanceReview : CompanyEntity
{
    [Range(1,int.MaxValue)] public int EmployeeId { get; set; }
    [Required,MaxLength(60)] public string ReviewPeriod { get; set; } = "";
    [Required,MaxLength(240)] public string Goal { get; set; } = "";
    [Range(0,5)] public int Score { get; set; }
    [MaxLength(1000)] public string Feedback { get; set; } = "";
    [MaxLength(20)] public string Status { get; set; } = "Draft";
}
public sealed class HrTicket : CompanyEntity
{
    [Range(1,int.MaxValue)] public int EmployeeId { get; set; }
    [Required,MaxLength(180)] public string Subject { get; set; } = "";
    [MaxLength(70)] public string Category { get; set; } = "General";
    [Required,MaxLength(3000)] public string Description { get; set; } = "";
    [MaxLength(20)] public string Status { get; set; } = "Open";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
public sealed class Asset : CompanyEntity
{
    [Required,MaxLength(60)] public string AssetTag { get; set; } = "";
    [Required,MaxLength(140)] public string Name { get; set; } = "";
    public int? AssignedToId { get; set; }
    [MaxLength(25)] public string Status { get; set; } = "Available";
}
public sealed class Project : CompanyEntity
{
    [Required,MaxLength(140)] public string Name { get; set; } = "";
    [Required,MaxLength(30)] public string Code { get; set; } = "";
    public bool IsActive { get; set; } = true;
}
public sealed class Timesheet : CompanyEntity
{
    [Range(1,int.MaxValue)] public int EmployeeId { get; set; }
    [Range(1,int.MaxValue)] public int ProjectId { get; set; }
    public DateOnly WorkDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    [Range(0,24)] public decimal Hours { get; set; }
    [MaxLength(300)] public string Description { get; set; } = "";
}
public sealed class TrainingCourse : CompanyEntity
{
    [Required,MaxLength(170)] public string Title { get; set; } = "";
    [MaxLength(100)] public string Provider { get; set; } = "";
    [Range(0,200)] public int DurationHours { get; set; }
    [MaxLength(20)] public string Status { get; set; } = "Active";
}
public sealed class TrainingEnrollment : CompanyEntity
{
    [Range(1,int.MaxValue)] public int EmployeeId { get; set; }
    [Range(1,int.MaxValue)] public int TrainingCourseId { get; set; }
    public DateOnly EnrolledOn { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    [MaxLength(20)] public string Status { get; set; } = "Enrolled";
}
public sealed class OnboardingTask : CompanyEntity
{
    [Range(1,int.MaxValue)] public int EmployeeId { get; set; }
    [Required,MaxLength(180)] public string Title { get; set; } = "";
    public DateOnly DueDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    [MaxLength(25)] public string Status { get; set; } = "Pending";
}
public sealed class OffboardingCase : CompanyEntity
{
    [Range(1,int.MaxValue)] public int EmployeeId { get; set; }
    public DateOnly LastWorkingDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    [MaxLength(300)] public string Reason { get; set; } = "";
    [MaxLength(25)] public string Status { get; set; } = "Initiated";
}
public sealed class EmployeeDocument : CompanyEntity
{
    [Range(1,int.MaxValue)] public int EmployeeId { get; set; }
    [Required,MaxLength(120)] public string Name { get; set; } = "";
    [MaxLength(70)] public string Category { get; set; } = "Contract";
    [Url,MaxLength(600)] public string? StorageUrl { get; set; }
    public DateOnly? ExpiryDate { get; set; }
}
public sealed class Survey : CompanyEntity
{
    [Required,MaxLength(180)] public string Title { get; set; } = "";
    [MaxLength(500)] public string Question { get; set; } = "";
    [MaxLength(20)] public string Status { get; set; } = "Draft";
}
public sealed class ThemePreset : CompanyEntity
{
    [Required,MaxLength(80)] public string Name { get; set; } = "";
    [Required] public string TokensJson { get; set; } = "{}";
    public bool Shared { get; set; }
    [MaxLength(450)] public string? CreatedById { get; set; }
}
public sealed class AuditEntry
{
    public long Id { get; set; }
    public int CompanyId { get; set; }
    [MaxLength(450)] public string UserId { get; set; } = "";
    [MaxLength(60)] public string Module { get; set; } = "";
    public int RecordId { get; set; }
    [MaxLength(20)] public string Action { get; set; } = "";
    public DateTime AtUtc { get; set; } = DateTime.UtcNow;
}
