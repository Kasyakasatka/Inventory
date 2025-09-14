using InventoryManagement.Web.Data.Models;
using System.Threading.Tasks;

namespace InventoryManagement.Web.Services.Abstractions;

public interface ICustomIdGeneratorService
{
    Task<string> GenerateIdAsync(Guid inventoryId);
    Task<bool> ValidateIdAsync(string customId, Guid inventoryId, Guid? itemIdBeingEdited);
    string BuildRegexPattern(CustomIdFormatModel formatModel);
    Task<string> GenerateSequenceValueAsync(string? format, Guid inventoryId);
    string GenerateRandomValue(string? format);
    Task<string> GenerateElementValueAsync(IdElement element, Guid inventoryId);
}