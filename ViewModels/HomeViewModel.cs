using InventoryManagement.Web.DTOs;

namespace InventoryManagement.Web.ViewModels
{
    public class HomeViewModel
    {
        public required IEnumerable<InventoryViewDTO> LatestInventories { get; set; }
        public required IEnumerable<InventoryViewDTO> PopularInventories { get; set; }
    }
}
