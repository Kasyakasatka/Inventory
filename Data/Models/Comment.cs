namespace InventoryManagement.Web.Data.Models
{
    public class Comment
    {
        public required Guid Id { get; set; }
        public required Guid ItemId { get; set; }
        public Item? Item { get; set; }
        public required Guid UserId { get; set; }
        public User? User { get; set; }
        public required string Text { get; set; }
        public required DateTime CreatedAt { get; set; }
    }
}
