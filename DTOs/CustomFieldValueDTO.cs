namespace InventoryManagement.Web.DTOs
{
    public class CustomFieldValueDTO
    {
        public required Guid FieldDefinitionId { get; set; }
        public string? StringValue { get; set; }
        public int? IntValue { get; set; }
        public bool? BoolValue { get; set; }
    }
}
