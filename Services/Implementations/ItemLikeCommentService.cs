using AutoMapper;
using InventoryManagement.Web.Data;
using InventoryManagement.Web.Data.Models;
using InventoryManagement.Web.DTOs;
using InventoryManagement.Web.Exceptions;
using InventoryManagement.Web.Hubs;
using InventoryManagement.Web.Services.Abstractions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace InventoryManagement.Web.Services.Implementations;

public class ItemLikeCommentService : IItemLikeCommentService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ItemLikeCommentService> _logger;
    private readonly IHubContext<CommentsHub> _hubContext;
    public ItemLikeCommentService(ApplicationDbContext context, ILogger<ItemLikeCommentService> logger, IHubContext<CommentsHub> hubContext)
    {
        _context = context;
        _logger = logger;
        _hubContext = hubContext;
    }
    public async Task<IEnumerable<Comment>> GetCommentsAsync(Guid itemId)
    {
        _logger.LogInformation("Retrieving comments for item {ItemId}.", itemId);
        return await _context.Comments
            .Include(c => c.User) 
            .Where(c => c.ItemId == itemId)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync();
    }

    public async Task<Guid> AddCommentAsync(Guid itemId, Guid userId, CommentDTO commentDto)
    {
        _logger.LogInformation("User {UserId} is adding a comment to item {ItemId}.", userId, itemId);
        var item = await _context.Items.FindAsync(itemId);
        if (item == null)
        {
            _logger.LogWarning("Add comment failed: Item {ItemId} not found.", itemId);
            throw new NotFoundException($"Item with ID {itemId} not found.");
        }
        var comment = new Comment
        {
            Id = Guid.NewGuid(),
            ItemId = itemId,
            UserId = userId,
            Text = commentDto.Text,
            CreatedAt = DateTime.UtcNow
        };
        _context.Comments.Add(comment);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Comment {CommentId} added successfully to item {ItemId}.", comment.Id, itemId);
        await _context.Entry(comment).Reference(c => c.User).LoadAsync();

        await _hubContext.Clients.Group(itemId.ToString()).SendAsync("ReceiveComment",
            comment.User?.UserName ?? "Anonymous",
            comment.Text,
            comment.CreatedAt.ToString("g"));
        return comment.Id;
    }

    public async Task<bool> AddOrRemoveLikeAsync(Guid itemId, Guid userId)
    {
        _logger.LogInformation("User {UserId} is attempting to like or unlike item {ItemId}.", userId, itemId);
        var existingLike = await _context.Likes
            .FirstOrDefaultAsync(l => l.ItemId == itemId && l.UserId == userId);
        if (existingLike == null)
        {
            var like = new Like 
            { 
                Id = Guid.NewGuid(),
                ItemId = itemId, 
                UserId = userId,
                
            };
            _context.Likes.Add(like);
            await _context.SaveChangesAsync();
            _logger.LogInformation("User {UserId} liked item {ItemId}.", userId, itemId);
            return true; 
        }
        else
        {
            _context.Likes.Remove(existingLike);
            await _context.SaveChangesAsync();
            _logger.LogInformation("User {UserId} unliked item {ItemId}.", userId, itemId);
            return false;
        }
    }
    public async Task<bool> CheckIfLikedAsync(Guid itemId, Guid userId)
    {
        return await _context.Likes.AnyAsync(l => l.ItemId == itemId && l.UserId == userId);
    }

    public async Task<int> GetLikesCountAsync(Guid itemId)
    {
        return await _context.Likes.CountAsync(l => l.ItemId == itemId);
    }
}