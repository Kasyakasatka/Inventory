using Microsoft.AspNetCore.Identity;

namespace InventoryManagement.Web.Models.Configurations;

public class IdentityConfig
{
    public PasswordOptions Password { get; set; } = new();
    public UserOptions User { get; set; } = new();
}

public class PasswordOptions
{
    public bool RequireDigit { get; set; }
    public int RequiredLength { get; set; }
    public bool RequireNonAlphanumeric { get; set; }
    public bool RequireUppercase { get; set; }
    public bool RequireLowercase { get; set; }
}
public class UserOptions
{
    public int MinLength { get; set; }
    public int MaxLength { get; set; } 
    public bool RequireUniqueEmail { get; set; }
}