using NpgsqlTypes;

namespace InventoryManagement.Web.Data.Models
{
    public class Item
    {
        public required Guid Id { get; set; }
        public required Guid InventoryId { get; set; }
        public required Inventory Inventory { get; set; }
        public required string CustomId { get; set; }
        public required Guid CreatedById { get; set; }
        public required User CreatedBy { get; set; }
        public required DateTime CreatedAt { get; set; }
        public required uint Version { get; set; }
        public required List<Like> Likes { get; set; } = new();
        public required List<Comment> Comments { get; set; } = new();
        public required NpgsqlTsVector SearchVector { get; set; }
        public required List<CustomFieldValue> CustomFields { get; set; } = new();
    }
}