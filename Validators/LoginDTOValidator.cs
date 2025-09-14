using FluentValidation;
using InventoryManagement.Web.DTOs;

namespace InventoryManagement.Web.Validators;

public class LoginDTOValidator : AbstractValidator<LoginDTO>
{
    public LoginDTOValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Invalid email address.");
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.");
    }
}