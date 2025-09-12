namespace InventoryManagement.Web.Data.Models
{
    public class Category
    {
        public required Guid Id { get; set; }
        public required string Name { get; set; }
        public List<Inventory> Inventories { get; set; } = new();
    }
}
