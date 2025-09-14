using InventoryManagement.Web.DTOs;
using InventoryManagement.Web.Services.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using InventoryManagement.Web.Exceptions;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using System.Linq;
using InventoryManagement.Web.Data.Models;
using AutoMapper;
using InventoryManagement.Web.ViewModels;
using Amazon.S3.Model;

namespace InventoryManagement.Web.Controllers;

public class ItemController : Controller
{
    private readonly IItemService _itemService;
    private readonly IInventoryService _inventoryService;
    private readonly ILogger<ItemController> _logger;
    private readonly IValidator<ItemDTO> _itemDtoValidator;
    private readonly ICurrentUserService _currentUserService;
    private readonly IItemLikeCommentService _likeCommentService;
    private readonly IMapper _mapper;

    public ItemController(
        IItemService itemService,
        IInventoryService inventoryService,
        ILogger<ItemController> logger,
        IValidator<ItemDTO> itemDtoValidator,
        ICurrentUserService currentUserService,
        IItemLikeCommentService likeCommentService,
        IMapper mapper)
    {
        _itemService = itemService;
        _inventoryService = inventoryService;
        _logger = logger;
        _itemDtoValidator = itemDtoValidator;
        _currentUserService = currentUserService;
        _likeCommentService = likeCommentService;
        _mapper = mapper;
    }

    [HttpGet("Item/Create/{inventoryId}")]
    [Authorize]
    public async Task<IActionResult> Create(Guid inventoryId)
    {
        var userId = _currentUserService.GetCurrentUserId();
        _logger.LogInformation("User {UserId} is requesting to create a new item in inventory {InventoryId}.", userId, inventoryId);
        var inventory = await _inventoryService.GetInventoryWithAccessAsync(inventoryId);
        if (inventory == null || (!inventory.IsPublic && inventory.CreatorId != userId && User?.IsInRole("Admin") == false && (inventory.InventoryAccesses == null || !inventory.InventoryAccesses.Any(ia => ia.UserId == userId))))
        {
            _logger.LogWarning("Access denied for user {UserId} to create item in inventory {InventoryId}.", userId, inventoryId);
            return Forbid();
        }
        var viewModel = new CreateItemViewModel
        {
            InventoryId = inventoryId,
            ItemDTO = new ItemDTO
            {
                CustomId = string.Empty,
                CustomFields = new List<CustomFieldValueDTO>()
            },
            FieldDefinitions = inventory.FieldDefinitions
        };
        return View(viewModel);
    }
    [HttpPost("Item/Create/{inventoryId}")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Guid inventoryId, ItemDTO itemDto)
    {
        var userId = _currentUserService.GetCurrentUserId();
        _logger.LogInformation("User {UserId} is attempting to create an item in inventory {InventoryId}.", userId, inventoryId);
        var inventory = await _inventoryService.GetInventoryWithAccessAsync(inventoryId);
        var isPublic = inventory?.IsPublic ?? false;
        var isCreator = inventory?.CreatorId == userId;
        var isAdmin = User?.IsInRole("Admin") == true;
        var hasAccess = inventory?.InventoryAccesses?.Any(ia => ia.UserId == userId) ?? false;
        if (inventory == null || !(isPublic || isCreator || isAdmin || hasAccess))
        {
            _logger.LogWarning("Access denied for user {UserId} to create item in inventory {InventoryId}.", userId, inventoryId);
            return Forbid();
        }
        try
        {
            var itemId = await _itemService.CreateItemAsync(inventoryId, itemDto, userId);
            _logger.LogInformation("Item {ItemId} created successfully by user {UserId} in inventory {InventoryId}.", itemId, userId, inventoryId);
            return RedirectToAction("Details", new { id = itemId });
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Failed to create item in inventory {InventoryId}. Possible duplicate CustomId.", inventoryId);
            ModelState.AddModelError(string.Empty, "Failed to create item. A duplicate custom ID may exist.");
            var viewModelWithErrors = new CreateItemViewModel
            {
                InventoryId = inventoryId,
                ItemDTO = itemDto,
                FieldDefinitions = inventory.FieldDefinitions,
                ErrorMessage = "Failed to create item. A duplicate custom ID may exist."
            };
            return View(viewModelWithErrors);
        }

    }

    [HttpGet("Item/Edit/{id}")]
    [Authorize]
    public async Task<IActionResult> Edit(Guid id)
    {
        var userId = _currentUserService.GetCurrentUserId();
        _logger.LogInformation("User {UserId} is requesting to edit item {ItemId}.", userId, id);
        var item = await _itemService.GetItemByIdAsync(id);
        if (item == null)
        {
            _logger.LogWarning("Edit item failed: Item {ItemId} not found.", id);
            return NotFound();
        }
        var inventory = await _inventoryService.GetInventoryWithAccessAsync(item.InventoryId);
        if (inventory == null || (!inventory.IsPublic && inventory.CreatorId != userId && User?.IsInRole("Admin") == false && (inventory.InventoryAccesses == null || !inventory.InventoryAccesses.Any(ia => ia.UserId == userId))))
        {
            _logger.LogWarning("Access denied for user {UserId} to edit item {ItemId}.", userId, id);
            return Forbid();
        }
        var itemDto = _mapper.Map<ItemDTO>(item);
        itemDto.Version = item.Version;
        var viewModel = new EditItemViewModel
        {
            InventoryId = item.InventoryId,
            ItemDTO = itemDto,
            ItemId = item.Id,
            FieldDefinitions = inventory.FieldDefinitions
        };
        return View(viewModel);
    }

    [HttpPost("Item/Edit/{id}")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, [Bind("Id,CustomId,CustomFields")] ItemDTO itemDto, [FromForm] IFormCollection form)
    {
        if (form.TryGetValue("ItemDTO.Version", out var versionValue))
        {
            if (uint.TryParse(versionValue, out uint parsedVersion))
            {
                itemDto.Version = parsedVersion;
            }
        }
        var userId = _currentUserService.GetCurrentUserId();
        _logger.LogInformation("User {UserId} is attempting to update item {ItemId}.", userId, id);
        var inventoryId = (await _itemService.GetItemByIdAsync(id))?.InventoryId;
        var inventory = await _inventoryService.GetInventoryWithAccessAsync(inventoryId.GetValueOrDefault());
        if (inventory == null || (!inventory.IsPublic && inventory.CreatorId != userId && User?.IsInRole("Admin") == false && (inventory.InventoryAccesses == null || !inventory.InventoryAccesses.Any(ia => ia.UserId == userId))))
        {
            _logger.LogWarning("Access denied for user {UserId} to update item {ItemId}.", userId, id);
            return Forbid();
        }
        var validationResult = await _itemDtoValidator.ValidateAsync(itemDto);
        if (!validationResult.IsValid)
        {
            _logger.LogWarning("Item update validation failed for user {UserId} for item {ItemId}. Errors: {@Errors}", userId, id, validationResult.Errors);
            var viewModelWithErrors = new EditItemViewModel
            {
                InventoryId = inventoryId.GetValueOrDefault(),
                ItemDTO = itemDto,
                ItemId = id,
                FieldDefinitions = inventory.FieldDefinitions,
                ErrorMessage = "Validation failed. Please correct the errors."
            };
            return View(viewModelWithErrors);
        }
        try
        {
            await _itemService.UpdateItemAsync(id, itemDto);
            _logger.LogInformation("Item {ItemId} updated successfully by user {UserId}.", id, userId);
            return RedirectToAction("Details", new { id = id });
        }
        catch (ConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Concurrency conflict during update of item {ItemId}.", id);
            ModelState.AddModelError(string.Empty, ex.Message);
            var latestItem = await _itemService.GetItemByIdAsync(id);
            if (latestItem == null)
            {
                return NotFound();
            }
            var latestInventory = await _inventoryService.GetInventoryWithAccessAsync(latestItem.InventoryId);
            if (latestInventory == null)
            {
                return NotFound("Inventory for the item not found.");
            }
            var viewModelWithErrors = new EditItemViewModel
            {
                ItemId = latestItem.Id,
                InventoryId = latestItem.InventoryId,
                ItemDTO = _mapper.Map<ItemDTO>(latestItem),
                FieldDefinitions = latestInventory.FieldDefinitions,
                ErrorMessage = ex.Message
            };
            return View(viewModelWithErrors);
        }
    }

    [HttpPost("Item/Delete/{id}")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userId = _currentUserService.GetCurrentUserId();
        _logger.LogInformation("User {UserId} is attempting to delete item {ItemId}.", userId, id);
        var item = await _itemService.GetItemByIdAsync(id);
        if (item == null)
        {
            _logger.LogInformation("Deletion attempt successful, item {ItemId} was not found.", id);
            return RedirectToAction("Index", "Inventory");
        }
        var inventory = await _inventoryService.GetInventoryWithAccessAsync(item.InventoryId);
        if (inventory == null || (!inventory.IsPublic && inventory.CreatorId != userId && User?.IsInRole("Admin") == false && (inventory.InventoryAccesses == null || !inventory.InventoryAccesses.Any(ia => ia.UserId == userId))))
        {
            _logger.LogWarning("Access denied for user {UserId} to delete item {ItemId}.", userId, id);
            return Forbid();
        }
        await _itemService.DeleteItemAsync(id);
        _logger.LogInformation("Item {ItemId} deleted successfully by user {UserId}.", id, userId);
        return RedirectToAction("Details", "Inventory", new { id = inventory.Id });
    }

    [HttpGet("Items/{itemId}/Interactions")]
    [AllowAnonymous]
    public async Task<IActionResult> GetItemInteractions(Guid itemId)
    {
        _logger.LogInformation("Retrieving interactions for item {ItemId}.", itemId);
        var comments = await _likeCommentService.GetCommentsAsync(itemId);
        var likesCount = await _likeCommentService.GetLikesCountAsync(itemId);
        var isLikedByUser = User?.Identity?.IsAuthenticated == true && await _likeCommentService.CheckIfLikedAsync(itemId, _currentUserService.GetCurrentUserId());
        var viewModel = new ItemInteractionsViewModel
        {
            ItemId = itemId,
            Comments = comments,
            LikesCount = likesCount,
            IsLikedByUser = isLikedByUser
        };
        _logger.LogInformation("Successfully retrieved interactions for item {ItemId}.", itemId);
        return View("Interactions", viewModel);
    }


    [HttpGet("Items/Details/{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> Details(Guid id, [FromQuery] string tab = "details")
    {
        _logger.LogInformation("Retrieving details for item {ItemId}.", id);
        var item = await _itemService.GetItemByIdAsync(id);
        if (item == null)
        {
            _logger.LogWarning("Item {ItemId} not found. Returning NotFound.", id);
            return NotFound();
        }
        var inventory = await _inventoryService.GetInventoryWithAccessAsync(item.InventoryId);
        if (inventory == null)
        {
            _logger.LogWarning("Inventory for item {ItemId} not found.", id);
            return NotFound();
        }
        var comments = await _likeCommentService.GetCommentsAsync(id);
        var likesCount = await _likeCommentService.GetLikesCountAsync(id);
        var isLikedByUser = User?.Identity?.IsAuthenticated == true && await _likeCommentService.CheckIfLikedAsync(id, _currentUserService.GetCurrentUserId());
        var currentUserId = User?.Identity?.IsAuthenticated == true ? _currentUserService.GetCurrentUserId() : Guid.Empty;
        var hasWriteAccess = inventory.IsPublic ||inventory.CreatorId == currentUserId || User?.IsInRole("Admin") == true || (inventory.InventoryAccesses != null && inventory.InventoryAccesses.Any(ia => ia.UserId == currentUserId));
        var viewModel = new ItemDetailsViewModel
        {
            Item = item,
            Comments = comments,
            LikesCount = likesCount,
            IsLikedByUser = isLikedByUser,
            HasWriteAccess = hasWriteAccess
        };
        ViewData["ActiveTab"] = tab;
        _logger.LogInformation("Successfully retrieved details for item {ItemId}.", id);
        return View(viewModel);
    }
}