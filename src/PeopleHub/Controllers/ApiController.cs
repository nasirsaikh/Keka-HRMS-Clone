using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PeopleHub.Data;
using PeopleHub.Domain;
using PeopleHub.Services;

namespace PeopleHub.Controllers;

[ApiController,Route("api/v1")]
[Authorize(AuthenticationSchemes="Identity.Application,ApiBearer")]
public sealed class ApiController(AppDbContext db,AccessService access,UserManager<AppUser> users,
    SignInManager<AppUser> signIn,IAntiforgery antiforgery):ControllerBase
{
    [AllowAnonymous,HttpPost("auth/token")]
    public async Task<IActionResult> Token([FromBody]TokenLogin request)
    {
        var user=await users.FindByNameAsync(request.Username);
        if(user is null)return Unauthorized(new{error="Invalid credentials."});
        var result=await signIn.CheckPasswordSignInAsync(user,request.Password,lockoutOnFailure:true);
        if(!result.Succeeded)return Unauthorized(new{error="Invalid credentials."});
        var principal=await signIn.CreateUserPrincipalAsync(user);
        return SignIn(principal,"ApiBearer");
    }
    public sealed record TokenLogin(string Username,string Password);
    private async Task<AppUser?> Current()=>await access.User(User);
    private async Task<bool> ValidWrite()=>Request.Headers.Authorization.ToString().StartsWith("Bearer ",StringComparison.OrdinalIgnoreCase)
        || await antiforgery.IsRequestValidAsync(HttpContext);
    [HttpGet("me")]
    public async Task<IActionResult> Me(){var user=await Current();return user is null?Unauthorized():Ok(new{user.Id,user.UserName,user.DisplayName,user.CompanyId,user.EmployeeId,Roles=await users.GetRolesAsync(user)});}
    [HttpGet("modules")]
    public IActionResult ModuleList()=>Ok(Modules.All.Select(x=>new{x.Slug,x.Title,x.Group,x.Description,x.Columns}));
    [HttpGet("{slug}")]
    public async Task<IActionResult> List(string slug,int page=1,string? search=null)
    {
        var user=await Current();if(user is null)return Unauthorized();
        if(!Modules.Index.TryGetValue(slug,out var module))return NotFound();
        if(module.HrOnly&&!await access.IsHr(user))return Forbid();
        var owner=module.OwnerProperty is not null&&!await access.IsManager(user)?user.EmployeeId??-1:null;
        if(slug=="payroll-lines"&&!await access.IsHr(user))owner=user.EmployeeId??-1;
        var result=await Modules.Handlers[slug].List(db,user.CompanyId,page,search,owner);
        var visible=new List<RecordRow>();foreach(var row in result.Rows)if(await access.CanRead(user,module,row))visible.Add(row);
        return Ok(result with {Rows=visible});
    }
    [HttpGet("{slug}/{id:int}")]
    public async Task<IActionResult> Get(string slug,int id)
    {
        var user=await Current();if(user is null)return Unauthorized();
        if(!Modules.Index.TryGetValue(slug,out var module))return NotFound();
        if(module.HrOnly&&!await access.IsHr(user))return Forbid();
        var row=await Modules.Handlers[slug].Get(db,user.CompanyId,id);
        return row is null||!await access.CanRead(user,module,row)?NotFound():Ok(row);
    }
    [HttpPost("{slug}")]
    public async Task<IActionResult> Create(string slug,[FromBody]Dictionary<string,JsonElement> values)
    {
        if(!await ValidWrite())return BadRequest(new{error="Missing or invalid antiforgery token."});
        var user=await Current();if(user is null)return Unauthorized();
        if(!Modules.Index.TryGetValue(slug,out var module))return NotFound();
        if(!await access.CanWrite(user,module,true))return Forbid();
        var result=await Modules.Handlers[slug].Save(db,user.CompanyId,0,ToStrings(values),user.Id,await access.IsHr(user),user.EmployeeId);
        if(!result.Success)return BadRequest(new{error=result.Error});
        await Audit(user,slug,result.Id,"create");
        return Created($"/api/v1/{slug}/{result.Id}",new{result.Id});
    }
    [HttpPut("{slug}/{id:int}")]
    public async Task<IActionResult> Update(string slug,int id,[FromBody]Dictionary<string,JsonElement> values)
    {
        if(!await ValidWrite())return BadRequest(new{error="Invalid antiforgery token."});
        var user=await Current();if(user is null)return Unauthorized();
        if(!Modules.Index.TryGetValue(slug,out var module))return NotFound();
        var row=await Modules.Handlers[slug].Get(db,user.CompanyId,id);if(row is null)return NotFound();
        if(!await access.CanWrite(user,module,false,row))return Forbid();
        var result=await Modules.Handlers[slug].Save(db,user.CompanyId,id,ToStrings(values),user.Id,await access.IsHr(user),user.EmployeeId);
        if(!result.Success)return BadRequest(new{error=result.Error});
        await Audit(user,slug,id,"update");return Ok(new{Id=id});
    }
    [HttpDelete("{slug}/{id:int}")]
    public async Task<IActionResult> Delete(string slug,int id)
    {
        if(!await ValidWrite())return BadRequest(new{error="Invalid antiforgery token."});
        var user=await Current();if(user is null)return Unauthorized();
        if(!Modules.Index.ContainsKey(slug))return NotFound();
        if(!await access.IsHr(user))return Forbid();
        if(!await Modules.Handlers[slug].Delete(db,user.CompanyId,id))return NotFound();
        await Audit(user,slug,id,"delete");return NoContent();
    }
    [HttpPost("{slug}/{id:int}/decision")]
    public async Task<IActionResult> Decision(string slug,int id,[FromBody]DecisionPayload payload)
    {
        if(!await ValidWrite())return BadRequest(new{error="Invalid antiforgery token."});
        var user=await Current();if(user is null)return Unauthorized();
        if(!Modules.Index.TryGetValue(slug,out var module)||!module.StatusWorkflow)return NotFound();
        var row=await Modules.Handlers[slug].Get(db,user.CompanyId,id);
        if(row is null)return NotFound();
        if(!await access.CanDecide(user,module,row))return Forbid();
        var allowed=slug switch {"payroll-runs"=>new[]{"Draft","Approved","Paid"},"hr-tickets"=>new[]{"Open","InProgress","Closed"},_=>new[]{"Pending","Approved","Rejected"}};
        if(!allowed.Contains(payload.Status))return BadRequest(new{error="Invalid decision."});
        var entity=await db.FindAsync(Modules.Handlers[slug].EntityType,id);if(entity is null)return NotFound();
        db.Entry(entity).Property("Status").CurrentValue=payload.Status;
        if(slug=="leave-requests")db.Entry(entity).Property("ReviewedById").CurrentValue=user.Id;
        await db.SaveChangesAsync();await Audit(user,slug,id,"decision:"+payload.Status);
        return Ok(new{Id=id,payload.Status});
    }
    public sealed record DecisionPayload(string Status);
    private async Task Audit(AppUser user,string slug,int id,string action){db.AuditEntries.Add(new AuditEntry{CompanyId=user.CompanyId,UserId=user.Id,Module=slug,RecordId=id,Action=action});await db.SaveChangesAsync();}
    private static Dictionary<string,string?> ToStrings(IDictionary<string,JsonElement> source)=>source.ToDictionary(x=>x.Key,x=>x.Value.ValueKind switch{
        JsonValueKind.Null=>null,JsonValueKind.String=>x.Value.GetString(),JsonValueKind.True=>"true",JsonValueKind.False=>"false",_=>x.Value.ToString()
    },StringComparer.OrdinalIgnoreCase);
}
