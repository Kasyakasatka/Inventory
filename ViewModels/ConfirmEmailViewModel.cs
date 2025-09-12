namespace InventoryManagement.Web.ViewModels
{
    public class ConfirmEmailViewModel
    {
        public required string Email { get; set; }
        public required string UserId { get; set; }
        public string? Code { get; set; }
    }
}
