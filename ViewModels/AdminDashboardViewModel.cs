using InventoryManagement.Web.DTOs;

namespace InventoryManagement.Web.ViewModels
{
    public class AdminDashboardViewModel
    {
        public required IEnumerable<UserManagementDTO> Users { get; set; }
    }
}
