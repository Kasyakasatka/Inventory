using InventoryManagement.Web.DTOs;
using Microsoft.AspNetCore.Identity;

namespace InventoryManagement.Web.Services.Abstractions;

public interface IAuthenticationService
{
    Task<IdentityResult> RegisterAsync(RegisterDTO model);
    Task LogoutAsync();
    Task<bool> SendEmailConfirmationOtpAsync(string email);
    Task<bool> ConfirmEmailAsync(string userId, string code);
    Task<bool> SendPasswordResetOtpAsync(string email);
    Task<IdentityResult> ResetPasswordAsync(ResetPasswordDTO model);
    Task<SignInResult> LoginAsync(LoginDTO model, bool rememberMe);
}