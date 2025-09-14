using InventoryManagement.Web.DTOs;
using Microsoft.AspNetCore.Identity;

namespace InventoryManagement.Web.Services.Abstractions;

public interface IAdminService
{
    Task<IEnumerable<UserManagementDTO>> GetAllUsersAsync(int count);
    Task<bool> AddUserToAdminRoleAsync(Guid userId);
    Task<bool> RemoveUserFromAdminRoleAsync(Guid userId);
    Task<bool> BlockUserAsync(Guid userId);
    Task<bool> UnblockUserAsync(Guid userId);
    Task<IdentityResult> DeleteUserAsync(Guid userId);
    Task<bool> HasInventoriesAsync(Guid userId);
}