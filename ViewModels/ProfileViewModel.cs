using InventoryManagement.Web.DTOs;
using System.Collections.Generic;

namespace InventoryManagement.Web.ViewModels
{
    public class ProfileViewModel
    {
        public  IEnumerable<InventoryViewDTO>? OwnedInventories { get; set; }
        public  IEnumerable<InventoryViewDTO>? WriteAccessInventories { get; set; }
        public SalesforceCreateProfileDTO? SalesforceProfile { get; set; }
        public bool IsSalesforceConnected { get; set; }
    }
}