using InventoryManagement.Web.Services.Abstractions;
using InventoryManagement.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace InventoryManagement.Web.Controllers;

[Authorize]
public class ProfileController : Controller
{
    private readonly IUserService _userService;
    private readonly ILogger<ProfileController> _logger;
    private readonly ICurrentUserService _currentUserService;

    public ProfileController(IUserService userService, ILogger<ProfileController> logger, ICurrentUserService currentUserService)
    {
        _userService = userService;
        _logger = logger;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var currentUserId = _currentUserService.GetCurrentUserId();
        _logger.LogInformation("User {UserId} is requesting their profile page.", currentUserId);

        var ownedInventories = await _userService.GetOwnedInventoriesAsync(currentUserId);
        var writeAccessInventories = await _userService.GetWriteAccessInventoriesAsync(currentUserId);

        var viewModel = new ProfileViewModel
        {
            OwnedInventories = ownedInventories,
            WriteAccessInventories = writeAccessInventories
        };

        return View(viewModel);
    }
}