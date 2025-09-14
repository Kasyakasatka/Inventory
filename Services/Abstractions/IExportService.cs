namespace InventoryManagement.Web.Services.Abstractions;

public interface IExportService
{
    Task<byte[]> ExportInventoryToExcelAsync(Guid inventoryId);
}