using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PeopleHub.Domain;

namespace PeopleHub.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<AppUser>(options)
{
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Attendance> Attendances => Set<Attendance>();
    public DbSet<LeavePolicy> LeavePolicies => Set<LeavePolicy>();
    public DbSet<LeaveRequest> LeaveRequests => Set<LeaveRequest>();
    public DbSet<Shift> Shifts => Set<Shift>();
    public DbSet<Holiday> Holidays => Set<Holiday>();
    public DbSet<PayrollRun> PayrollRuns => Set<PayrollRun>();
    public DbSet<PayrollLine> PayrollLines => Set<PayrollLine>();
    public DbSet<ExpenseClaim> ExpenseClaims => Set<ExpenseClaim>();
    public DbSet<JobOpening> JobOpenings => Set<JobOpening>();
    public DbSet<Candidate> Candidates => Set<Candidate>();
    public DbSet<PerformanceReview> PerformanceReviews => Set<PerformanceReview>();
    public DbSet<HrTicket> HrTickets => Set<HrTicket>();
    public DbSet<Asset> Assets => Set<Asset>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Timesheet> Timesheets => Set<Timesheet>();
    public DbSet<TrainingCourse> TrainingCourses => Set<TrainingCourse>();
    public DbSet<TrainingEnrollment> TrainingEnrollments => Set<TrainingEnrollment>();
    public DbSet<OnboardingTask> OnboardingTasks => Set<OnboardingTask>();
    public DbSet<OffboardingCase> OffboardingCases => Set<OffboardingCase>();
    public DbSet<EmployeeDocument> EmployeeDocuments => Set<EmployeeDocument>();
    public DbSet<Survey> Surveys => Set<Survey>();
    public DbSet<ThemePreset> ThemePresets => Set<ThemePreset>();
    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);
        b.Entity<Company>().HasIndex(x=>x.Code).IsUnique();
        b.Entity<Employee>().HasIndex(x=>new {x.CompanyId,x.EmployeeCode}).IsUnique();
        b.Entity<Department>().HasIndex(x=>new {x.CompanyId,x.Code}).IsUnique();
        b.Entity<Attendance>().HasIndex(x=>new {x.CompanyId,x.EmployeeId,x.WorkDate}).IsUnique();
        b.Entity<Project>().HasIndex(x=>new {x.CompanyId,x.Code}).IsUnique();
        b.Entity<Asset>().HasIndex(x=>new {x.CompanyId,x.AssetTag}).IsUnique();
        b.Entity<PayrollRun>().HasIndex(x=>new {x.CompanyId,x.Period}).IsUnique();
        b.Entity<PayrollLine>().HasIndex(x=>new {x.PayrollRunId,x.EmployeeId}).IsUnique();
        b.Entity<AppUser>().HasIndex(x=>x.CompanyId);
        foreach (var e in b.Model.GetEntityTypes())
            if (typeof(CompanyEntity).IsAssignableFrom(e.ClrType))
                b.Entity(e.ClrType).HasIndex(nameof(CompanyEntity.CompanyId));
    }
}
