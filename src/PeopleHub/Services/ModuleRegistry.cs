using PeopleHub.Data;
using PeopleHub.Domain;

namespace PeopleHub.Services;

public sealed record ModuleSpec(string Slug,string Title,string Group,string Icon,string Description, string[] Columns,
    string? OwnerProperty=null,bool HrOnly=false,bool StaffOnly=false,bool StatusWorkflow=false);

public static class Modules
{
    public static readonly IReadOnlyList<ModuleSpec> All = new List<ModuleSpec> {
        new("employees","People directory","People","people","Employee profiles, teams and reporting lines",["EmployeeCode","FirstName","LastName","JobTitle","DepartmentId","Status","Location"]),
        new("departments","Departments","People","diagram-3","Organisational structure",["Name","Code","Description"]),
        new("attendance","Attendance","Time & Leave","calendar-check","Daily presence and time logs",["EmployeeId","WorkDate","CheckIn","CheckOut","Status"],"EmployeeId"),
        new("leave-policies","Leave policies","Time & Leave","sliders","Leave types and entitlements",["Name","AnnualDays","IsPaid","IsActive"]),
        new("leave-requests","Leave requests","Time & Leave","calendar-range","Apply, review and approve leave",["EmployeeId","LeavePolicyId","StartDate","EndDate","Status"],"EmployeeId",StatusWorkflow:true),
        new("shifts","Shift patterns","Time & Leave","clock","Define shifts and grace periods",["Name","StartsAt","EndsAt","GraceMinutes"]),
        new("holidays","Holiday calendar","Time & Leave","calendar-event","Company holidays",["Name","HolidayDate"]),
        new("payroll-runs","Payroll runs","Payroll","cash-stack","Monthly payroll workflow",["Period","Status","Notes"],HrOnly:true,StatusWorkflow:true),
        new("payroll-lines","Payslips","Payroll","receipt","Salary components and net pay",["EmployeeId","PayrollRunId","Basic","Allowances","Deductions"],"EmployeeId",StatusWorkflow:false),
        new("expense-claims","Expense claims","Payroll","wallet2","Reimbursements and approvals",["EmployeeId","SubmittedOn","Category","Amount","Status"],"EmployeeId",StatusWorkflow:true),
        new("job-openings","Job openings","Talent","briefcase","Recruitment vacancies",["Title","DepartmentId","Location","Vacancies","Status"]),
        new("candidates","Applicants","Talent","person-lines-fill","Recruitment pipeline",["Name","Email","JobOpeningId","Stage","Notes"]),
        new("performance-reviews","Performance reviews","Talent","bullseye","Goals, ratings and feedback",["EmployeeId","ReviewPeriod","Goal","Score","Status"],"EmployeeId",StaffOnly:true),
        new("training-courses","Learning catalog","Talent","mortarboard","Employee training offerings",["Title","Provider","DurationHours","Status"]),
        new("training-enrollments","Course enrollments","Talent","book","Training progress",["EmployeeId","TrainingCourseId","EnrolledOn","Status"],"EmployeeId"),
        new("hr-tickets","HR helpdesk","Workplace","chat-left-text","Employee requests and support",["EmployeeId","Subject","Category","Status","CreatedAt"],"EmployeeId",StatusWorkflow:true),
        new("assets","Asset allocation","Workplace","laptop","Company assets and assignments",["AssetTag","Name","AssignedToId","Status"]),
        new("projects","Projects","Workplace","kanban","Projects and internal work",["Name","Code","IsActive"]),
        new("timesheets","Timesheets","Workplace","stopwatch","Time tracked on projects",["EmployeeId","ProjectId","WorkDate","Hours"],"EmployeeId"),
        new("onboarding-tasks","Onboarding","Workplace","person-plus","New joiner checklist",["EmployeeId","Title","DueDate","Status"],"EmployeeId"),
        new("offboarding-cases","Offboarding","Workplace","person-dash","Employee exits and clearance",["EmployeeId","LastWorkingDate","Status"],"EmployeeId",StaffOnly:true),
        new("employee-documents","Documents","Workplace","file-earmark-text","Document metadata and expiry",["EmployeeId","Name","Category","ExpiryDate"],"EmployeeId",StaffOnly:true),
        new("surveys","Engagement surveys","Workplace","emoji-smile","Employee pulse and engagement",["Title","Question","Status"]),
        new("themes","Theme studio","Settings","palette","Custom AdminLTE palettes and layout preferences",["Name","Shared","CreatedById"]),
    }.AsReadOnly();
    public static readonly IReadOnlyDictionary<string,ModuleSpec> Index = All.ToDictionary(x=>x.Slug);
    public static readonly IReadOnlyDictionary<string,IModuleHandler> Handlers = new Dictionary<string,IModuleHandler> {
        ["employees"]=new ModuleHandler<Employee>(),["departments"]=new ModuleHandler<Department>(),
        ["attendance"]=new ModuleHandler<Attendance>(),["leave-policies"]=new ModuleHandler<LeavePolicy>(),
        ["leave-requests"]=new ModuleHandler<LeaveRequest>(),["shifts"]=new ModuleHandler<Shift>(),
        ["holidays"]=new ModuleHandler<Holiday>(),["payroll-runs"]=new ModuleHandler<PayrollRun>(),
        ["payroll-lines"]=new ModuleHandler<PayrollLine>(),["expense-claims"]=new ModuleHandler<ExpenseClaim>(),
        ["job-openings"]=new ModuleHandler<JobOpening>(),["candidates"]=new ModuleHandler<Candidate>(),
        ["performance-reviews"]=new ModuleHandler<PerformanceReview>(),["training-courses"]=new ModuleHandler<TrainingCourse>(),
        ["training-enrollments"]=new ModuleHandler<TrainingEnrollment>(),["hr-tickets"]=new ModuleHandler<HrTicket>(),
        ["assets"]=new ModuleHandler<Asset>(),["projects"]=new ModuleHandler<Project>(),
        ["timesheets"]=new ModuleHandler<Timesheet>(),["onboarding-tasks"]=new ModuleHandler<OnboardingTask>(),
        ["offboarding-cases"]=new ModuleHandler<OffboardingCase>(),["employee-documents"]=new ModuleHandler<EmployeeDocument>(),
        ["surveys"]=new ModuleHandler<Survey>(),["themes"]=new ModuleHandler<ThemePreset>()
    };
    public static readonly IReadOnlyDictionary<string,string> Foreign = new Dictionary<string,string> {
        ["DepartmentId"]="departments", ["ManagerId"]="employees", ["EmployeeId"]="employees",
        ["LeavePolicyId"]="leave-policies", ["PayrollRunId"]="payroll-runs", ["JobOpeningId"]="job-openings",
        ["AssignedToId"]="employees", ["ProjectId"]="projects", ["TrainingCourseId"]="training-courses"
    };
}

public sealed record FieldSpec(string Name,string Label,string Kind,bool Required,string? Lookup=null,string[]? Choices=null);
public sealed record RecordRow(int Id, Dictionary<string,object?> Fields);
public sealed record ListResult(int Count,int Page,int Pages,IReadOnlyList<RecordRow> Rows);
public sealed record ChangeResult(bool Success,string Error,int Id=0);
public interface IModuleHandler
{
    Type EntityType {get;}
    IReadOnlyList<FieldSpec> Fields {get;}
    Task<ListResult> List(AppDbContext db,int company,int page,string? search,int? owner=null);
    Task<RecordRow?> Get(AppDbContext db,int company,int id,int? owner=null);
    Task<ChangeResult> Save(AppDbContext db, int company,int id,IDictionary<string,string?> values, string userId,bool canManage,int? userEmployeeId);
    Task<bool> Delete(AppDbContext db,int company,int id);
    Task<bool> Exists(AppDbContext db,int company,int id);
}
