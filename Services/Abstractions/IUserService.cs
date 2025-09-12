using InventoryManagement.Web.DTOs;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace InventoryManagement.Web.Services.Abstractions;

public interface IUserService
{
    Task<IEnumerable<UserSearchDTO>> SearchUsersAsync(string query, Guid? excludeUserId);
}