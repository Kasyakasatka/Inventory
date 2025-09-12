using System.ComponentModel.DataAnnotations;

namespace InventoryManagement.Web.DTOs;

public class ResetPasswordDTO
{
    public required string Email { get; set; }
    public required string Code { get; set; }
    public required string Password { get; set; }
    public required string ConfirmPassword { get; set; }
}