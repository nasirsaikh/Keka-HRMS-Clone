using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PeopleHub.Data;
using PeopleHub.Domain;
using PeopleHub.Services;

var builder=WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<AppDbContext>(o=>o.UseSqlite(builder.Configuration.GetConnectionString("Default")));
builder.Services.AddIdentity<AppUser,IdentityRole>(o=>{
    o.Password.RequiredLength=12;
    o.Password.RequireUppercase=true;
    o.Password.RequireLowercase=true;
    o.Password.RequireDigit=true;
    o.Lockout.DefaultLockoutTimeSpan=TimeSpan.FromMinutes(15);
    o.Lockout.MaxFailedAccessAttempts=5;
    o.User.RequireUniqueEmail=true;
}).AddEntityFrameworkStores<AppDbContext>().AddDefaultTokenProviders();
builder.Services.AddAuthentication().AddBearerToken("ApiBearer");
builder.Services.ConfigureApplicationCookie(o=>{o.LoginPath="/auth/login";o.AccessDeniedPath="/auth/login";o.Cookie.Name="PeopleHub.Auth";o.Cookie.HttpOnly=true;o.Cookie.SameSite=SameSiteMode.Lax;o.Cookie.SecurePolicy=CookieSecurePolicy.SameAsRequest;o.ExpireTimeSpan=TimeSpan.FromHours(8);});
builder.Services.AddAntiforgery(o=>o.HeaderName="RequestVerificationToken");
builder.Services.AddScoped<AccessService>();
var app=builder.Build();
if(!app.Environment.IsDevelopment()){app.UseExceptionHandler("/error");app.UseHsts();}
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapGet("/error",()=>Results.Problem("An unexpected error occurred."));
app.MapControllers();
app.MapGet("/health",()=>Results.Ok(new{status="ok"}));
await SeedData.Initialise(app.Services,app.Configuration,app.Environment);
app.Run();
public partial class Program { }
