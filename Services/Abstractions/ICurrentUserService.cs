using System;

namespace InventoryManagement.Web.Services.Abstractions;

public interface ICurrentUserService
{
    Guid GetCurrentUserId();
}