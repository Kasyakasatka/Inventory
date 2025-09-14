namespace InventoryManagement.Web.Data.Configurations
{

    public class AwsSettings
    {
        public required string AccessKey { get; set; }
        public required string SecretKey { get; set; }
        public required string Region { get; set; }
        public required string BucketName { get; set; }
        public int SignedUrlExpiresInMinutes { get; set; }
    }
}
