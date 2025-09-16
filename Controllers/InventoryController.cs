﻿using InventoryManagement.Web.DTOs;
using InventoryManagement.Web.Data.Models;
using InventoryManagement.Web.Services.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using InventoryManagement.Web.Exceptions;
using FluentValidation;
using InventoryManagement.Web.Data.Configurations;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using InventoryManagement.Web.Data;
using InventoryManagement.Web.ViewModels;

namespace InventoryManagement.Web.Controllers;

public class InventoryController : Controller
{
    private readonly IInventoryService _inventoryService;
    private readonly IInventoryStatsService _inventoryStatsService;
    private readonly ILogger<InventoryController> _logger;
    private readonly AppConfiguration _appConfig;
    private readonly ICurrentUserService _currentUserService;
    private readonly IValidator<InventoryDTO> _inventoryDtoValidator;
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly IExportService _exportService;
    private readonly IApiTokenService _apiTokenService;


    public InventoryController(
        IInventoryService inventoryService,
        IInventoryStatsService inventoryStatsService,
        ILogger<InventoryController> logger,
        IOptions<AppConfiguration> appConfig,
        ICurrentUserService currentUserService,
        IValidator<InventoryDTO> inventoryDtoValidator,
        ApplicationDbContext context,
        IMapper mapper,
        IExportService exportService,
        IApiTokenService apiTokenService)
    {
        _inventoryService = inventoryService;
        _inventoryStatsService = inventoryStatsService;
        _logger = logger;
        _appConfig = appConfig.Value;
        _currentUserService = currentUserService;
        _inventoryDtoValidator = inventoryDtoValidator;
        _context = context;
        _mapper = mapper;
        _exportService = exportService;
        _apiTokenService = apiTokenService;

    }

    [HttpGet("/")]
    [HttpGet("/Home/Index")]
    [AllowAnonymous]
    public async Task<IActionResult> Index()
    {
        _logger.LogInformation("Retrieving the latest and most popular inventories for the home page.");
        var latestInventories = await _inventoryService.GetLatestInventoriesAsync(_appConfig.LatestInventoriesCount);
        var popularInventories = await _inventoryService.GetMostPopularInventoriesAsync(_appConfig.PopularInventoriesCount);
        var viewModel = new HomeViewModel
        {
            LatestInventories = latestInventories,
            PopularInventories = popularInventories
        };
        return View(viewModel);
    }

    [HttpGet("Inventory/Details/{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> Details(Guid id, [FromQuery] string tab = "items")
    {
        _logger.LogInformation("Retrieving details for inventory with ID {InventoryId}.", id);
        var inventory = await _inventoryService.GetInventoryWithAccessAsync(id);
        if (inventory == null)
        {
            _logger.LogWarning("Inventory with ID {InventoryId} not found. Returning NotFound.", id);
            return NotFound();
        }
        var items = await _inventoryService.GetItemsByInventoryIdAsync(id);
        var allComments = await _context.Comments
            .Where(c => c.Item != null && c.Item.InventoryId == id)
            .Include(c => c.User)
            .Include(c => c.Item)
            .ToListAsync();
        var stats = await _inventoryStatsService.GetInventoryStatisticsAsync(id);
        var currentUserId = User?.Identity?.IsAuthenticated == true ? _currentUserService.GetCurrentUserId() : Guid.Empty;
        var isAdmin = User?.IsInRole("Admin") == true;
        var isOwnerOrAdmin = (inventory.CreatorId == currentUserId) || isAdmin;
        var hasWriteAccess = isOwnerOrAdmin ||
                             (inventory.IsPublic && User?.Identity?.IsAuthenticated == true) ||
                             (inventory.InventoryAccesses?.Any(ia => ia.UserId == currentUserId) == true);
        var viewModel = new InventoryDetailsViewModel
        {
            Inventory = inventory,
            Items = items,
            AllComments = allComments,
            Statistics = stats,
            HasWriteAccess = hasWriteAccess,
            IsOwnerOrAdmin = isOwnerOrAdmin,
            IsAdmin = isAdmin,
        };
        ViewData["ActiveTab"] = tab;
        _logger.LogInformation("Successfully retrieved details for inventory {InventoryId}.", id);
        return View(viewModel);
    }

    [HttpGet("Inventory/Create")]
    [Authorize]
    public async Task<IActionResult> Create()
    {
        var userId = _currentUserService.GetCurrentUserId();
        _logger.LogInformation("User {UserId} is requesting the inventory creation page.", userId);
        var categories = await _context.Categories.ToListAsync();
        var inventoryDto = new InventoryDTO
        {
            Title = string.Empty,
            CategoryId = Guid.Empty,
            IsPublic = false,
            Tags = new List<string>(),
            CustomIdFormat = string.Empty,
            FieldDefinitions = new List<FieldDefinitionDTO>(),
            InventoryAccesses = new List<InventoryAccess>()

        };
        var viewModel = new CreateInventoryViewModel
        {
            InventoryDTO = inventoryDto,
            Categories = categories
        };
        return View(viewModel);
    }

    [HttpPost("Inventory/Create/")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(InventoryDTO inventoryDto, IFormFile imageFile)
    {
        var userId = _currentUserService.GetCurrentUserId();
        _logger.LogInformation("User {UserId} is attempting to create a new inventory.", userId);
        var validationResult = await _inventoryDtoValidator.ValidateAsync(inventoryDto);
        if (!validationResult.IsValid)
        {
            _logger.LogWarning("Validation failed for inventory creation attempt by user {UserId}. Errors: {@Errors}", userId, validationResult.Errors);
            var categories = await _context.Categories.ToListAsync();
            var viewModel = new CreateInventoryViewModel { InventoryDTO = inventoryDto, Categories = categories };
            return View(viewModel);
        }
        try
        {
            var inventoryId = await _inventoryService.CreateInventoryAsync(inventoryDto, userId, imageFile);
            _logger.LogInformation("New inventory with ID {InventoryId} created successfully by user {UserId}.", inventoryId, userId);
            return RedirectToAction("Details", new { id = inventoryId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create inventory.");
            var categories = await _context.Categories.ToListAsync();
            var viewModel = new CreateInventoryViewModel { InventoryDTO = inventoryDto, Categories = categories, ErrorMessage = ex.Message };
            return View(viewModel);
        }
    }

    [HttpGet("Inventory/Edit/{id}")]
    [Authorize]
    public async Task<IActionResult> Edit(Guid id)
    {
        var userId = _currentUserService.GetCurrentUserId();
        _logger.LogInformation("User {UserId} is requesting to edit inventory {InventoryId}.", userId, id);
        var inventory = await _inventoryService.GetInventoryByIdAsync(id);
        if (inventory == null || (inventory.CreatorId != userId && !User.IsInRole("Admin")))
        {
            _logger.LogWarning("Access denied for user {UserId} to edit inventory {InventoryId}.", userId, id);
            return Forbid();
        }
        var categories = await _context.Categories.ToListAsync();
        var inventoryDto = _mapper.Map<InventoryDTO>(inventory);
        var viewModel = new CreateInventoryViewModel { InventoryDTO = inventoryDto, Categories = categories };
        return View(viewModel);
    }

    [HttpPost("Inventory/Edit/{id}")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, InventoryDTO inventoryDto, IFormFile imageFile)
    {
        var userId = _currentUserService.GetCurrentUserId();
        _logger.LogInformation("User {UserId} is attempting to update inventory {InventoryId}.", userId, id);
        var validationResult = await _inventoryDtoValidator.ValidateAsync(inventoryDto);
        if (!validationResult.IsValid)
        {
            _logger.LogWarning("Validation failed for updating inventory {InventoryId}. Errors: {@Errors}", id, validationResult.Errors);
            var categories = await _context.Categories.ToListAsync();
            var viewModel = new CreateInventoryViewModel { InventoryDTO = inventoryDto, Categories = categories };
            return View(viewModel);
        }
        var inventory = await _inventoryService.GetInventoryWithAccessAsync(id);
        if (inventory == null || (inventory.CreatorId != userId && !User.IsInRole("Admin")))
        {
            _logger.LogWarning("Access denied for user {UserId} to update inventory {InventoryId}.", userId, id);
            return Forbid();
        }
        try
        {
            await _inventoryService.UpdateInventoryAsync(id, inventoryDto, imageFile);
            _logger.LogInformation("Inventory {InventoryId} updated successfully by user {UserId}.", id, userId);
            return RedirectToAction("Details", new { id = id });
        }
        catch (ConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Concurrency conflict during update of inventory {InventoryId} by user {UserId}. Message: {Message}", id, userId, ex.Message);
            ModelState.AddModelError(string.Empty, "The inventory has been modified by another user. Please refresh and try again.");
            var categories = await _context.Categories.ToListAsync();
            var viewModel = new CreateInventoryViewModel { InventoryDTO = inventoryDto, Categories = categories };
            return View(viewModel);
        }
    }

    [HttpPost("Inventory/Delete/{id}")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userId = _currentUserService.GetCurrentUserId();
        _logger.LogInformation("User {UserId} is attempting to delete inventory {InventoryId}.", userId, id);
        var inventory = await _inventoryService.GetInventoryWithAccessAsync(id);
        if (inventory == null)
        {
            _logger.LogInformation("Deletion attempt successful, inventory {InventoryId} was not found.", id);
            return RedirectToAction("Index", "Home");
        }
        if (inventory.CreatorId != userId && !User.IsInRole("Admin"))
        {
            _logger.LogWarning("Access denied for user {UserId} to delete inventory {InventoryId}.", userId, id);
            return Forbid();
        }
        await _inventoryService.DeleteInventoryAsync(id);
        _logger.LogInformation("Inventory {InventoryId} deleted successfully by user {UserId}.", id, userId);
        return RedirectToAction("Index", "Home");
    }
    [HttpGet("Inventory/Export/{id}")]
    [Authorize]
    public async Task<IActionResult> ExportInventory(Guid id)
    {
        var userId = User?.Identity?.IsAuthenticated == true ? _currentUserService.GetCurrentUserId() : Guid.Empty;
        _logger.LogInformation("User {UserId} is attempting to export inventory {InventoryId} to Excel.", userId, id);
        var inventory = await _inventoryService.GetInventoryWithAccessAsync(id);
        if (inventory == null || (inventory.CreatorId != userId && !User?.IsInRole("Admin") == true))
        {
            _logger.LogWarning("Access denied for user {UserId} to export inventory {InventoryId}.", userId, id);
            return Forbid();
        }
        try
        {
            var fileBytes = await _exportService.ExportInventoryToExcelAsync(id);
            var fileName = $"inventory_{inventory.Title}_{DateTime.Now:yyyyMMdd}.xlsx";
            _logger.LogInformation("Inventory {InventoryId} exported to Excel successfully.", id);
            return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        catch (FileNotFoundException ex)
        {
            _logger.LogError(ex, "Export failed: Inventory {InventoryId} not found.", id);
            return NotFound();
        }
    }

    [HttpGet("Inventory/{id}/Tokens")]
    [Authorize]
    public async Task<IActionResult> ManageTokens(Guid id)
    {
        var currentUserId = _currentUserService.GetCurrentUserId();
        var inventory = await _inventoryService.GetInventoryByIdAsync(id);
        if (inventory == null || (inventory.CreatorId != currentUserId && !User.IsInRole("Admin")))
        {
            return Forbid(); 
        }
        var tokens = await _apiTokenService.GetTokensForInventoryAsync(id);
        var viewModel = new ManageApiTokensViewModel
        {
            InventoryId = id,
            InventoryTitle = inventory.Title,
            Tokens = tokens
        };
        return View(viewModel);
    }

    [HttpPost("Inventory/GenerateToken/{inventoryId}")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerateToken(Guid inventoryId)
    {
        var currentUserId = _currentUserService.GetCurrentUserId();
        var inventory = await _inventoryService.GetInventoryByIdAsync(inventoryId);
        if (inventory == null || (inventory.CreatorId != currentUserId && !User.IsInRole("Admin")))
        {
            return Forbid();
        }
        var token = await _apiTokenService.CreateTokenAsync(inventoryId);
        _logger.LogInformation("New API token generated for inventory {InventoryId} by user {UserId}.", inventoryId, currentUserId);
        return RedirectToAction(nameof(ManageTokens), new { id = inventoryId });
    }
}