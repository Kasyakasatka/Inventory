using System;
using System.ComponentModel.DataAnnotations;

namespace InventoryManagement.Web.DTOs;

public class ConfirmEmailDTO
{
    public required Guid UserId { get; set; }
    public required string Code { get; set; }
}