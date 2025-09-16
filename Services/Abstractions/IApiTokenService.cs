using InventoryManagement.Web.Data.Models;

namespace InventoryManagement.Web.Services.Abstractions;

public interface IApiTokenService
{
    Task<string> CreateTokenAsync(Guid inventoryId);
    Task<bool> IsValidTokenAsync(string token, Guid inventoryId);
    Task<Guid?> GetInventoryIdByTokenAsync(string token);
    Task<IEnumerable<ApiToken>> GetTokensForInventoryAsync(Guid inventoryId);
}