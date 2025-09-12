using FluentValidation;
using InventoryManagement.Web.DTOs;
using InventoryManagement.Web.Constants;
using System.Text.RegularExpressions;
using InventoryManagement.Web.Models.Configurations;

namespace InventoryManagement.Web.Validators;

public class FieldDefinitionDTOValidator : AbstractValidator<FieldDefinitionDTO>
{
    public FieldDefinitionDTOValidator()
    {
        RuleFor(f => f.Title)
            .NotEmpty().WithMessage("Field title cannot be empty.")
            .MaximumLength(ValidationConstants.FieldTitleMaxLength).WithMessage($"Field title cannot exceed {ValidationConstants.FieldTitleMaxLength} characters.");

        RuleFor(f => f.Type)
            .IsInEnum().WithMessage("Invalid field type.");

        RuleFor(f => f.Description)
            .MaximumLength(ValidationConstants.FieldDescriptionMaxLength).WithMessage($"Description cannot exceed {ValidationConstants.FieldDescriptionMaxLength} characters.")
            .When(f => !string.IsNullOrEmpty(f.Description));

        RuleFor(f => f.ValidationRegex)
            .MaximumLength(ValidationConstants.FieldValidationRegexMaxLength).WithMessage($"Regex pattern cannot exceed {ValidationConstants.FieldValidationRegexMaxLength} characters.")
            .Must(IsValidRegex).When(f => !string.IsNullOrEmpty(f.ValidationRegex))
            .WithMessage("Invalid regular expression pattern.");

        RuleFor(f => f.ValidationMin)
            .MaximumLength(ValidationConstants.FieldValidationRangeMaxLength).WithMessage($"Validation Min value cannot exceed {ValidationConstants.FieldValidationRangeMaxLength} characters.")
            .When(f => !string.IsNullOrEmpty(f.ValidationMin));

        RuleFor(f => f.ValidationMax)
            .MaximumLength(ValidationConstants.FieldValidationRangeMaxLength).WithMessage($"Validation Max value cannot exceed {ValidationConstants.FieldValidationRangeMaxLength} characters.")
            .When(f => !string.IsNullOrEmpty(f.ValidationMax));

        RuleFor(f => f)
            .Must(BeAValidRange).When(f => !string.IsNullOrEmpty(f.ValidationMin) || !string.IsNullOrEmpty(f.ValidationMax))
            .WithMessage("Validation Min must be less than or equal to Validation Max.");
    }

    private static bool IsValidRegex(string regexPattern)
    {
        if (string.IsNullOrEmpty(regexPattern)) return true;
        try
        {
            _ = new Regex(regexPattern);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private static bool BeAValidRange(FieldDefinitionDTO dto)
    {
        if (!string.IsNullOrEmpty(dto.ValidationMin) && !int.TryParse(dto.ValidationMin, out _)) return false;
        if (!string.IsNullOrEmpty(dto.ValidationMax) && !int.TryParse(dto.ValidationMax, out _)) return false;
        if (!string.IsNullOrEmpty(dto.ValidationMin) && !string.IsNullOrEmpty(dto.ValidationMax))
        {
            return int.Parse(dto.ValidationMin) <= int.Parse(dto.ValidationMax);
        }
        return true;
    }
}