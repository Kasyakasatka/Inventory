namespace InventoryManagement.Web.DTOs
{

    public class FieldStatsDTO
    {
        public required Guid FieldDefinitionId { get; set; }
        public required string Title { get; set; }
        public required string Type { get; set; }
        public int? MinValue { get; set; }
        public int? MaxValue { get; set; }
        public double? AverageValue { get; set; }
        public required IEnumerable<string> MostPopularValues { get; set; }

    }
}
