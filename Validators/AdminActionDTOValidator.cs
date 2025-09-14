using FluentValidation;
using InventoryManagement.Web.DTOs;
using System;

namespace InventoryManagement.Web.Validators;

public class AdminActionDTOValidator : AbstractValidator<AdminActionDTO>
{
    public AdminActionDTOValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID cannot be empty.");
    }
}