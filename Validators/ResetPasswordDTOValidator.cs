using FluentValidation;
using InventoryManagement.Web.DTOs;
using InventoryManagement.Web.Models.Configurations;
using Microsoft.Extensions.Options;

namespace InventoryManagement.Web.Validators;

public class ResetPasswordDTOValidator : AbstractValidator<ResetPasswordDTO>
{
    private readonly IOptions<IdentityConfig> _identityOptions;

    public ResetPasswordDTOValidator(IOptions<IdentityConfig> identityOptions)
    {
        _identityOptions = identityOptions;

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email is required.");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Token is required.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("New password is required.")
            .MinimumLength(_identityOptions.Value.Password.RequiredLength).WithMessage($"Password must be at least {_identityOptions.Value.Password.RequiredLength} characters long.");
    }
}