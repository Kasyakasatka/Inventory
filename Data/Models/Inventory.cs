using NpgsqlTypes;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection.Metadata;

namespace InventoryManagement.Web.Data.Models;

public class Inventory
{
    public required Guid Id { get; set; }
    public required string Title { get; set; }
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public required Guid CreatorId { get; set; }
    public required User Creator { get; set; }
    public required Guid CategoryId { get; set; }
    public required Category Category { get; set; }
    public required bool IsPublic { get; set; }
    public required List<string> Tags { get; set; } = new();
    [Column(TypeName = "jsonb")]
    public required string CustomIdFormat { get; set; }
    public required DateTime CreatedAt { get; set; }
    public DateTime? LastModifiedAt { get; set; }
    public required uint Version { get; set; }
    public required NpgsqlTsVector SearchVector { get; set; }
    public required List<InventoryAccess> InventoryAccesses { get; set; } = new();
    public required List<Item> Items { get; set; } = new();
    public required List<FieldDefinition> FieldDefinitions { get; set; } = new();
}