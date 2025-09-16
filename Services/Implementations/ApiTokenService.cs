using InventoryManagement.Web.Data;
using InventoryManagement.Web.Data.Models;
using InventoryManagement.Web.Services.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;

namespace InventoryManagement.Web.Services.Implementations;

public class ApiTokenService : IApiTokenService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ApiTokenService> _logger;

    public ApiTokenService(ApplicationDbContext context, ILogger<ApiTokenService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<string> CreateTokenAsync(Guid inventoryId)
    {
        var token = GenerateUniqueToken();
        var apiToken = new ApiToken
        {
            Id = Guid.NewGuid(),
            Token = token,
            InventoryId = inventoryId,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = null
        };
        await _context.ApiTokens.AddAsync(apiToken);
        await _context.SaveChangesAsync();
        _logger.LogInformation("New API token created for inventory {InventoryId}.", inventoryId);
        return token;
    }

    public async Task<bool> IsValidTokenAsync(string token, Guid inventoryId)
    {
        return await _context.ApiTokens
            .AnyAsync(t => t.Token == token && t.InventoryId == inventoryId && (t.ExpiresAt == null || t.ExpiresAt > DateTime.UtcNow));
    }

    public async Task<Guid?> GetInventoryIdByTokenAsync(string token)
    {
        var apiToken = await _context.ApiTokens
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Token == token && (t.ExpiresAt == null || t.ExpiresAt > DateTime.UtcNow));
        return apiToken?.InventoryId;
    }

    public async Task<IEnumerable<ApiToken>> GetTokensForInventoryAsync(Guid inventoryId)
    {
        return await _context.ApiTokens
            .AsNoTracking()
            .Where(t => t.InventoryId == inventoryId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    private string GenerateUniqueToken()
    {
        using var generator = RandomNumberGenerator.Create();
        var bytes = new byte[32];
        generator.GetBytes(bytes);
        return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
    }
}