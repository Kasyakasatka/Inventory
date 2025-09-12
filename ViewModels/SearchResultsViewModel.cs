using InventoryManagement.Web.Data.Models;
using InventoryManagement.Web.DTOs;

namespace InventoryManagement.Web.ViewModels
{
    public class SearchResultsViewModel
    {
        public required string Query { get; set; }
        public required IEnumerable<InventoryViewDTO> FoundInventories { get; set; }
        public required IEnumerable<Item> FoundItems { get; set; }
    }
}
