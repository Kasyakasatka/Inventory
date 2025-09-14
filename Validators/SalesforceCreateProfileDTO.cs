using FluentValidation;
using InventoryManagement.Web.DTOs;
using InventoryManagement.Web.Constants;

namespace InventoryManagement.Web.Validators;

public class SalesforceCreateProfileDTOValidator : AbstractValidator<SalesforceCreateProfileDTO>
{
    public SalesforceCreateProfileDTOValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required.")
            .MaximumLength(ValidationConstants.NameMaxLength).WithMessage($"First name must not exceed {ValidationConstants.NameMaxLength} characters.");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required.")
            .MaximumLength(ValidationConstants.NameMaxLength).WithMessage($"Last name must not exceed {ValidationConstants.NameMaxLength} characters.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.");

        RuleFor(x => x.CompanyName)
            .NotEmpty().WithMessage("Company name is required.")
            .MaximumLength(ValidationConstants.CompanyNameMaxLength).WithMessage($"Company name must not exceed {ValidationConstants.CompanyNameMaxLength} characters.");
    }
}