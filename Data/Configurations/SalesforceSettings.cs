namespace InventoryManagement.Web.Data.Configurations
{
    public class SalesforceSettings
    {
        public required string InstanceUrl { get; set; }
        public required string ClientId { get; set; }
        public required string ClientSecret { get; set; }
        public required string Username { get; set; }
        public required string Password { get; set; }
        public required string RedirectUri { get; set; }
        public required string AuthUrl { get; set; }

    }
}