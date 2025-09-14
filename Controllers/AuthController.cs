using InventoryManagement.Web.DTOs;
using InventoryManagement.Web.Services.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using InventoryManagement.Web.Data.Models;
using Microsoft.Extensions.Logging;
using InventoryManagement.Web.ViewModels;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages;
using System.Security.Claims;

namespace InventoryManagement.Web.Controllers;

public class AuthController : Controller
{
    private readonly IAuthenticationService _authService;
    private readonly ILogger<AuthController> _logger;
    private readonly IValidator<RegisterDTO> _registerValidator;
    private readonly IValidator<LoginDTO> _loginValidator;
    private readonly IValidator<ConfirmEmailDTO> _confirmEmailValidator;
    private readonly IValidator<ForgotPasswordDTO> _forgotPasswordValidator;
    private readonly IValidator<ResetPasswordDTO> _resetPasswordValidator;
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;

    public AuthController(IAuthenticationService authService,
        ILogger<AuthController> logger,
        IValidator<RegisterDTO> registerValidator,
        IValidator<LoginDTO> loginValidator,
        IValidator<ConfirmEmailDTO> confirmEmailValidator,
        IValidator<ForgotPasswordDTO> forgotPasswordValidator,
        IValidator<ResetPasswordDTO> resetPasswordValidator,
        UserManager<User> userManager,
        SignInManager<User> signInManager)
    {
        _authService = authService;
        _logger = logger;
        _registerValidator = registerValidator;
        _loginValidator = loginValidator;
        _confirmEmailValidator = confirmEmailValidator;
        _forgotPasswordValidator = forgotPasswordValidator;
        _resetPasswordValidator = resetPasswordValidator;
        _userManager = userManager;
        _signInManager = signInManager;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Register()
    {
        _logger.LogInformation("User is viewing the registration page.");
        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterDTO model)
    {
        _logger.LogInformation("Attempting to register new user with email: {Email}", model.Email);
        var validationResult = await _registerValidator.ValidateAsync(model);
        if (!validationResult.IsValid)
        {
            _logger.LogWarning("Registration validation failed for email: {Email}. Errors: {@Errors}", model.Email, validationResult.Errors);
            foreach (var error in validationResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.ErrorMessage);
            }
            return View(model);
        }
        var result = await _authService.RegisterAsync(model);
        if (result.Succeeded)
        {
            _logger.LogInformation("User with email {Email} registered successfully.", model.Email);
            return RedirectToAction("ConfirmEmail", new { email = model.Email });
        }
        _logger.LogWarning("Registration failed for email: {Email}. Errors: {@Errors}", model.Email, result.Errors);
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }
        return View(model);
    }

    [HttpGet("/Auth/AccessDenied")]
    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        return View();
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Inventory");
        }
        _logger.LogInformation("User is viewing the login page.");
        return View();
    }
    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginDTO model)
    {
        _logger.LogInformation("Attempting to log in user with email: {Email}", model.Email);
        var result1 = await _authService.LoginAsync(model, model.RememberMe);
        if (result1.Succeeded)
        {
            _logger.LogInformation("User with email {Email} logged in successfully.", model.Email);
            return RedirectToAction("Index", "Home");
        }
        var validationResult = await _loginValidator.ValidateAsync(model);
        if (!validationResult.IsValid)
        {
            _logger.LogWarning("Login validation failed for email: {Email}. Errors: {@Errors}", model.Email, validationResult.Errors);
            foreach (var error in validationResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.ErrorMessage);
            }
            return View(model);
        }
        var result = await _authService.LoginAsync(model, model.RememberMe);
        if (result.Succeeded)
        {
            _logger.LogInformation("User with email {Email} logged in successfully.", model.Email);
            return RedirectToAction("Index", "Home");
        }
        if (result.IsNotAllowed)
        {
            _logger.LogWarning("Login failed for unconfirmed email: {Email}", model.Email);
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user != null && !user.EmailConfirmed)
            {
                await _authService.SendEmailConfirmationOtpAsync(model.Email);
                return RedirectToAction("ConfirmEmail", new { email = model.Email });
            }
        }
        _logger.LogWarning("Invalid login attempt for email: {Email}", model.Email);
        ModelState.AddModelError(string.Empty, "Invalid login attempt.");
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        _logger.LogInformation("User is attempting to log out.");
        await _authService.LogoutAsync();
        _logger.LogInformation("User logged out successfully.");
        return RedirectToAction("Login", "Auth");
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> ConfirmEmail(string email)
    {
        _logger.LogInformation("User is requesting email confirmation for email: {Email}", email);
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            _logger.LogWarning("Email confirmation request failed: User with email {Email} not found.", email);
            return NotFound("User not found.");
        }
        var model = new ConfirmEmailViewModel { Email = email, UserId = user.Id.ToString() };
        return View(model);
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmEmail(ConfirmEmailDTO model)
    {
        _logger.LogInformation("Attempting to confirm email for user ID: {UserId}", model.UserId);
        var validationResult = await _confirmEmailValidator.ValidateAsync(model);
        if (!validationResult.IsValid)
        {
            foreach (var error in validationResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.ErrorMessage);
            }
            var user = await _userManager.FindByIdAsync(model.UserId.ToString());
            var viewModel = new ConfirmEmailViewModel { Email = user?.Email, UserId = model.UserId.ToString() };
            return View(viewModel);
        }
        var result = await _authService.ConfirmEmailAsync(model.UserId.ToString(), model.Code);
        if (result)
        {
            _logger.LogInformation("Email confirmed successfully for user ID: {UserId}", model.UserId);
            return RedirectToAction("EmailConfirmed", "Auth");
        }
        _logger.LogWarning("Email confirmation failed for user ID: {UserId}: Invalid or expired code.", model.UserId);
        ModelState.AddModelError(string.Empty, "Invalid or expired confirmation code.");
        return View();
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult EmailConfirmed()
    {
        _logger.LogInformation("User is viewing the email confirmed page.");
        return View();
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ForgotPassword()
    {
        _logger.LogInformation("User is viewing the forgot password page.");
        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordDTO model)
    {
        _logger.LogInformation("Attempting to send password reset OTP to email: {Email}", model.Email);
        var validationResult = await _forgotPasswordValidator.ValidateAsync(model);
        if (!validationResult.IsValid)
        {
            _logger.LogWarning("Forgot password validation failed for email: {Email}. Errors: {@Errors}", model.Email, validationResult.Errors);
            foreach (var error in validationResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.ErrorMessage);
            }
            return View(model);
        }
        await _authService.SendPasswordResetOtpAsync(model.Email);
        _logger.LogInformation("Password reset OTP sent to email: {Email}.", model.Email);
        return RedirectToAction("ResetPassword", new { email = model.Email });
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ResetPassword(string email)
    {
        _logger.LogInformation("User is viewing the reset password page for email: {Email}", email);
        var model = new ResetPasswordViewModel { Email = email };
        return View(model);
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordDTO model)
    {
        _logger.LogInformation("Attempting to reset password for email: {Email}", model.Email);
        var validationResult = await _resetPasswordValidator.ValidateAsync(model);
        if (!validationResult.IsValid)
        {
            _logger.LogWarning("Password reset validation failed for email: {Email}. Errors: {@Errors}", model.Email, validationResult.Errors);
            foreach (var error in validationResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.ErrorMessage);
            }
            return View(model);
        }
        var result = await _authService.ResetPasswordAsync(model);
        if (result.Succeeded)
        {
            _logger.LogInformation("Password reset succeeded for email: {Email}", model.Email);
            return RedirectToAction("Login", "Auth");
        }
        _logger.LogWarning("Password reset failed for email: {Email}. Errors: {@Errors}", model.Email, result.Errors);
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }
        return View(model);
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public IActionResult LoginExternal(string provider, string returnUrl = null)
    {
        var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Auth", new { returnUrl });
        var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
        return Challenge(properties, provider);
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> ExternalLoginCallback(string returnUrl = null, string remoteError = null)
    {
        if (remoteError != null)
        {
            _logger.LogError("Error from external provider: {RemoteError}", remoteError);
            ModelState.AddModelError(string.Empty, $"Error from external provider: {remoteError}");
            return View(nameof(Login));
        }
        var info = await _signInManager.GetExternalLoginInfoAsync();
        if (info == null)
        {
            _logger.LogWarning("No external login info found.");
            return RedirectToAction(nameof(Login));
        }
        var signInResult = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);
        if (signInResult.Succeeded)
        {
            _logger.LogInformation("User logged in with external provider: {LoginProvider}", info.LoginProvider);
            return RedirectToLocal(returnUrl);
        }
        if (signInResult.IsLockedOut)
        {
            return RedirectToAction(nameof(Lockout));
        }
        else
        {
            ViewData["ReturnUrl"] = returnUrl;
            ViewData["LoginProvider"] = info.LoginProvider;
            var email = info.Principal.FindFirstValue(ClaimTypes.Email);
            var user = await _userManager.FindByEmailAsync(email);
            if (user != null)
            {
                var result = await _userManager.AddLoginAsync(user, info);
                if (result.Succeeded)
                {
                    await _signInManager.SignInAsync(user, isPersistent: false, authenticationMethod: info.LoginProvider);
                    _logger.LogInformation("User signed in with an existing account using an external provider.");
                    return RedirectToLocal(returnUrl);
                }
            }
            return View("ExternalLoginConfirmation", new ExternalLoginConfirmationViewModel { Email = email });
        }
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExternalLoginConfirmation(ExternalLoginConfirmationViewModel model, string returnUrl = null)
    {
        _logger.LogInformation("Attempting to confirm external login for email: {Email}", model.Email);
        returnUrl = returnUrl ?? Url.Content("~/");
        if (ModelState.IsValid)
        {
            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                _logger.LogError("Error loading external login information during confirmation.");
                return RedirectToAction(nameof(Login));
            }
            var user = new User
            {
                UserName = model.Email,
                Email = model.Email,
                EmailConfirmed = true,
                Inventories = new List<Inventory>(),
                InventoryAccesses = new List<InventoryAccess>(),
                SearchVector = null!
            };
            var result = await _userManager.CreateAsync(user);
            if (result.Succeeded)
            {
                result = await _userManager.AddLoginAsync(user, info);
                if (result.Succeeded)
                {
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    _logger.LogInformation("User created an account using an external provider.");
                    return RedirectToLocal(returnUrl);
                }
            }
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }
        ViewData["ReturnUrl"] = returnUrl;
        return View(nameof(ExternalLoginConfirmation), model);
    }
    private IActionResult RedirectToLocal(string returnUrl)
    {
        if (Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }
        else
        {
            return RedirectToAction("Index", "Home");
        }
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ExternalLogin(string provider, string returnUrl = null)
    {
        var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Auth", new { returnUrl });
        var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
        return Challenge(properties, provider);
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Lockout()
    {
        return View();
    }
}