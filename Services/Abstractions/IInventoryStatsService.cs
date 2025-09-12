using InventoryManagement.Web.DTOs;
using System;
using System.Threading.Tasks;

namespace InventoryManagement.Web.Services.Abstractions;

public interface IInventoryStatsService
{
    Task<InventoryStatsDTO> GetInventoryStatisticsAsync(Guid inventoryId);
}