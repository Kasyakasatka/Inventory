using System.Text.Json.Serialization;

namespace InventoryManagement.Web.Models;

public class ErrorResponse
{
    public required string Message { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IDictionary<string, string[]>? Errors { get; set; }
}