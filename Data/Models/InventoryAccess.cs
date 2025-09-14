namespace InventoryManagement.Web.Data.Models
{
    public class InventoryAccess
    {
        public required Guid Id { get; set; }
        public required Guid InventoryId { get; set; }
        public required Inventory Inventory { get; set; }
        public required Guid UserId { get; set; }
        public required User User { get; set; } 
    }
}