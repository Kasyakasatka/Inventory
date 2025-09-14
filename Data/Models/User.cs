using Microsoft.AspNetCore.Identity;
using NpgsqlTypes;

namespace InventoryManagement.Web.Data.Models;

public class User : IdentityUser<Guid>
{
    public required List<Inventory> Inventories { get; set; } = new();
    public required List<InventoryAccess> InventoryAccesses { get; set; } = new();
    public NpgsqlTsVector? SearchVector { get; set; }
   
}