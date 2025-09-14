using System.Text.Json.Serialization;

namespace InventoryManagement.Web.Data.Models.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum IdElementType
{
    FixedText,
    Random,
    Sequence,
    DateTime,
    Guid
}