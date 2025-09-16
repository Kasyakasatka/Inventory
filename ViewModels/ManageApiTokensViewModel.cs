using InventoryManagement.Web.Data.Models;
using System;
using System.Collections.Generic;

namespace InventoryManagement.Web.ViewModels;

public class ManageApiTokensViewModel
{
    public required Guid InventoryId { get; set; }
    public required string InventoryTitle { get; set; }
    public required IEnumerable<ApiToken> Tokens { get; set; }
}