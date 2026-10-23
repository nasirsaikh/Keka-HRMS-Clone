using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using PeopleHub.Data;
using PeopleHub.Domain;

namespace PeopleHub.Services;

public sealed class ModuleHandler<T> : IModuleHandler where T:CompanyEntity,new()
{
    private static readonly PropertyInfo[] Properties = typeof(T).GetProperties(BindingFlags.Public|BindingFlags.Instance)
        .Where(p=>p.CanRead&&p.CanWrite&&p.Name is not (nameof(CompanyEntity.Id) or nameof(CompanyEntity.CompanyId))
                && p.Name is not ("Net" or "CreatedAt" or "ReviewedById" or "CreatedById"))
        .ToArray();
    public Type EntityType => typeof(T);
    public IReadOnlyList<FieldSpec> Fields => Properties.Select(p=>{
        var type=Nullable.GetUnderlyingType(p.PropertyType)??p.PropertyType;
        var kind=Modules.Foreign.ContainsKey(p.Name)?"select":type==typeof(bool)?"checkbox":
            type==typeof(DateOnly)?"date":type==typeof(TimeOnly)?"time":
            type==typeof(decimal)||type==typeof(int)?"number":p.Name.Contains("Email")?"email":
            p.Name.Contains("Description")||p.Name.Contains("Feedback")||p.Name.Contains("Reason")||p.Name=="Notes"?"textarea":"text";
        if(p.Name is "Status" or "Stage")kind="choice";
        var options=Choices(p.Name);
        var required=p.IsDefined(typeof(RequiredAttribute)) || (type==typeof(int)&&p.Name.EndsWith("Id")&& Nullable.GetUnderlyingType(p.PropertyType)==null);
        return new FieldSpec(p.Name,ToLabel(p.Name),kind,required,Modules.Foreign.GetValueOrDefault(p.Name),options);
    }).ToList();
    private static string[]? Choices(string name)=>name switch{
        "Status" when typeof(T)==typeof(Employee)=>["Active","Probation","Exited"],
        "Status" when typeof(T)==typeof(Attendance)=>["Present","Absent","Remote","Leave"],
        "Status" when typeof(T)==typeof(JobOpening)=>["Open","Closed"],
        "Status" when typeof(T)==typeof(HrTicket)=>["Open","InProgress","Closed"],
        "Status" when typeof(T)==typeof(ExpenseClaim)||typeof(T)==typeof(LeaveRequest)=>["Pending","Approved","Rejected"],
        "Status" when typeof(T)==typeof(PayrollRun)=>["Draft","Approved","Paid"],
        "Stage"=>["Applied","Screening","Interview","Offer","Hired","Rejected"],
        "Status"=>["Draft","Active","Pending","Completed","Closed"],
        _=>null
    };
    private static string ToLabel(string value)=>System.Text.RegularExpressions.Regex.Replace(value, "([a-z])([A-Z])", "$1 $2");
    private static RecordRow Row(T entity)=>new(entity.Id,
        typeof(T).GetProperties().Where(p=>p.CanRead&&p.GetIndexParameters().Length==0)
        .ToDictionary(p=>p.Name,p=>p.GetValue(entity)));

    public async Task<ListResult> List(AppDbContext db,int company,int page,string? search,int? owner=null)
    {
        var q=db.Set<T>().AsNoTracking().Where(x=>x.CompanyId==company);
        if(owner.HasValue){
            var ep=typeof(T).GetProperty("EmployeeId") ?? (typeof(T)==typeof(Asset)?typeof(T).GetProperty("AssignedToId"):null);
            if(ep is not null) {
                // query remains server-side for tenant+owner filtering
                var p=System.Linq.Expressions.Expression.Parameter(typeof(T),"x");
                var property=System.Linq.Expressions.Expression.Property(p,ep);
                var value=System.Linq.Expressions.Expression.Constant(owner.Value,ep.PropertyType==typeof(int?)?typeof(int?):typeof(int));
                var body=System.Linq.Expressions.Expression.Equal(property,value);
                q=q.Where(System.Linq.Expressions.Expression.Lambda<Func<T,bool>>(body,p));
            }
        }
        var count=await q.CountAsync();
        var results=await q.OrderByDescending(x=>x.Id).Skip((Math.Max(1,page)-1)*25).Take(25).ToListAsync();
        var rows=results.Select(Row).ToList();
        if(!string.IsNullOrWhiteSpace(search))rows=rows.Where(x=>x.Fields.Values.Any(v=>v?.ToString()?.Contains(search,StringComparison.OrdinalIgnoreCase)==true)).ToList();
        return new ListResult(count,Math.Max(1,page),Math.Max(1,(count+24)/25),rows);
    }
    public async Task<RecordRow?> Get(AppDbContext db,int company,int id,int? owner=null)
    {
        var item=await db.Set<T>().AsNoTracking().FirstOrDefaultAsync(x=>x.CompanyId==company&&x.Id==id);
        if(item is null)return null;
        if(owner.HasValue){
            var ep=typeof(T).GetProperty("EmployeeId");
            if(ep is not null && Convert.ToInt32(ep.GetValue(item))!=owner.Value)return null;
        }
        return Row(item);
    }
    public Task<bool> Exists(AppDbContext db,int company,int id)=>db.Set<T>().AnyAsync(x=>x.CompanyId==company&&x.Id==id);
    public async Task<bool> Delete(AppDbContext db,int company,int id)
    {
        var item=await db.Set<T>().FirstOrDefaultAsync(x=>x.CompanyId==company&&x.Id==id);
        if(item is null)return false;
        db.Set<T>().Remove(item);
        await db.SaveChangesAsync();
        return true;
    }
    public async Task<ChangeResult> Save(AppDbContext db,int company,int id,IDictionary<string,string?> values,string userId,bool canManage,int? userEmployeeId)
    {
        T? entity=id>0?await db.Set<T>().FirstOrDefaultAsync(x=>x.Id==id&&x.CompanyId==company):new T{CompanyId=company};
        if(entity is null)return new(false,"Record not found.");
        if(!canManage && typeof(T).GetProperty("EmployeeId") is { } ep){
            if(userEmployeeId is null)return new(false,"Your employee account has not been linked.");
            if(id>0 && Convert.ToInt32(ep.GetValue(entity))!=userEmployeeId)return new(false,"Not authorised to edit this record.");
        }
        if(id>0&&!canManage)return new(false,"Employees cannot modify submitted records. Contact HR for changes.");
        foreach(var p in Properties){
            if(!values.TryGetValue(p.Name,out var raw))continue;
            if(p.Name=="Status" && Modules.All.FirstOrDefault(m=>Modules.Handlers[m.Slug]==this)?.StatusWorkflow==true)continue;
            if(p.Name=="Status" && typeof(T)==typeof(PayrollRun))continue;
            if(!canManage && p.Name=="Status")continue;
            if(!canManage && p.Name=="EmployeeId") {p.SetValue(entity,userEmployeeId);continue;}
            try{
                var type=Nullable.GetUnderlyingType(p.PropertyType)??p.PropertyType;
                object? value=string.IsNullOrWhiteSpace(raw)&&Nullable.GetUnderlyingType(p.PropertyType)!=null?null:
                    type==typeof(string)?raw??"":type==typeof(int)?int.Parse(raw??"0",CultureInfo.InvariantCulture):
                    type==typeof(decimal)?decimal.Parse(raw??"0",CultureInfo.InvariantCulture):
                    type==typeof(bool)?raw=="true"||raw=="on"||raw=="1":
                    type==typeof(DateOnly)?DateOnly.Parse(raw??"",CultureInfo.InvariantCulture):
                    type==typeof(TimeOnly)?TimeOnly.Parse(raw??"",CultureInfo.InvariantCulture):raw;
                p.SetValue(entity,value);
            }catch{return new(false,$"Invalid {ToLabel(p.Name)}.");}
        }
        if(!canManage && typeof(T).GetProperty("EmployeeId") is { } own)own.SetValue(entity,userEmployeeId);
        if(entity is LeaveRequest leave && leave.EndDate<leave.StartDate)return new(false,"Leave end date must not precede start date.");
        if(entity is Timesheet t && t.Hours>24)return new(false,"Daily hours cannot exceed 24.");
        if(entity is PayrollLine line && line.Basic+line.Allowances<line.Deductions)return new(false,"Deductions exceed gross pay.");
        if(entity is ThemePreset theme){
            if(!ThemeService.TryValidate(theme.TokensJson,out var err))return new(false,err);
            if(!canManage)theme.Shared=false;
            if(id==0)theme.CreatedById=userId;
            if(!canManage&&id!=0&&theme.CreatedById!=userId)return new(false,"Only the theme creator can change it.");
        }
        foreach(var p in Properties.Where(p=>p.Name.EndsWith("Id")&&Modules.Foreign.ContainsKey(p.Name))){
            var value=p.GetValue(entity);
            if(value is null)continue;
            var foreignId=Convert.ToInt32(value);
            if(foreignId<=0)return new(false,$"Please select {ToLabel(p.Name)}.");
            var target=Modules.Handlers[Modules.Foreign[p.Name]];
            if(!await target.Exists(db,company,foreignId))return new(false,$"{ToLabel(p.Name)} does not belong to this company.");
        }
        var validation=new List<ValidationResult>();
        if(!Validator.TryValidateObject(entity,new ValidationContext(entity),validation,true))
            return new(false,string.Join(" ",validation.Select(v=>v.ErrorMessage)));
        if(id==0)db.Set<T>().Add(entity);
        try{await db.SaveChangesAsync();}
        catch(DbUpdateException){return new(false,"Could not save. Check unique codes and related records.");}
        return new(true,"Saved",entity.Id);
    }
}
