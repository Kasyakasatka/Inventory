namespace InventoryManagement.Web.DTOs
{

    public class SupportTicketDTO
    {
        public required string ReportedBy { get; set; }
        public required string Inventory { get; set; }
        public required string Link { get; set; }
        public required string Priority { get; set; }
        public required string Summary { get; set; }
        public required List<string> AdminEmails { get; set; }
    }
}