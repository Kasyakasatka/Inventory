using InventoryManagement.Web.Services.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Security.Claims;
using InventoryManagement.Web.DTOs;
using InventoryManagement.Web.ViewModels;

namespace InventoryManagement.Web.Controllers;

public class SearchController : Controller
{
    private readonly IInventoryService _inventoryService;
    private readonly IItemService _itemService;
    private readonly ILogger<SearchController> _logger;
    private readonly ICurrentUserService _currentUserService;

    public SearchController(
        IInventoryService inventoryService,
        IItemService itemService,
        ILogger<SearchController> logger,
        ICurrentUserService currentUserService)
    {
        _inventoryService = inventoryService;
        _itemService = itemService;
        _logger = logger;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] string query, [FromQuery] Guid? excludeUserId)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            _logger.LogWarning("Search request failed: query is empty.");
            return View("SearchResults", new SearchResultsViewModel
            {
                Query = string.Empty,
                FoundInventories = Enumerable.Empty<InventoryViewDTO>(),
                FoundItems = Enumerable.Empty<Data.Models.Item>()
            });
        }
        var userId = User?.Identity?.IsAuthenticated == true ? _currentUserService.GetCurrentUserId().ToString() : "anonymous";
        _logger.LogInformation("Search requested for query: {Query} by user {UserId}.", query, userId);
        var inventories = await _inventoryService.SearchInventoriesAsync(query);
        var items = await _itemService.SearchItemsAsync(query);
        var viewModel = new SearchResultsViewModel
        {
            Query = query,
            FoundInventories = inventories,
            FoundItems = items
        };
        return View("SearchResults", viewModel);
    }
}