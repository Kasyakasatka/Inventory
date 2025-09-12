using AutoMapper;
using InventoryManagement.Web.Data;
using InventoryManagement.Web.Data.Configurations;
using InventoryManagement.Web.Data.Models.Enums;
using InventoryManagement.Web.DTOs;
using InventoryManagement.Web.Services.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Linq;

namespace InventoryManagement.Web.Services.Implementations;

public class InventoryStatsService : IInventoryStatsService
{
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<InventoryStatsService> _logger;
    private readonly AppConfiguration _appConfig;

    public InventoryStatsService(ApplicationDbContext context, IMapper mapper, ILogger<InventoryStatsService> logger, IOptions<AppConfiguration> appConfig)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
        _appConfig = appConfig.Value;
    }

    public async Task<InventoryStatsDTO> GetInventoryStatisticsAsync(Guid inventoryId)
    {
        _logger.LogInformation("Calculating statistics for inventory {InventoryId}.", inventoryId);
        var inventory = await _context.Inventories
            .AsNoTracking()
            .Include(i => i.FieldDefinitions)
            .FirstOrDefaultAsync(i => i.Id == inventoryId);
        if (inventory == null)
        {
            _logger.LogWarning("Statistics failed: Inventory {InventoryId} not found.", inventoryId);
            throw new InvalidOperationException("Inventory not found.");
        }
        var totalItems = await _context.Items.CountAsync(i => i.InventoryId == inventoryId);
        var allCustomFieldValues = await _context.CustomFields
            .Where(cf => cf.Item.InventoryId == inventoryId)
            .GroupBy(cf => cf.FieldDefinitionId)
            .Select(g => new
            {
                FieldDefinitionId = g.Key,
                IntValues = g.Where(v => v.IntValue.HasValue).Select(v => v.IntValue!.Value).ToList(),
                StringValues = g.Where(v => v.StringValue != null).Select(v => v.StringValue!).ToList(),
                BoolValues = g.Where(v => v.BoolValue.HasValue).Select(v => v.BoolValue!.Value).ToList(),
            })
            .ToListAsync();
        var fieldStatsList = _mapper.Map<List<FieldStatsDTO>>(inventory.FieldDefinitions);
        foreach (var definition in inventory.FieldDefinitions)
        {
            var fieldStats = fieldStatsList.FirstOrDefault(s => s.FieldDefinitionId == definition.Id);
            var customFieldData = allCustomFieldValues.FirstOrDefault(d => d.FieldDefinitionId == definition.Id);
            if (fieldStats == null || customFieldData == null) continue;
            switch (definition.Type)
            {
                case FieldType.Int:
                    if (customFieldData.IntValues.Any())
                    {
                        fieldStats.MinValue = customFieldData.IntValues.Min();
                        fieldStats.MaxValue = customFieldData.IntValues.Max();
                        fieldStats.AverageValue = customFieldData.IntValues.Average();
                    }
                    break;
                case FieldType.String:
                    fieldStats.MostPopularValues = customFieldData.StringValues
                        .GroupBy(v => v)
                        .OrderByDescending(g => g.Count())
                        .Take(_appConfig.MostPopularValuesCount)
                        .Select(g => g.Key)
                        .ToList();
                    break;
                case FieldType.Bool:
                    var trueCount = customFieldData.BoolValues.Count(v => v);
                    var falseCount = customFieldData.BoolValues.Count(v => !v);
                    fieldStats.MostPopularValues = new List<string> {
                        $"True: {trueCount}",
                        $"False: {falseCount}"
                    };
                    break;
            }
        }
        return new InventoryStatsDTO
        {
            TotalItems = totalItems,
            FieldStatistics = fieldStatsList
        };
    }
}