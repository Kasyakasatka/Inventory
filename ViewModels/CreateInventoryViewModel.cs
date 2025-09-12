using InventoryManagement.Web.Data.Models;
using InventoryManagement.Web.DTOs;

namespace InventoryManagement.Web.ViewModels
{

    public class CreateInventoryViewModel
    {
        public required InventoryDTO InventoryDTO { get; set; }
        public required IEnumerable<Category> Categories { get; set; }
        public string? ErrorMessage { get; set; }
        
    }
}
