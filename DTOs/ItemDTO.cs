using System.Collections.Generic;

namespace InventoryManagement.Web.DTOs;

public class ItemDTO
{
    public  string? CustomId { get; set; }
    public required List<CustomFieldValueDTO> CustomFields { get; set; }
    public uint Version { get; internal set; }
}