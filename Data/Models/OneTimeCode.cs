using InventoryManagement.Web.Data.Models.Enums;

namespace InventoryManagement.Web.Data.Models;

public class OneTimeCode
{
    public required Guid Id { get; set; }
    public required Guid UserId { get; set; }
    public required User User { get; set; }
    public required string Code { get; set; }
    public required DateTime ExpirationDate { get; set; }
    public required CodeType Type { get; set; }
}

