using InventoryManagement.Web.Services.Abstractions;
using Microsoft.AspNetCore.Http;
using System;
using System.Security.Claims;

namespace InventoryManagement.Web.Services.Implementations;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid GetCurrentUserId()
    {
        var userId = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
        {
            throw new UnauthorizedAccessException("User is not authenticated.");
        }
        return Guid.Parse(userId);
    }
}