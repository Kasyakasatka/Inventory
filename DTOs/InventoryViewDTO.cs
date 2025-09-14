namespace InventoryManagement.Web.DTOs;

public class InventoryViewDTO
{
    public required Guid Id { get; set; }
    public required string Title { get; set; }
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public required string CreatorName { get; set; }
    public required DateTime CreatedAt { get; set; }
    public required uint ItemCount { get; set; } 
}