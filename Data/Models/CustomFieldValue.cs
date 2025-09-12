using InventoryManagement.Web.Data.Models;
using InventoryManagement.Web.Models;

namespace InventoryManagement.Web.Data.Models;
public class CustomFieldValue
{
    public required Guid Id { get; set; }
    public required Guid ItemId { get; set; }
    public required Item Item { get; set; }
    public required Guid FieldDefinitionId { get; set; }
    public required FieldDefinition FieldDefinition { get; set; }
    public string? StringValue { get; set; }
    public int? IntValue { get; set; }
    public bool? BoolValue { get; set; }
}