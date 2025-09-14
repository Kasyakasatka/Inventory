using InventoryManagement.Web.DTOs;
using InventoryManagement.Web.Data.Models;

namespace InventoryManagement.Web.Services.Abstractions;

public interface IInventoryService
{
    Task<IEnumerable<InventoryViewDTO>> GetLatestInventoriesAsync(int count);
    Task<IEnumerable<InventoryViewDTO>> GetMostPopularInventoriesAsync(int count);
    Task<Inventory?> GetInventoryByIdAsync(Guid id);
    Task<Guid> CreateInventoryAsync(InventoryDTO inventoryDto, Guid creatorId, IFormFile imageFile);
    Task UpdateInventoryAsync(Guid inventoryId, InventoryDTO inventoryDto, IFormFile imageFile);
    Task DeleteInventoryAsync(Guid inventoryId);
    Task<Inventory?> GetInventoryWithAccessAsync(Guid id);
    Task<IEnumerable<InventoryViewDTO>> SearchInventoriesAsync(string query);
    Task<IEnumerable<Item>> GetItemsByInventoryIdAsync(Guid inventoryId);

}