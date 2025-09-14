using AutoMapper;
using InventoryManagement.Web.Data;
using InventoryManagement.Web.Data.Models;
using InventoryManagement.Web.DTOs;
using InventoryManagement.Web.Exceptions;
using InventoryManagement.Web.Services.Abstractions;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Data;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Identity;

namespace InventoryManagement.Web.Services.Implementations;


public class InventoryService : IInventoryService
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<InventoryService> _logger;
    private readonly ICloudStorageService _cloudStorageService;
    private readonly UserManager<User> _userManager;

    public InventoryService(ApplicationDbContext context, IMapper mapper, ILogger<InventoryService> logger, ICloudStorageService cloudStorageService, UserManager<User> userManager)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
        _cloudStorageService = cloudStorageService;
        _userManager = userManager;
    }
    public async Task<IEnumerable<InventoryViewDTO>> GetLatestInventoriesAsync(int count)
    {
        _logger.LogInformation("Retrieving the latest {Count} inventories.", count);
        var inventories = await _context.Inventories
            .Include(i => i.Creator)
            .OrderByDescending(i => i.CreatedAt)
            .Take(count)
            .ToListAsync();
        _logger.LogInformation("Successfully retrieved {Count} inventories.", inventories.Count);
        return _mapper.Map<IEnumerable<InventoryViewDTO>>(inventories);
    }
    public async Task<IEnumerable<InventoryViewDTO>> GetMostPopularInventoriesAsync(int count)
    {
        _logger.LogInformation("Retrieving the top {Count} most popular inventories.", count);
        var inventories = await _context.Inventories
            .Include(i => i.Creator)
            .Select(i => new { Inventory = i, ItemCount = i.Items.Count() })
            .OrderByDescending(x => x.ItemCount)
            .Take(count)
            .ToListAsync();
        var inventoryDTOs = inventories.Select(x =>
        {
            var dto = _mapper.Map<InventoryViewDTO>(x.Inventory);
            dto.ItemCount = (uint)x.ItemCount;
            return dto;
        });
        _logger.LogInformation("Successfully retrieved {Count} popular inventories.", inventoryDTOs.Count());
        return inventoryDTOs;
    }

    public async Task<Inventory?> GetInventoryByIdAsync(Guid id)
    {
        _logger.LogInformation("Attempting to get inventory with ID {InventoryId}.", id);
        var inventory = await _context.Inventories
             .Include(i => i.Creator)
             .Include(i => i.Category)
             .Include(i => i.FieldDefinitions)
             .Include(i => i.InventoryAccesses)
                .ThenInclude(ia => ia.User)
             .FirstOrDefaultAsync(i => i.Id == id);
        if (inventory == null)
        {
            _logger.LogWarning("Inventory with ID {InventoryId} not found.", id);
        }
        else
        {
            _logger.LogInformation("Inventory with ID {InventoryId} found successfully.", id);
        }
        return inventory;
    }

    public string ProcessCustomIdFormat(string? customIdFormatDto)
    {
        if (string.IsNullOrEmpty(customIdFormatDto))
        {
            var defaultFormat = new
            {
                Elements = new[]
                {
                new { Type = "Guid" }
            }
            };
            return System.Text.Json.JsonSerializer.Serialize(defaultFormat);
        }
        else
        {
            return customIdFormatDto;
        }
    }

    public async Task<Guid> CreateInventoryAsync(InventoryDTO inventoryDto, Guid creatorId, IFormFile imageFile)
    {
        _logger.LogInformation("Creating a new inventory for user {CreatorId}.", creatorId);
        var creator = await _userManager.FindByIdAsync(creatorId.ToString());
        if (creator == null)
        {
            _logger.LogWarning("Creator user with ID {CreatorId} not found.", creatorId);
            throw new InvalidOperationException("Creator user not found.");
        }
        var category = await _context.Categories.FindAsync(inventoryDto.CategoryId);
        if (category == null)
        {
            _logger.LogWarning("Category with ID {CategoryId} not found.", inventoryDto.CategoryId);
            throw new InvalidOperationException("Category not found.");
        }
        var tags = inventoryDto.TagsInput?
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(t => t.Trim())
            .ToList() ?? new List<string>();
        var customIdFormat = !string.IsNullOrEmpty(inventoryDto.CustomIdFormat)
            ? inventoryDto.CustomIdFormat
            : "{\"Elements\":[{\"Type\":\"Guid\"}]}"; 
        var inventory = new Inventory
        {
            Id = Guid.NewGuid(),
            Title = inventoryDto.Title,
            Description = inventoryDto.Description,
            ImageUrl = null,
            CreatorId = creatorId,
            Creator = creator,
            CategoryId = inventoryDto.CategoryId,
            Category = category,
            IsPublic = inventoryDto.IsPublic,
            Tags = tags,
            CustomIdFormat = customIdFormat,
            CreatedAt = DateTime.UtcNow,
            Version = 0,
            SearchVector = null!,
            InventoryAccesses = new List<InventoryAccess>(),
            Items = new List<Item>(),
            FieldDefinitions = new List<FieldDefinition>(),
        };
        if (imageFile != null && imageFile.Length > 0)
        {
            var imageUrl = await _cloudStorageService.UploadImageAsync(imageFile);
            if (!string.IsNullOrEmpty(imageUrl))
            {
                inventory.ImageUrl = imageUrl;
            }
        }
        inventory.FieldDefinitions = _mapper.Map<List<FieldDefinition>>(inventoryDto.FieldDefinitions);
        
        _context.Inventories.Add(inventory);
        await _context.SaveChangesAsync();
        _logger.LogInformation("New inventory with ID {InventoryId} created successfully.", inventory.Id);
        return inventory.Id;
    }

    public async Task UpdateInventoryAsync(Guid inventoryId, InventoryDTO inventoryDto, IFormFile imageFile)
    {
        _logger.LogInformation("Attempting to update inventory with ID {InventoryId}.", inventoryId);
        var inventory = await _context.Inventories
            .Include(i => i.FieldDefinitions)
            .Include(i => i.InventoryAccesses)
            .FirstOrDefaultAsync(i => i.Id == inventoryId);
        if (inventory == null)
        {
            _logger.LogWarning("Update failed: Inventory with ID {InventoryId} not found.", inventoryId);
            throw new NotFoundException($"Inventory with ID {inventoryId} not found.");
        }
        if (inventory.Version != inventoryDto.Version)
        {
            _logger.LogWarning("Concurrency conflict occurred for inventory {InventoryId}. Version mismatch.", inventoryId);
            throw new ConcurrencyException("The inventory has been modified by another user. Please refresh and try again.");
        }
        inventory.Title = inventoryDto.Title;
        inventory.Description = inventoryDto.Description;
        inventory.CategoryId = inventoryDto.CategoryId;
        inventory.IsPublic = inventoryDto.IsPublic;
        inventory.LastModifiedAt = DateTime.UtcNow;
        if (!string.IsNullOrEmpty(inventoryDto.CustomIdFormat))
        {
            inventory.CustomIdFormat = inventoryDto.CustomIdFormat;
        }
        inventory.Tags = inventoryDto.TagsInput?
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(t => t.Trim())
            .ToList() ?? new List<string>();
        if (imageFile != null && imageFile.Length > 0)
        {
            var imageUrl = await _cloudStorageService.UploadImageAsync(imageFile);
            if (!string.IsNullOrEmpty(imageUrl))
            {
                inventory.ImageUrl = imageUrl;
            }
        }
        var currentFieldDefinitions = inventory.FieldDefinitions.ToList();
        var incomingFieldDefinitions = inventoryDto.FieldDefinitions.ToList();
        var fieldsToRemove = currentFieldDefinitions
            .Where(current => incomingFieldDefinitions
            .All(incoming => incoming.Id != current.Id))
            .ToList();
        var fieldsToAdd = incomingFieldDefinitions
            .Where(incoming => incoming.Id == null || incoming.Id == Guid.Empty)
            .Select(incoming =>{var newField = _mapper.Map<FieldDefinition>(incoming);
                    return newField;})
            .ToList();
        var fieldsToUpdate = incomingFieldDefinitions
            .Where(incoming => incoming.Id != null && incoming.Id != Guid.Empty)
            .ToList();
        if (fieldsToRemove.Any())
        {
            _context.FieldDefinitions.RemoveRange(fieldsToRemove);
        }
        if (fieldsToAdd.Any())
        {
            foreach (var newField in fieldsToAdd)
            {
                newField.InventoryId = inventoryId;
            }
            _context.FieldDefinitions.AddRange(fieldsToAdd);
        }
        foreach (var updatedFieldDto in fieldsToUpdate)
        {
            var existingField = currentFieldDefinitions.FirstOrDefault(f => f.Id == updatedFieldDto.Id);
            if (existingField != null)
            {
                _mapper.Map(updatedFieldDto, existingField);
            }
        }
        try
        {
            await _context.SaveChangesAsync();
            _logger.LogInformation("Inventory with ID {InventoryId} updated successfully.", inventoryId);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Concurrency conflict occurred for inventory {InventoryId}. Message: {Message}", inventoryId, ex.Message);
            throw new ConcurrencyException("The inventory has been modified by another user. Please refresh and try again.");
        }
    }
    public async Task DeleteInventoryAsync(Guid inventoryId)
    {
        _logger.LogInformation("Attempting to delete inventory with ID {InventoryId}.", inventoryId);
        var inventory = await _context.Inventories.FindAsync(inventoryId);
        if (inventory == null)
        {
            _logger.LogWarning("Deletion attempt for inventory {InventoryId} successful, as it was not found.", inventoryId);
            return;
        }
        _context.Inventories.Remove(inventory);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Inventory with ID {InventoryId} deleted successfully.", inventoryId);
    }
    public async Task<Inventory?> GetInventoryWithAccessAsync(Guid id)
    {
        _logger.LogInformation("Retrieving inventory with ID {InventoryId} including access rights.", id);
        var inventory = await _context.Inventories
            .Include(i => i.Creator)
            .Include(i => i.Category)
            .Include(i => i.InventoryAccesses)
            .Include(i => i.FieldDefinitions)
            .AsSplitQuery()
            .FirstOrDefaultAsync(i => i.Id == id);
        if (inventory == null)
        {
            _logger.LogWarning("Inventory with ID {InventoryId} (including access rights) not found.", id);
        }
        else
        {
            _logger.LogInformation("Inventory with ID {InventoryId} (including access rights) retrieved successfully.", id);
        }
        return inventory;
    }

    public async Task<IEnumerable<InventoryViewDTO>> SearchInventoriesAsync(string query)
    {
        _logger.LogInformation("Performing full-text search for inventories with query: {Query}.", query);
        if (string.IsNullOrWhiteSpace(query))
        {
            return Enumerable.Empty<InventoryViewDTO>();
        }
        var formattedQuery = string.Join(" & ", query.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(w => $"{w}:*"));
        var inventories = await _context.Inventories
            .Include(i => i.Creator)
            .Where(i => i.SearchVector.Matches(EF.Functions.ToTsQuery("english", formattedQuery)))
            .ToListAsync();
        _logger.LogInformation("Found {Count} inventories for query: {Query}.", inventories.Count, query);
        return _mapper.Map<IEnumerable<InventoryViewDTO>>(inventories);
    }

    public async Task<IEnumerable<Item>> GetItemsByInventoryIdAsync(Guid inventoryId)
    {
        _logger.LogInformation("Retrieving items for inventory {InventoryId}.", inventoryId);
        return await _context.Items
            .Include(i => i.CustomFields)
            .ThenInclude(cf => cf.FieldDefinition)
            .Where(i => i.InventoryId == inventoryId)
            .ToListAsync();
    }
}