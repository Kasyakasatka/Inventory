using InventoryManagement.Web.DTOs;
using System.Threading.Tasks;

namespace InventoryManagement.Web.Services.Abstractions
{
    public interface IZapierService
    {
        Task<bool> SendSupportTicketAsync(SupportTicketDTO ticket);
    }
}