using AutoMapper;
using InventoryManagement.Web.Data;
using InventoryManagement.Web.Data.Models;
using InventoryManagement.Web.DTOs;
using InventoryManagement.Web.Exceptions;
using InventoryManagement.Web.Services.Abstractions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading.Tasks;

namespace InventoryManagement.Web.Services.Implementations;


public class ItemService : IItemService
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<ItemService> _logger;
    private readonly ICustomIdGeneratorService _idGenerator;
    private readonly IInventoryService _inventoryService;
    private readonly UserManager<User> _userManager;
    public ItemService(ApplicationDbContext context, IMapper mapper, ILogger<ItemService> logger, ICustomIdGeneratorService idGenerator, IInventoryService inventoryService, UserManager<User> userManager)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
        _idGenerator = idGenerator;
        _inventoryService = inventoryService;
        _userManager = userManager;
    }
    public async Task<Item?> GetItemByIdAsync(Guid itemId)
    {
        _logger.LogInformation("Retrieving item with ID {ItemId}.", itemId);
        return await _context.Items
            .Include(i => i.CreatedBy)
            .Include(i => i.CustomFields)
            .ThenInclude(cf => cf.FieldDefinition)
            .FirstOrDefaultAsync(i => i.Id == itemId);
    }

    public async Task<Guid> CreateItemAsync(Guid inventoryId, ItemDTO itemDto, Guid createdByUserId)
    {
        _logger.LogInformation("Creating new item for inventory {InventoryId}.", inventoryId);
        var inventory = await _inventoryService.GetInventoryWithAccessAsync(inventoryId);
        if (inventory == null)
        {
            _logger.LogWarning("Inventory with ID {InventoryId} not found.", inventoryId);
            throw new InvalidOperationException("Inventory not found.");
        }
        var user = await _userManager.FindByIdAsync(createdByUserId.ToString());
        if (user == null)
        {
            _logger.LogWarning("Create item failed: User {UserId} not found.", createdByUserId);
            throw new InvalidOperationException("User not found.");
        }
        var customId = await _idGenerator.GenerateIdAsync(inventoryId);

        var item = new Item
        {
            Id = Guid.NewGuid(),
            CustomId = customId,
            InventoryId = inventoryId,
            Inventory = inventory,
            CreatedById = createdByUserId,
            CreatedBy = user,
            CreatedAt = DateTime.UtcNow,
            Version = 0,
            SearchVector = null!,
            Comments = new List<Comment>(),
            Likes = new List<Like>(),
            CustomFields = new List<CustomFieldValue>()
        };
        item.CustomFields = itemDto.CustomFields.Select(dto =>
        {
            var customField = _mapper.Map<CustomFieldValue>(dto);
            customField.Id = Guid.NewGuid();
            customField.ItemId = item.Id;
            customField.Item = item;
            return customField;
        }).ToList();
        _context.Items.Add(item);
        try
        {
            await _context.SaveChangesAsync();
            _logger.LogInformation("New item with ID {ItemId} created successfully in inventory {InventoryId}.", item.Id, inventoryId);
            return item.Id;
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Failed to create item in inventory {InventoryId}. Possible duplicate CustomId.", inventoryId);
            throw new InvalidOperationException("Failed to create item. A duplicate custom ID may exist.", ex);
        }
    }

    public async Task UpdateItemAsync(Guid itemId, ItemDTO itemDto)
    {
        _logger.LogInformation("Updating item with ID {ItemId}.", itemId);
        var item = await GetItemByIdAsync(itemId);
        if (item == null)
        {
            _logger.LogWarning("Update failed: Item {ItemId} not found.", itemId);
            throw new NotFoundException($"Item with ID {itemId} not found.");
        }
        if (item.Version != itemDto.Version)
        {
            _logger.LogWarning("Concurrency conflict occurred for item {ItemId}. Version mismatch.", itemId);
            throw new ConcurrencyException("The item has been modified by another user. Please refresh and try again.");
        }
        if (itemDto == null)
        {
            _logger.LogWarning("Update failed for item {ItemId}: DTO is null.", itemId);
            throw new ArgumentNullException(nameof(itemDto), "Item DTO cannot be null.");
        }
        string? newCustomId = itemDto.CustomId;
        if (string.IsNullOrWhiteSpace(newCustomId))
        {
            newCustomId = await _idGenerator.GenerateIdAsync(item.InventoryId);
        }
        else
        {
            var isCustomIdValid = await _idGenerator.ValidateIdAsync(itemDto.CustomId, item.InventoryId, itemId);
            if (!isCustomIdValid)
            {
                _logger.LogWarning("Update failed for item {ItemId}: Invalid Custom ID format.", itemId);
                throw new InvalidOperationException("The custom ID format is invalid.");
            }
        }
        _mapper.Map(itemDto, item);
        UpdateCustomFields(item, itemDto);
        try
        {
            await _context.SaveChangesAsync();
            _logger.LogInformation("Item with ID {ItemId} updated successfully.", itemId);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Concurrency conflict during update of item {ItemId}.", itemId);
            throw new ConcurrencyException("The item has been modified by another user. Please refresh and try again.");
        }
    }

    private void UpdateCustomFields(Item item, ItemDTO itemDto)
    {
        var existingFields = item.CustomFields.ToDictionary(f => f.FieldDefinitionId);
        var incomingFields = itemDto.CustomFields.ToDictionary(f => f.FieldDefinitionId);
        var fieldsToRemove = existingFields.Where(f => !incomingFields.ContainsKey(f.Key)).ToList();
        _context.CustomFields.RemoveRange(fieldsToRemove.Select(f => f.Value));
        var fieldsToAdd = incomingFields.Where(f => !existingFields.ContainsKey(f.Key)).ToList();
        foreach (var field in fieldsToAdd)
        {
            _context.CustomFields.Add(_mapper.Map<CustomFieldValue>(field.Value));
        }
        var fieldsToUpdate = incomingFields.Where(f => existingFields.ContainsKey(f.Key)).ToList();
        foreach (var field in fieldsToUpdate)
        {
            _mapper.Map(field.Value, existingFields[field.Key]);
        }
    }

    public async Task DeleteItemAsync(Guid itemId)
    {
        _logger.LogInformation("Deleting item with ID {ItemId}.", itemId);
        var item = await _context.Items.FindAsync(itemId);
        if (item == null)
        {
            _logger.LogWarning("Deletion attempt for item {ItemId} successful, as it was not found.", itemId);
            return;
        }
        _context.Items.Remove(item);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Item with ID {ItemId} deleted successfully.", itemId);
    }

    public async Task<IEnumerable<Item>> SearchItemsAsync(string query)
    {
        _logger.LogInformation("Performing full-text search for items with query: {Query}.", query);
        if (string.IsNullOrWhiteSpace(query))
        {
            return Enumerable.Empty<Item>();
        }
        var formattedQuery = string.Join(" & ", query.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(w => $"{w}:*"));
        var items = await _context.Items
            .Include(i => i.Inventory)
            .Where(i => i.SearchVector.Matches(EF.Functions.ToTsQuery("english", formattedQuery)))
            .ToListAsync();
        _logger.LogInformation("Found {Count} items for query: {Query}.", items.Count, query);
        return items;
    }
}