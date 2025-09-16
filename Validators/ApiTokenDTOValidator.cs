using FluentValidation;
using InventoryManagement.Web.DTOs;
using InventoryManagement.Web.Constants;

namespace InventoryManagement.Web.Validators;

public class ApiTokenDTOValidator : AbstractValidator<ApiTokenDTO>
{
    public ApiTokenDTOValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Token cannot be empty.")
            .MaximumLength(ValidationConstants.TokenMaxLength).WithMessage($"Token must not exceed {ValidationConstants.TokenMaxLength} characters.");
    }
}