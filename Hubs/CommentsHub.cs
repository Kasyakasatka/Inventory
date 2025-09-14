using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace InventoryManagement.Web.Hubs;

public class CommentsHub : Hub
{
    public async Task SendComment(string itemId, string userName, string text, string createdAt)
    {
        await Clients.Group(itemId).SendAsync("ReceiveComment", userName, text, createdAt);
    }

    public async Task JoinItemDiscussion(string itemId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, itemId);
    }
}