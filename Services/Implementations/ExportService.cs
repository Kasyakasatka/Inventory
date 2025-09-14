using InventoryManagement.Web.Data;
using InventoryManagement.Web.Services.Abstractions;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using InventoryManagement.Web.Data.Models;

namespace InventoryManagement.Web.Services.Implementations;

public class ExportService : IExportService
{
    private readonly ApplicationDbContext _context;

    public ExportService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<byte[]> ExportInventoryToExcelAsync(Guid inventoryId)
    {
        var inventory = await _context.Inventories
            .Include(i => i.FieldDefinitions)
            .Include(i => i.Items)
            .ThenInclude(i => i.CustomFields)
            .ThenInclude(cf => cf.FieldDefinition)
            .FirstOrDefaultAsync(i => i.Id == inventoryId);
        if (inventory == null)
        {
            throw new FileNotFoundException("Inventory not found.");
        }
        var itemsWithCreator = await _context.Items
        .Where(item => item.InventoryId == inventoryId)
        .Include(i => i.CreatedBy) 
        .Include(i => i.CustomFields)
        .ThenInclude(cf => cf.FieldDefinition)
        .ToListAsync();
        using var memoryStream = new MemoryStream();
        ExcelPackage.License.SetNonCommercialOrganization("My Noncommercial organization");
        using (var package = new ExcelPackage())
        {
            var worksheet = package.Workbook.Worksheets.Add("Inventory");
            var headers = new List<string> { "CustomId", "CreatedBy", "CreatedAt" };
            headers.AddRange(inventory.FieldDefinitions.OrderBy(fd => fd.Title).Select(fd => fd.Title));
            for (int i = 0; i < headers.Count; i++)
            {
                worksheet.Cells[1, i + 1].Value = headers[i];
            }
            int row = 2;
            foreach (var item in itemsWithCreator)
            {
                worksheet.Cells[row, 1].Value = item.CustomId;
                worksheet.Cells[row, 2].Value = item.CreatedBy.UserName; 
                worksheet.Cells[row, 3].Value = item.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");
                int col = 4;
                foreach (var field in inventory.FieldDefinitions.OrderBy(fd => fd.Title))
                {
                    var customValue = item.CustomFields.FirstOrDefault(cf => cf.FieldDefinitionId == field.Id);
                    if (customValue != null)
                    {
                        switch (field.Type)
                        {
                            case Data.Models.Enums.FieldType.String:
                                worksheet.Cells[row, col].Value = customValue.StringValue;
                                break;
                            case Data.Models.Enums.FieldType.Int:
                                worksheet.Cells[row, col].Value = customValue.IntValue;
                                break;
                            case Data.Models.Enums.FieldType.Bool:
                                worksheet.Cells[row, col].Value = customValue.BoolValue;
                                break;
                            default:
                                worksheet.Cells[row, col].Value = string.Empty;
                                break;
                        }
                    }
                    col++;
                }
                row++;
            }
            var fileBytes = await package.GetAsByteArrayAsync();
            return fileBytes;
        }
    }
}