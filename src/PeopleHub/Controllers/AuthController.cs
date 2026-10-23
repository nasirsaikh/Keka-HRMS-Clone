using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PeopleHub.Domain;

namespace PeopleHub.Controllers;

[Route("auth")]
public sealed class AuthController(SignInManager<AppUser> signIn,UserManager<AppUser> users): Controller
{
    [AllowAnonymous,HttpGet("login")]
    public IActionResult Login(string? returnUrl=null){if(User.Identity?.IsAuthenticated==true)return RedirectToAction("Index","Workspace");ViewData["ReturnUrl"]=returnUrl;return View();}
    [AllowAnonymous,HttpPost("login"),ValidateAntiForgeryToken]
    public async Task<IActionResult> LoginPost(string username,string password,string? returnUrl=null)
    {
        var user=await users.FindByNameAsync(username);
        if(user is not null){
            var result=await signIn.PasswordSignInAsync(user,password,isPersistent:false,lockoutOnFailure:true);
            if(result.Succeeded)return LocalRedirect(Url.IsLocalUrl(returnUrl)?returnUrl!:"/");
        }
        ModelState.AddModelError("","Invalid username or password.");ViewData["ReturnUrl"]=returnUrl;
        return View("Login");
    }
    [Authorize,HttpPost("logout"),ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout(){await signIn.SignOutAsync();return RedirectToAction("Login");}
}
