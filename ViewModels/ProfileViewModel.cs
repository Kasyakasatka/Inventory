using InventoryManagement.Web.DTOs;
using System.Collections.Generic;

namespace InventoryManagement.Web.ViewModels
{
    public class ProfileViewModel
    {
        public required IEnumerable<InventoryViewDTO> OwnedInventories { get; set; }
        public required IEnumerable<InventoryViewDTO> WriteAccessInventories { get; set; }
    }
}