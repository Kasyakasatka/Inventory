using InventoryManagement.Web.Data;
using InventoryManagement.Web.Models.Configurations;
using InventoryManagement.Web.Services.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text;
using System.Globalization;
using InventoryManagement.Web.Data.Models;
using InventoryManagement.Web.Data.Models.Enums;
using System.Text.RegularExpressions;

namespace InventoryManagement.Web.Services.Implementations;

public class CustomIdGeneratorService : ICustomIdGeneratorService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CustomIdGeneratorService> _logger;
    private static readonly Random _random = new Random();

    public CustomIdGeneratorService(ApplicationDbContext context, ILogger<CustomIdGeneratorService> logger)
    {
        _context = context;
        _logger = logger;
    }
    public async Task<string> GenerateIdAsync(Guid inventoryId)
    {
        _logger.LogInformation("Generating custom ID for inventory {InventoryId}.", inventoryId);
        var inventory = await _context.Inventories
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == inventoryId);
        if (inventory == null)
        {
            _logger.LogError("Inventory {InventoryId} not found.", inventoryId);
            throw new InvalidOperationException("Inventory not found.");
        }
        var format = inventory.CustomIdFormat;
        CustomIdFormatModel formatModel;
        if (string.IsNullOrWhiteSpace(format))
        {
            formatModel = new CustomIdFormatModel
            {
                Elements = new List<IdElement>
            {
                new IdElement { Type = IdElementType.Guid }
            }
            };
        }
        else
        {
            formatModel = JsonSerializer.Deserialize<CustomIdFormatModel>(format);
            if (formatModel == null || !formatModel.Elements.Any())
            {
                _logger.LogError("Invalid custom ID format stored for inventory {InventoryId}.", inventoryId);
                throw new InvalidOperationException("Invalid custom ID format.");
            }
        }
        var idBuilder = new StringBuilder();
        foreach (var element in formatModel.Elements)
        {
            idBuilder.Append(await GenerateElementValueAsync(element, inventoryId));
        }
        return idBuilder.ToString();
    }

    public async Task<string> GenerateElementValueAsync(IdElement element, Guid inventoryId)
    {
        switch (element.Type)
        {
            case IdElementType.FixedText:
                return element.Value ?? string.Empty;
            case IdElementType.Random:
                return GenerateRandomValue(element.Format);
            case IdElementType.Sequence:
                return await GenerateSequenceValueAsync(element.Format, inventoryId);
            case IdElementType.DateTime:
                return DateTime.UtcNow.ToString(element.Format, CultureInfo.InvariantCulture);
            case IdElementType.Guid: 
                return Guid.NewGuid().ToString();
            default:
                throw new InvalidOperationException($"Unknown ID element type: {element.Type}");
        }
    }
    public  string GenerateRandomValue(string? format)
    {
        if (format == null) return string.Empty;
        if (format.StartsWith("D"))
        {
            int length = int.Parse(format.Substring(1));
            int max = (int)Math.Pow(10, length) - 1;
            return _random.Next(0, max).ToString($"D{length}");
        }
        else if (format.StartsWith("X"))
        {
            int length = int.Parse(format.Substring(1));
            byte[] bytes = new byte[length];
            _random.NextBytes(bytes);
            return Convert.ToHexString(bytes).Substring(0, length);
        }
        return string.Empty;
    }
    public async Task<string> GenerateSequenceValueAsync(string? format, Guid inventoryId)
    {
        var count = await _context.Items.CountAsync(i => i.InventoryId == inventoryId);
        long nextValue = count + 1;
        if (format == null || format == "D") return nextValue.ToString();
        if (format.StartsWith("D") && format.Length > 1)
        {
            if (int.TryParse(format.Substring(1, 1), out int length))
            {
                return nextValue.ToString($"D{length}");
            }
        }
        return nextValue.ToString();
    }
    public async Task<bool> ValidateIdAsync(string customId, Guid inventoryId, Guid? itemIdBeingEdited)
    {
        if (string.IsNullOrWhiteSpace(customId))
        {
            _logger.LogWarning("Validation failed: Custom ID is empty for inventory {InventoryId}.", inventoryId);
            return false;
        }
        var isDuplicate = await _context.Items
            .AnyAsync(i => i.InventoryId == inventoryId && i.CustomId == customId && i.Id != itemIdBeingEdited);
        if (isDuplicate)
        {
            _logger.LogWarning("Validation failed: Custom ID '{CustomId}' is a duplicate for inventory {InventoryId}.", customId, inventoryId);
            return false;
        }
        var inventory = await _context.Inventories
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == inventoryId);
        if (inventory == null)
        {
            _logger.LogWarning("Validation failed: Inventory not found for inventory {InventoryId}.", inventoryId);
            return false;
        }
        if (string.IsNullOrWhiteSpace(inventory.CustomIdFormat))
        {
            return true;
        }
        CustomIdFormatModel formatModel;
        try
        {
            formatModel = JsonSerializer.Deserialize<CustomIdFormatModel>(inventory.CustomIdFormat);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize CustomIdFormat for inventory {InventoryId}. Data is likely corrupted.", inventoryId);
            return false;
        }
        if (formatModel == null || !formatModel.Elements.Any())
        {
            return true;
        }
        var regexPattern = BuildRegexPattern(formatModel);
        var regex = new Regex(regexPattern, RegexOptions.IgnoreCase);
        if (!regex.IsMatch(customId))
        {
            _logger.LogWarning("Validation failed: Custom ID '{CustomId}' does not match the required format.", customId);
            return false;
        }
        return true;
    }
    public string BuildRegexPattern(CustomIdFormatModel formatModel)
    {
        var patternBuilder = new System.Text.StringBuilder();
        foreach (var element in formatModel.Elements)
        {
            switch (element.Type)
            {
                case IdElementType.FixedText:
                    if (!string.IsNullOrEmpty(element.Value))
                    {
                        patternBuilder.Append(Regex.Escape(element.Value));
                    }
                    break;
                case IdElementType.Random:
                case IdElementType.Sequence:
                    if (!string.IsNullOrEmpty(element.Format))
                    {
                        if (element.Format.StartsWith("D"))
                        {
                            int length = int.Parse(element.Format.Substring(1));
                            patternBuilder.Append($"\\d{{{length}}}");
                        }
                        else if (element.Format.StartsWith("X"))
                        {
                            int length = int.Parse(element.Format.Substring(1));
                            patternBuilder.Append($"[0-9a-fA-F]{{{length}}}");
                        }
                    }
                    else
                    {
                        patternBuilder.Append("\\d+");
                    }
                    break;
                case IdElementType.DateTime:
                    patternBuilder.Append(".+");
                    break;
                case IdElementType.Guid:
                    patternBuilder.Append("[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}");
                    break;
                default:
                    break;
            }
        }
        return $"^{patternBuilder}$"; 
    }
}