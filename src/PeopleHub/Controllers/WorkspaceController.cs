using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PeopleHub.Data;
using PeopleHub.Domain;
using PeopleHub.Services;

namespace PeopleHub.Controllers;

[Authorize,Route("")]
public sealed class WorkspaceController(AppDbContext db,AccessService access,UserManager<AppUser> users):Controller
{
    private async Task<ShellModel?> Shell(string active){
        var user=await access.User(User);if(user is null)return null;
        var company=await db.Companies.FindAsync(user.CompanyId);if(company is null)return null;
        var role=(await users.GetRolesAsync(user)).FirstOrDefault()??"Employee";
        return new ShellModel{User=user,Company=company,Role=role,Navigation=Modules.All,ActiveSlug=active};
    }
    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var shell=await Shell("overview");if(shell is null)return Forbid();
        var company=shell.Company.Id;var employee=shell.User.EmployeeId;
        var elevated=await access.IsManager(shell.User);
        var model=new DashboardModel{Shell=shell,
            Employees=await db.Employees.CountAsync(x=>x.CompanyId==company&&x.Status=="Active"),
            Present=await db.Attendances.CountAsync(x=>x.CompanyId==company&&x.WorkDate==DateOnly.FromDateTime(DateTime.Today)&&x.Status=="Present"),
            Leaves=await db.LeaveRequests.CountAsync(x=>x.CompanyId==company&&x.Status=="Pending"&&(elevated||(employee!=null&&x.EmployeeId==employee.Value))),
            Tickets=await db.HrTickets.CountAsync(x=>x.CompanyId==company&&x.Status!="Closed"&&(elevated||(employee!=null&&x.EmployeeId==employee.Value))),
            Vacancies=await db.JobOpenings.CountAsync(x=>x.CompanyId==company&&x.Status=="Open"),
            Expenses=await db.ExpenseClaims.CountAsync(x=>x.CompanyId==company&&x.Status=="Pending"&&(elevated||(employee!=null&&x.EmployeeId==employee.Value))),
            Recent=(await Modules.Handlers["leave-requests"].List(db,company,1,null,elevated?null:employee)).Rows.Take(5).ToArray()
        };
        return View("Dashboard",model);
    }
    [HttpGet("workspace/{slug}")]
    public async Task<IActionResult> Module(string slug,string? q=null,int page=1)
    {
        if(!Modules.Index.TryGetValue(slug,out var module))return NotFound();
        var shell=await Shell(slug);if(shell is null)return Forbid();
        if(module.HrOnly && !await access.IsHr(shell.User))return Forbid();
        var model=await BuildModel(shell,module,q,page);
        return View("Module",model);
    }
    [HttpGet("workspace/{slug}/grid")]
    public async Task<IActionResult> Grid(string slug,string? q=null,int page=1)
    {
        if(!Modules.Index.TryGetValue(slug,out var module))return NotFound();
        var shell=await Shell(slug);if(shell is null)return Forbid();
        if(module.HrOnly && !await access.IsHr(shell.User))return Forbid();
        return PartialView("_Grid",await BuildModel(shell,module,q,page));
    }
    private async Task<ModuleModel> BuildModel(ShellModel shell,ModuleSpec module,string? q,int page)
    {
        var elevated=await access.IsManager(shell.User);
        var owner=module.OwnerProperty is not null&&!elevated?shell.User.EmployeeId??-1:null;
        if(module.Slug=="payroll-lines"&&!await access.IsHr(shell.User))owner=shell.User.EmployeeId??-1;
        var result=await Modules.Handlers[module.Slug].List(db,shell.Company.Id,Math.Max(1,page),q,owner);
        var visible=new List<RecordRow>();
        foreach(var row in result.Rows)if(await access.CanRead(shell.User,module,row))visible.Add(row);
        result=result with{Rows=visible};
        return new ModuleModel{Shell=shell,Module=module,Results=result,Search=q??"",
            CanWrite=await access.CanWrite(shell.User,module,create:true),
            CanDecide=module.StatusWorkflow&&await access.IsManager(shell.User),
            LookupNames=await Lookups(module,shell.Company.Id)};
    }
    private async Task<IReadOnlyDictionary<string,Dictionary<int,string>>> Lookups(ModuleSpec module,int company)
    {
        var types=Modules.Handlers[module.Slug].Fields.Where(x=>x.Lookup!=null).Select(x=>x.Lookup!).Distinct().ToArray();
        var result=new Dictionary<string,Dictionary<int,string>>();
        foreach(var slug in types){
            var data=await Modules.Handlers[slug].List(db,company,1,null);
            // Maximum 25 per lookup in the starter; search/API remain independently paginated.
            result[slug]=data.Rows.ToDictionary(x=>x.Id,x=>Label(x,slug));
        }
        return result;
    }
    public static string Label(RecordRow row,string slug)
    {
        var d=row.Fields;
        if(slug=="employees")return $"{d.GetValueOrDefault("FirstName")} {d.GetValueOrDefault("LastName")}";
        return (d.GetValueOrDefault("Name")??d.GetValueOrDefault("Title")??d.GetValueOrDefault("Period")??d.GetValueOrDefault("Code")??$"#{row.Id}").ToString()??$"#{row.Id}";
    }
    [HttpGet("workspace/{slug}/editor")]
    public async Task<IActionResult> Editor(string slug,int id=0,bool view=false)
    {
        if(!Modules.Index.TryGetValue(slug,out var module))return NotFound();
        var shell=await Shell(slug);if(shell is null)return Forbid();
        if(module.HrOnly && !await access.IsHr(shell.User))return Forbid();
        var handler=Modules.Handlers[slug];
        var owner=module.OwnerProperty is not null&&!await access.IsManager(shell.User)?shell.User.EmployeeId??-1:null;
        var record=id>0?await handler.Get(db,shell.Company.Id,id,owner):null;
        if(id>0 && (record is null||!await access.CanRead(shell.User,module,record)))return NotFound();
        if(!view && !await access.CanWrite(shell.User,module,id==0,record))return Forbid();
        return PartialView("_Editor",new EditorModel{Module=module,Fields=handler.Fields,Lookups=await Lookups(module,shell.Company.Id),Record=record,ReadOnly=view});
    }
    [HttpPost("workspace/{slug}/save"),ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(string slug,int id=0)
    {
        if(!Modules.Index.TryGetValue(slug,out var module))return NotFound();
        var shell=await Shell(slug);if(shell is null)return Forbid();
        if(module.HrOnly && !await access.IsHr(shell.User))return Forbid();
        var handler=Modules.Handlers[slug];var row=id>0?await handler.Get(db,shell.Company.Id,id):null;
        if(id>0&&row is null)return NotFound();
        if(!await access.CanWrite(shell.User,module,id==0,row))return Forbid();
        var form=await Request.ReadFormAsync();
        var values=form.Keys.ToDictionary(x=>x,x=>(string?)form[x].ToString(),StringComparer.OrdinalIgnoreCase);
        var result=await handler.Save(db,shell.Company.Id,id,values,shell.User.Id,await access.IsHr(shell.User),shell.User.EmployeeId);
        if(!result.Success){Response.StatusCode=422;return PartialView("_Editor",new EditorModel{Module=module,Fields=handler.Fields,Lookups=await Lookups(module,shell.Company.Id),Record=id>0?row:new RecordRow(0,values.ToDictionary(x=>x.Key,x=>(object?)x.Value)),Error=result.Error});}
        db.AuditEntries.Add(new AuditEntry{CompanyId=shell.Company.Id,UserId=shell.User.Id,Module=slug,RecordId=result.Id,Action=id>0?"update":"create"});await db.SaveChangesAsync();
        Response.Headers["HX-Trigger"]="{\"hrms:updated\":true,\"hrms:close-modal\":true}";
        return Content("<div class='alert alert-success mb-0'>Record saved successfully.</div>","text/html");
    }
    [HttpPost("workspace/{slug}/delete/{id:int}"),ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string slug,int id)
    {
        if(!Modules.Index.TryGetValue(slug,out var module))return NotFound();
        var shell=await Shell(slug);if(shell is null)return Forbid();
        if(!await access.IsHr(shell.User))return Forbid();
        var success=await Modules.Handlers[slug].Delete(db,shell.Company.Id,id);
        if(!success)return NotFound();
        db.AuditEntries.Add(new AuditEntry{CompanyId=shell.Company.Id,UserId=shell.User.Id,Module=slug,RecordId=id,Action="delete"});await db.SaveChangesAsync();
        Response.Headers["HX-Trigger"]="hrms:updated";
        return Content("","text/html");
    }
    [HttpPost("workspace/{slug}/decision/{id:int}"),ValidateAntiForgeryToken]
    public async Task<IActionResult> Decide(string slug,int id,string status)
    {
        if(!Modules.Index.TryGetValue(slug,out var module)||!module.StatusWorkflow)return NotFound();
        var shell=await Shell(slug);if(shell is null)return Forbid();
        var row=await Modules.Handlers[slug].Get(db,shell.Company.Id,id);if(row is null)return NotFound();
        if(!await access.CanDecide(shell.User,module,row))return Forbid();
        var states=slug switch{
            "payroll-runs"=>new[]{"Draft","Approved","Paid"},
            "hr-tickets"=>new[]{"Open","InProgress","Closed"},
            _=>new[]{"Pending","Approved","Rejected"}
        };
        if(!states.Contains(status))return BadRequest("Invalid status.");
        var entity=await db.FindAsync(Modules.Handlers[slug].EntityType,id);
        if(entity is null)return NotFound();
        db.Entry(entity).Property("Status").CurrentValue=status;
        if(slug=="leave-requests")db.Entry(entity).Property("ReviewedById").CurrentValue=shell.User.Id;
        await db.SaveChangesAsync();
        db.AuditEntries.Add(new AuditEntry{CompanyId=shell.Company.Id,UserId=shell.User.Id,Module=slug,RecordId=id,Action="decision:"+status});await db.SaveChangesAsync();
        Response.Headers["HX-Trigger"]="hrms:updated";
        return Content("<span class='badge text-bg-success'>"+System.Net.WebUtility.HtmlEncode(status)+"</span>","text/html");
    }
    [HttpGet("themes")]
    public async Task<IActionResult> Themes()
    {
        var shell=await Shell("themes");if(shell is null)return Forbid();
        ViewBag.Shell=shell;
        ViewBag.Themes=await db.ThemePresets.AsNoTracking().Where(x=>x.CompanyId==shell.Company.Id&&(x.Shared||x.CreatedById==shell.User.Id)).OrderBy(x=>x.Name).ToListAsync();
        return View("Themes");
    }
    [HttpPost("themes/save"),ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveTheme(string name,string tokens,bool shared=false)
    {
        var shell=await Shell("themes");if(shell is null)return Forbid();
        if(!ThemeService.TryValidate(tokens,out var error))return BadRequest(error);
        if(string.IsNullOrWhiteSpace(name)||name.Length>80)return BadRequest("Theme name must be 1–80 characters.");
        db.ThemePresets.Add(new ThemePreset{CompanyId=shell.Company.Id,Name=name,TokensJson=tokens,Shared=shared&&await access.IsHr(shell.User),CreatedById=shell.User.Id});
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Themes));
    }
}
