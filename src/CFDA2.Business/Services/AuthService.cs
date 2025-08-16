using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace CFDA2.Business.Services;

public class AuthService
{
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly UserManager<IdentityUser> _userManager;
    public AuthService(SignInManager<IdentityUser> signInManager, UserManager<IdentityUser> userManager)
    {
        _signInManager = signInManager;
        _userManager = userManager;
    }

    public async Task<bool> Login(LoginRequest request)
    {
        var result = await _signInManager.PasswordSignInAsync(request.UserName, request.Password, true, lockoutOnFailure: false);
        if (result.Succeeded)
        {
            var user = await _userManager.FindByNameAsync(request.UserName);
            return true;
        }
        return false;
    }

    public async Task<bool> Logout()
    {
        await _signInManager.SignOutAsync();
        return true;
    }

    public async Task<(IdentityUser User, IList<string> Roles)> Me(ClaimsPrincipal principal)
    {
        var user = await _userManager.GetUserAsync(principal);
        if (user == null) return new();
        var roles = await _userManager.GetRolesAsync(user);
        return (user, roles);
    }

    public record LoginRequest (string UserName, string Password);    
}