using InventoryManagement.Web.Data.Models;
using InventoryManagement.Web.DTOs;

namespace InventoryManagement.Web.Services.Abstractions;

public interface IItemLikeCommentService
{
    Task<IEnumerable<Comment>> GetCommentsAsync(Guid itemId);
    Task<Guid> AddCommentAsync(Guid itemId, Guid userId, CommentDTO commentDto);
    Task<bool> AddOrRemoveLikeAsync(Guid itemId, Guid userId);
    Task<bool> CheckIfLikedAsync(Guid itemId, Guid userId);
    Task<int> GetLikesCountAsync(Guid itemId);
}