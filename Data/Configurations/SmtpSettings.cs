namespace InventoryManagement.Web.Data.Configurations
{
    public class SmtpSettings
    {
        public required string Server { get; set; }
        public required int Port { get; set; }
        public required string SenderName { get; set; }
        public required string SenderEmail { get; set; }
        public required string Username { get; set; }
        public required string Password { get; set; }
    }
}
