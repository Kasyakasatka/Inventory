using System.ComponentModel.DataAnnotations.Schema;

namespace InventoryManagement.Web.Data.Models;

public class ApiToken
{
    public Guid Id { get; set; }
    public required string Token { get; set; }
    public required Guid InventoryId { get; set; }
    public Inventory? Inventory { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
}