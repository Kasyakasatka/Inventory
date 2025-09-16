using InventoryManagement.Web.Services.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using InventoryManagement.Web.DTOs;

namespace InventoryManagement.Web.Controllers;

[Authorize]
public class UsersController : Controller
{
    private readonly IUserService _userService;
    private readonly ILogger<UsersController> _logger;
    private readonly ICurrentUserService _currentUserService;

    public UsersController(IUserService userService, ILogger<UsersController> logger, ICurrentUserService currentUserService)
    {
        _userService = userService;
        _logger = logger;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    [Route("Users/Search")]
    public async Task<IActionResult> Search([FromQuery] string query, [FromQuery] Guid? excludeUserId)
    {
        var currentUserId = _currentUserService.GetCurrentUserId();
        _logger.LogInformation("User {UserId} is searching for users with query: {Query}.", currentUserId, query);
        if (string.IsNullOrWhiteSpace(query))
        {
            _logger.LogWarning("Search request failed: query is empty.");
            return Json(new List<UserSearchDTO>());
        }
        var users = await _userService.SearchUsersAsync(query, excludeUserId);
        return Json(users);
    }
}