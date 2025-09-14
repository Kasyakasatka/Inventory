using InventoryManagement.Web.DTOs;

namespace InventoryManagement.Web.ViewModels
{
    public class UserSearchResultsViewModel
    {
        public required string Query { get; set; }
        public required IEnumerable<UserSearchDTO> FoundUsers { get; set; }
    }
}
