namespace InventoryManagement.Web.DTOs
{
    public class SupportTicketRequestDTO
    {
        public required string Summary { get; set; }
        public required string Priority { get; set; }
        public string? Inventory { get; set; }
    }
}