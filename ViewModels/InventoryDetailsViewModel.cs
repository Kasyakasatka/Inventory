using InventoryManagement.Web.Data.Models;
using InventoryManagement.Web.DTOs;
using System.ComponentModel;

namespace InventoryManagement.Web.ViewModels
{
    public class InventoryDetailsViewModel
    {
        public required Inventory Inventory { get; set; }
        public required IEnumerable<Item> Items { get; set; }
        public required bool HasWriteAccess { get; set; }
        public required bool IsAdmin { get; set; }
        public required InventoryStatsDTO Statistics { get; set; }
        public required bool IsOwnerOrAdmin { get; set; }
        public required IEnumerable<Comment> AllComments { get; set; }
        public List<FieldDefinitionDTO>? FieldDefinitions { get; set; }
    }
}
