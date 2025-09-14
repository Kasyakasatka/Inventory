using InventoryManagement.Web.Data.Models.Enums;

namespace InventoryManagement.Web.DTOs
{
    public class FieldDefinitionDTO
    {
        public Guid? Id { get; set; } 
        public required string Title { get; set; }
        public required FieldType Type { get; set; }
        public required bool IsRequired { get; set; }
        public required bool ShowInTable { get; set; }
        public string? Description { get; set; }
        public string? ValidationRegex { get; set; }
        public string? ValidationMin { get; set; }
        public string? ValidationMax { get; set; }
    }
}
