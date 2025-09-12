using FluentValidation;
using InventoryManagement.Web.Data;
using InventoryManagement.Web.DTOs;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using InventoryManagement.Web.Models.Configurations;
using InventoryManagement.Web.Data.Models;
using InventoryManagement.Web.Data.Models.Enums;

namespace InventoryManagement.Web.Validators;

public class CustomFieldValueDTOValidator : AbstractValidator<CustomFieldValueDTO>
{
    private readonly ApplicationDbContext _context;

    public CustomFieldValueDTOValidator(ApplicationDbContext context)
    {
        _context = context;
        RuleFor(x => x)
            .MustAsync(async (dto, cancellation) =>
            {
                var fieldDefinition = await _context.FieldDefinitions
                    .AsNoTracking()
                    .FirstOrDefaultAsync(fd => fd.Id == dto.FieldDefinitionId, cancellation);
                if (fieldDefinition == null) return false;
                switch (fieldDefinition.Type)
                {
                    case FieldType.String:
                        return ValidateStringValue(dto.StringValue, fieldDefinition);
                    case FieldType.Int:
                        return ValidateIntValue(dto.IntValue, fieldDefinition);
                    case FieldType.Bool:
                        return ValidateBoolValue(dto.BoolValue, fieldDefinition);
                    default:
                        return false;
                }
            })
            .WithMessage("Validation failed for one or more custom fields.");
    }

    private static bool ValidateStringValue(string? value, FieldDefinition definition)
    {
        if (definition.IsRequired && string.IsNullOrEmpty(value)) return false;
        if (!string.IsNullOrEmpty(definition.ValidationRegex) && !string.IsNullOrEmpty(value))
        {
            return Regex.IsMatch(value, definition.ValidationRegex);
        }
        return true;
    }

    private static bool ValidateIntValue(int? value, FieldDefinition definition)
    {
        if (definition.IsRequired && !value.HasValue) return false;
        if (value.HasValue)
        {
            if (!string.IsNullOrEmpty(definition.ValidationMin) && value.Value < int.Parse(definition.ValidationMin)) return false;
            if (!string.IsNullOrEmpty(definition.ValidationMax) && value.Value > int.Parse(definition.ValidationMax)) return false;
        }
        return true;
    }

    private static bool ValidateBoolValue(bool? value, FieldDefinition definition)
    {
        if (definition.IsRequired && !value.HasValue) return false;
        return true;
    }
}