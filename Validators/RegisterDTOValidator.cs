using FluentValidation;
using InventoryManagement.Web.DTOs;
using InventoryManagement.Web.Models.Configurations;
using Microsoft.Extensions.Options;

public class RegisterDTOValidator : AbstractValidator<RegisterDTO>
{
    private readonly IOptions<IdentityConfig> _identityOptions;

    public RegisterDTOValidator(IOptions<IdentityConfig> identityOptions)
    {
        _identityOptions = identityOptions;

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email is required.");

        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Username is required.")
            .MinimumLength(_identityOptions.Value.User.MinLength).WithMessage($"Username must be at least {_identityOptions.Value.User.MinLength} characters long.")
            .MaximumLength(_identityOptions.Value.User.MaxLength).WithMessage($"Username must not exceed {_identityOptions.Value.User.MaxLength} characters.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(_identityOptions.Value.Password.RequiredLength).WithMessage($"Password must be at least {_identityOptions.Value.Password.RequiredLength} characters long.");
    }
}