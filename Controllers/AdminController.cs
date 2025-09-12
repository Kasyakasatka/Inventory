using InventoryManagement.Web.DTOs;
using InventoryManagement.Web.Services.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using InventoryManagement.Web.Data.Configurations;
using FluentValidation;
using InventoryManagement.Web.Models;
using InventoryManagement.Web.ViewModels;
using Microsoft.AspNetCore.Identity;
using InventoryManagement.Web.Data.Models;
namespace InventoryManagement.Web.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly IAdminService _adminService;
    private readonly ILogger<AdminController> _logger;
    private readonly ICurrentUserService _currentUserService;
    private readonly IValidator<AdminActionDTO> _adminActionValidator;
    private readonly AppConfiguration _appConfig;
    private readonly SignInManager<User> _signInManager;

    public AdminController(
        IAdminService adminService,
        ILogger<AdminController> logger,
        ICurrentUserService currentUserService,
        IValidator<AdminActionDTO> adminActionValidator,
        IOptions<AppConfiguration> appConfig,
        SignInManager<User> signInManager)
    {
        _adminService = adminService;
        _logger = logger;
        _currentUserService = currentUserService;
        _adminActionValidator = adminActionValidator;
        _appConfig = appConfig.Value;
        _signInManager = signInManager;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var currentUserId = _currentUserService.GetCurrentUserId();
        _logger.LogInformation("Admin user {UserId} is requesting the admin dashboard.", currentUserId);
        var users = await _adminService.GetAllUsersAsync(_appConfig.AdminUsersCount);
        var viewModel = new AdminDashboardViewModel { Users = users };
        return View(viewModel);
    }

    [HttpPost] 
    [Route("add-admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddAdmin(AdminActionDTO dto)
    {
        var validationResult = await _adminActionValidator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            return BadRequest(validationResult.Errors);
        }
        var currentUserId = _currentUserService.GetCurrentUserId();
        _logger.LogInformation("Admin user {CurrentUserId} is adding user {TargetUserId} to admin role.", currentUserId, dto.UserId);
        var result = await _adminService.AddUserToAdminRoleAsync(dto.UserId);
        if (!result) return BadRequest(new ErrorResponse { Message = "Failed to add user to admin role. User not found or an error occurred." });
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [Route("remove-admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveAdmin(AdminActionDTO dto)
    {
        var validationResult = await _adminActionValidator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            return BadRequest(validationResult.Errors);
        }
        var currentUserId = _currentUserService.GetCurrentUserId();
        _logger.LogInformation("Admin user {CurrentUserId} is removing user {TargetUserId} from admin role.", currentUserId, dto.UserId);
        var result = await _adminService.RemoveUserFromAdminRoleAsync(dto.UserId);
        if (!result) return BadRequest(new ErrorResponse { Message = "Failed to remove user from admin role. User not found or an error occurred." });
        if (dto.UserId == currentUserId)
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login", "Auth");
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [Route("block-user")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BlockUser(AdminActionDTO dto)
    {
        var validationResult = await _adminActionValidator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            return BadRequest(validationResult.Errors);
        }
        var currentUserId = _currentUserService.GetCurrentUserId();
        _logger.LogInformation("Admin user {CurrentUserId} is blocking user {TargetUserId}.", currentUserId, dto.UserId);
        var result = await _adminService.BlockUserAsync(dto.UserId);
        if (!result) return BadRequest(new ErrorResponse { Message = "Failed to block user. User not found or an error occurred." });
        if (dto.UserId == currentUserId)
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login", "Auth");
        }

        return RedirectToAction(nameof(Index));
    }


    [HttpPost]
    [Route("unblock-user")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UnblockUser(AdminActionDTO dto)
    {
        var validationResult = await _adminActionValidator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            return BadRequest(validationResult.Errors);
        }
        var currentUserId = _currentUserService.GetCurrentUserId();
        _logger.LogInformation("Admin user {CurrentUserId} is unblocking user {TargetUserId}.", currentUserId, dto.UserId);
        var result = await _adminService.UnblockUserAsync(dto.UserId);
        if (!result) return BadRequest(new ErrorResponse { Message = "Failed to unblock user. User not found or an error occurred." });
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [Route("delete-user")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteUser(AdminActionDTO dto)
    {
        var validationResult = await _adminActionValidator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            return BadRequest(validationResult.Errors);
        }
        var currentUserId = _currentUserService.GetCurrentUserId();
        _logger.LogInformation("Admin user {CurrentUserId} is deleting user {TargetUserId}.", currentUserId, dto.UserId);
        var result = await _adminService.DeleteUserAsync(dto.UserId);

        if (!result.Succeeded)
        {
            return BadRequest(new ErrorResponse { Message = "Failed to delete user. An error occurred." });
        }

        if (dto.UserId == currentUserId)
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login", "Auth");
        }

        return RedirectToAction(nameof(Index));
    }
}