using InventoryManagement.Web.Services.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace InventoryManagement.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OdooController : ControllerBase
{
    private readonly IApiTokenService _apiTokenService;
    private readonly IInventoryStatsService _inventoryStatsService;
    private readonly ILogger<OdooController> _logger;

    public OdooController(IApiTokenService apiTokenService, IInventoryStatsService inventoryStatsService, ILogger<OdooController> logger)
    {
        _apiTokenService = apiTokenService;
        _inventoryStatsService = inventoryStatsService;
        _logger = logger;
    }

    [HttpGet("inventory-stats/{token}")]
    public async Task<IActionResult> GetInventoryStats(string token)
    {
        _logger.LogInformation("API request received with token.");
        var inventoryId = await _apiTokenService.GetInventoryIdByTokenAsync(token);
        if (!inventoryId.HasValue)
        {
            _logger.LogWarning("API request failed: Invalid or expired token.");
            return Unauthorized(new { message = "Invalid or expired token." });
        }
        var stats = await _inventoryStatsService.GetInventoryStatisticsAsync(inventoryId.Value);
        _logger.LogInformation("Successfully retrieved statistics for inventory {InventoryId} via API.", inventoryId.Value);
        return Ok(stats);
    }
}