using InventoryManagement.Web.Data.Models;

namespace InventoryManagement.Web.ViewModels
{
    public class ItemInteractionsViewModel
    {
        public required Guid ItemId { get; set; }
        public required IEnumerable<Comment> Comments { get; set; }
        public required int LikesCount { get; set; }
        public required bool IsLikedByUser { get; set; }
    }

}
