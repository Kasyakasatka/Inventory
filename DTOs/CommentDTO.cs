namespace InventoryManagement.Web.DTOs;

public class CommentDTO
{
    public required string Text { get; set; }
    public required Guid ItemId { get; set; }
}