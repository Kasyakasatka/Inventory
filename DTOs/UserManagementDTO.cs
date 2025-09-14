namespace InventoryManagement.Web.DTOs;

public class UserManagementDTO
{
    public required Guid Id { get; set; }
    public required string Username { get; set; }
    public required string Email { get; set; }
    public required bool IsAdmin { get; set; }
    public required bool IsLockedOut { get; set; }
}