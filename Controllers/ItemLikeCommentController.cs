using InventoryManagement.Web.DTOs;
using InventoryManagement.Web.Services.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FluentValidation;

namespace InventoryManagement.Web.Controllers;

public class ItemLikeCommentController : Controller
{
    private readonly IItemLikeCommentService _interactionService;
    private readonly ILogger<ItemLikeCommentController> _logger;
    private readonly ICurrentUserService _currentUserService;
    private readonly IValidator<CommentDTO> _commentDtoValidator;

    public ItemLikeCommentController(
        IItemLikeCommentService interactionService,
        ILogger<ItemLikeCommentController> logger,
        ICurrentUserService currentUserService,
        IValidator<CommentDTO> commentDtoValidator)
    {
        _interactionService = interactionService;
        _logger = logger;
        _currentUserService = currentUserService;
        _commentDtoValidator = commentDtoValidator;
    }

    [HttpPost("Items/{itemId}/AddComment")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddComment(Guid itemId, [FromBody] string text)
    {
        _logger.LogInformation("Attempting to add comment to item {ItemId}.", itemId);
        CommentDTO commentDto=  new CommentDTO() { ItemId  = itemId, Text = text }; 
        var validationResult = await _commentDtoValidator.ValidateAsync(commentDto);
        if (!validationResult.IsValid)
        {
            _logger.LogWarning("Add comment validation failed for item {ItemId}. Errors: {@Errors}", itemId, validationResult.Errors);
            return RedirectToAction("Details", "Item", new { id = itemId });
        }
        var userId = _currentUserService.GetCurrentUserId();
        await _interactionService.AddCommentAsync(itemId, userId, commentDto);
        _logger.LogInformation("Comment added successfully to item {ItemId} by user {UserId}.", itemId, userId);
        return RedirectToAction("Details", "Item", new { id = itemId });
    }

    [HttpPost]
    [Route("Items/{itemId}/ToggleLike")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleLike(Guid itemId)
    {
        var userId = _currentUserService.GetCurrentUserId();
        _logger.LogInformation("User {UserId} is toggling like for item {ItemId}.", userId, itemId);
        await _interactionService.AddOrRemoveLikeAsync(itemId, userId);
        _logger.LogInformation("Like toggled successfully for item {ItemId}.", itemId);
        return RedirectToAction("Details", "Item", new { id = itemId, tab = "discussion" });
    }
}