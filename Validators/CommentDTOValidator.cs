using FluentValidation;
using InventoryManagement.Web.DTOs;
using InventoryManagement.Web.Constants;

namespace InventoryManagement.Web.Validators;

public class CommentDTOValidator : AbstractValidator<CommentDTO>
{
    public CommentDTOValidator()
    {
        RuleFor(x => x.Text)
            .NotEmpty().WithMessage("Comment text cannot be empty.")
            .MaximumLength(ValidationConstants.CommentTextMaxLength).WithMessage($"Comment text must not exceed {ValidationConstants.CommentTextMaxLength} characters.");
    }
}