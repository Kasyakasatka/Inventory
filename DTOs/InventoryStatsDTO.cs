using System;
using System.Collections.Generic;

namespace InventoryManagement.Web.DTOs;

public class InventoryStatsDTO
{
    public required int TotalItems { get; set; }
    public required IEnumerable<FieldStatsDTO> FieldStatistics { get; set; }
}
