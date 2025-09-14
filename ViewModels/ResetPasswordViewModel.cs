namespace InventoryManagement.Web.ViewModels
{

    public class ResetPasswordViewModel
    {
        public required string Email { get; set; }
        public string? Code { get; set; }
        public string? Password { get; set; }
        public string? ConfirmPassword { get; set; }
    }
}
