namespace InventoryManagement.Web.Constants;

public static class IdentityConstants
{
    public const int DefaultMaxFailedAccessAttempts = 1;
    public static readonly TimeSpan DefaultLockoutTimeSpan = TimeSpan.FromMinutes(1);
}