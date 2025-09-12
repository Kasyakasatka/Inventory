using InventoryManagement.Web.Data;
using InventoryManagement.Web.Data.Models;
using InventoryManagement.Web.Data.Models.Enums;
using InventoryManagement.Web.DTOs;
using InventoryManagement.Web.Services.Abstractions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace InventoryManagement.Web.Services.Implementations;

public class AuthenticationService : IAuthenticationService
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly IEmailService _emailService;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AuthenticationService> _logger;

    public AuthenticationService(UserManager<User> userManager, SignInManager<User> signInManager, IEmailService emailService, ApplicationDbContext context, ILogger<AuthenticationService> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _emailService = emailService;
        _context = context;
        _logger = logger;
    }

    public async Task<IdentityResult> RegisterAsync(RegisterDTO model)
    {
        var user = new User
        {
            UserName = model.Username,
            Email = model.Email,
            EmailConfirmed = false,
            Inventories = new List<Inventory>(), 
            InventoryAccesses = new List<InventoryAccess>(), 
            SearchVector = null! 
        };
        _logger.LogInformation("Attempting to register new user with email: {Email}.", model.Email);
        var result = await _userManager.CreateAsync(user, model.Password);
        if (result.Succeeded)
        {
            await SendEmailConfirmationOtpAsync(model.Email);
        }
        return result;
    }

    public async Task<SignInResult> LoginAsync(LoginDTO model, bool rememberMe)
    {
        _logger.LogInformation("User with email {Email} is attempting to log in.", model.Email);
        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null)
        {
            _logger.LogWarning("Login failed: User with email {Email} not found.", model.Email);
            return SignInResult.Failed;
        }
        if (!user.EmailConfirmed)
        {
            _logger.LogWarning("Login failed: Email not confirmed for user with email {Email}.", model.Email);
            return SignInResult.NotAllowed;
        }
        var result = await _signInManager.PasswordSignInAsync(user, model.Password, rememberMe, lockoutOnFailure: true);
        if (result.Succeeded)
        {
            _logger.LogInformation("Login successful for user with email {Email}.", model.Email);
        }
        else
        {
            _logger.LogWarning("Login failed for user with email {Email}. Reason: {@Reason}", model.Email, result);
        }
        return result;
    }

    public async Task LogoutAsync()
    {
        await _signInManager.SignOutAsync();
        _logger.LogInformation("User logged out successfully.");
    }

    public async Task<bool> SendEmailConfirmationOtpAsync(string email)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null || user.EmailConfirmed) return false;
        var existingCodes = await _context.OneTimeCodes
            .Where(c => c.UserId == user.Id && c.Type == CodeType.EmailConfirmation)
            .ToListAsync();
        _context.OneTimeCodes.RemoveRange(existingCodes);
        await _context.SaveChangesAsync();
        var otpCode = GenerateOtpCode();
        var oneTimeCode = new OneTimeCode
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Code = otpCode,
            User = user,
            ExpirationDate = DateTime.UtcNow.AddMinutes(15),
            Type = CodeType.EmailConfirmation
        };
        _context.OneTimeCodes.Add(oneTimeCode);
        await _context.SaveChangesAsync();
        var subject = "Email Confirmation";
        var body = $"Your one-time email confirmation code is: {otpCode}";
        await _emailService.SendEmailAsync(user.Email!, subject, body);
        _logger.LogInformation("Email confirmation OTP sent to {Email}.", email);
        return true;
    }

    public async Task<bool> ConfirmEmailAsync(string userId, string code)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null || user.EmailConfirmed) return false;
        var oneTimeCode = await _context.OneTimeCodes
            .FirstOrDefaultAsync(c => c.UserId.ToString() == userId && c.Code == code && c.Type == CodeType.EmailConfirmation);
        if (oneTimeCode == null || oneTimeCode.ExpirationDate < DateTime.UtcNow)
        {
            _logger.LogWarning("Email confirmation failed for user {UserId}: Invalid or expired code.", userId);
            return false;
        }
        user.EmailConfirmed = true;
        _context.OneTimeCodes.Remove(oneTimeCode);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Email confirmed successfully for user {UserId}.", userId);
        return true;
    }

    public async Task<bool> SendPasswordResetOtpAsync(string email)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null) return false;
        var existingCodes = await _context.OneTimeCodes
            .Where(c => c.UserId == user.Id && c.Type == CodeType.PasswordReset)
            .ToListAsync();
        _context.OneTimeCodes.RemoveRange(existingCodes);
        await _context.SaveChangesAsync();
        var otpCode = GenerateOtpCode();
        var oneTimeCode = new OneTimeCode
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Code = otpCode,
            User = user,
            ExpirationDate = DateTime.UtcNow.AddMinutes(15),
            Type = CodeType.PasswordReset
        };
        _context.OneTimeCodes.Add(oneTimeCode);
        await _context.SaveChangesAsync();
        var subject = "Password Reset";
        var body = $"Your one-time password reset code is: {otpCode}";
        await _emailService.SendEmailAsync(user.Email!, subject, body);
        _logger.LogInformation("Password reset OTP sent to {Email}.", email);
        return true;
    }

    public async Task<IdentityResult> ResetPasswordAsync(ResetPasswordDTO model)
    {
        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null) return IdentityResult.Failed(new IdentityError { Description = "User not found." });
        var oneTimeCode = await _context.OneTimeCodes
            .FirstOrDefaultAsync(c => c.UserId == user.Id && c.Code == model.Code && c.Type == CodeType.PasswordReset);
        if (oneTimeCode == null || oneTimeCode.ExpirationDate < DateTime.UtcNow)
        {
            _logger.LogWarning("Password reset failed for user {Email}: Invalid or expired code.", model.Email);
            return IdentityResult.Failed(new IdentityError { Description = "Invalid or expired code." });
        }
        _context.OneTimeCodes.Remove(oneTimeCode);
        await _context.SaveChangesAsync();
        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, model.Password);
        if (result.Succeeded)
        {
            _logger.LogInformation("Password reset successfully for user {Email}.", model.Email);
        }
        else
        {
            _logger.LogWarning("Password reset failed for user {Email}. Errors: {@Errors}", model.Email, result.Errors);
        }
        return result;
    }

    private string GenerateOtpCode()
    {
        var random = new Random();
        return random.Next(100000, 999999).ToString();
    }
}