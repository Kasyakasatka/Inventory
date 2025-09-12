using System.ComponentModel.DataAnnotations;

namespace InventoryManagement.Web.DTOs
{
    public class LoginDTO
    {
        public required string Username { get; set; }
        public required string Password { get; set; }
        public required string Email { get; set; }
        public bool RememberMe { get; set; }
    }
}
