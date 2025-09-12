using FluentValidation;
using InventoryManagement.Web.DTOs;
using InventoryManagement.Web.Constants;
using InventoryManagement.Web.Data;

namespace InventoryManagement.Web.Validators;

public class ItemDTOValidator : AbstractValidator<ItemDTO>
{
    public ItemDTOValidator(ApplicationDbContext context)
    {
        RuleFor(dto => dto.CustomId)
            .MaximumLength(ValidationConstants.CustomIdMaxLength)
            .WithMessage($"Custom ID must not exceed {ValidationConstants.CustomIdMaxLength} characters.")
            .When(dto => !string.IsNullOrEmpty(dto.CustomId));

        RuleForEach(dto => dto.CustomFields).SetValidator(new CustomFieldValueDTOValidator(context));
    }

}
