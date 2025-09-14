using InventoryManagement.Web.Data.Models;

namespace InventoryManagement.Web.ViewModels
{
    public class ItemDetailsViewModel
    {
        public required Item Item { get; set; }
        public required IEnumerable<Comment> Comments { get; set; }
        public required bool HasWriteAccess { get; set; }
        public required int LikesCount { get; set; }
        public required bool IsLikedByUser { get; set; }
    }
}
