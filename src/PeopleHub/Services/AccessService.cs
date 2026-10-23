using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PeopleHub.Data;
using PeopleHub.Domain;

namespace PeopleHub.Services;

public sealed class AccessService(UserManager<AppUser> users, AppDbContext db)
{
    public async Task<AppUser?> User(ClaimsPrincipal principal)=>await users.GetUserAsync(principal);
    public async Task<bool> IsHr(AppUser user)=>await users.IsInRoleAsync(user,"Admin")||await users.IsInRoleAsync(user,"HR");
    public async Task<bool> IsManager(AppUser user)=>await IsHr(user)||await users.IsInRoleAsync(user,"Manager");
    public async Task<bool> CanRead(AppUser user,ModuleSpec module,RecordRow? row=null)
    {
        if(module.HrOnly && !await IsHr(user))return false;
        if(module.StaffOnly && !await IsManager(user) && row is null)return true; // personal records permitted
        if(row is null)return true;
        if(module.Slug=="themes")return await IsHr(user) || row.Fields.GetValueOrDefault("Shared") is true || (string?)row.Fields.GetValueOrDefault("CreatedById")==user.Id;
        // Payroll details are personal data: managers may not browse another employee's payslip.
        if(module.Slug=="payroll-lines" && !await IsHr(user))
            return user.EmployeeId.HasValue && row.Fields.GetValueOrDefault("EmployeeId") is int payrollEmployee && payrollEmployee==user.EmployeeId.Value;
        if(await IsManager(user) && !module.HrOnly)return true;
        if(module.OwnerProperty is not null){
            var id=row.Fields.GetValueOrDefault(module.OwnerProperty);
            return user.EmployeeId.HasValue && id is not null && Convert.ToInt32(id)==user.EmployeeId.Value;
        }
        return !module.StaffOnly && !module.HrOnly;
    }
    public async Task<bool> CanWrite(AppUser user,ModuleSpec module,bool create=false,RecordRow? record=null)
    {
        if(module.Slug=="themes")return record is null || await IsHr(user) || (string?)record.Fields.GetValueOrDefault("CreatedById")==user.Id;
        if(await IsHr(user))return true;
        if(create && new[]{"leave-requests","expense-claims","hr-tickets","timesheets"}.Contains(module.Slug))return user.EmployeeId is not null;
        return false;
    }
    public async Task<bool> CanDecide(AppUser user,ModuleSpec module,RecordRow record)
    {
        if(!module.StatusWorkflow)return false;
        if(module.HrOnly)return await IsHr(user);
        if(!await IsManager(user))return false;
        if(module.OwnerProperty is not null && user.EmployeeId.HasValue && record.Fields.GetValueOrDefault(module.OwnerProperty) is int owner && owner==user.EmployeeId.Value)return false;
        return true;
    }
    public IQueryable<AppUser> CompanyUsers(int company)=>db.Users.Where(x=>x.CompanyId==company);
}
