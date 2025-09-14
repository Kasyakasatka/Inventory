using InventoryManagement.Web.Data.Models.Enums;
using System.Text.Json.Serialization;

namespace InventoryManagement.Web.Data.Models;

public class IdElement
{
    public required IdElementType Type { get; set; }
    public string? Value { get; set; }
    public string? Format { get; set; }
}