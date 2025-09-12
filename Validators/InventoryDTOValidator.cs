using FluentValidation;
using InventoryManagement.Web.DTOs;
using InventoryManagement.Web.Constants;

namespace InventoryManagement.Web.Validators;

public class InventoryDTOValidator : AbstractValidator<InventoryDTO>
{
    public InventoryDTOValidator()
    {
        RuleFor(dto => dto.Title)
            .NotEmpty()
            .WithMessage("Title is required.")
            .MaximumLength(ValidationConstants.TitleMaxLength)
            .WithMessage($"Title must not exceed {ValidationConstants.TitleMaxLength} characters.");

        RuleFor(dto => dto.Description)
            .MaximumLength(ValidationConstants.DescriptionMaxLength)
            .When(dto => !string.IsNullOrEmpty(dto.Description))
            .WithMessage($"Description must not exceed {ValidationConstants.DescriptionMaxLength} characters.");
            
        RuleFor(dto => dto.ImageUrl)
            .MaximumLength(ValidationConstants.ImageUrlMaxLength)
            .When(dto => !string.IsNullOrEmpty(dto.ImageUrl))
            .WithMessage($"Image URL must not exceed {ValidationConstants.ImageUrlMaxLength} characters.")
            .Must(BeAValidUrl)
            .When(dto => !string.IsNullOrEmpty(dto.ImageUrl))
            .WithMessage("Image URL is not a valid URL.");

        RuleFor(dto => dto.CategoryId)
            .NotEmpty().WithMessage("Category is required.");

        RuleFor(dto => dto.Tags)
            .Must(tags => tags == null || tags.Count <= ValidationConstants.TagsCount)
            .When(dto => dto.Tags != null)
            .WithMessage($"You cannot have more than {ValidationConstants.TagsCount} tags.");

        RuleFor(dto => dto.CustomIdFormat)
            .MaximumLength(ValidationConstants.CustomIdFormatMaxLength)
            .WithMessage($"Custom ID format must not exceed {ValidationConstants.CustomIdFormatMaxLength} characters.");
       
        RuleForEach(dto => dto.FieldDefinitions)
            .SetValidator(new FieldDefinitionDTOValidator());
    }
    private static bool BeAValidUrl(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out _);
    }
}