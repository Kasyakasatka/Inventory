using System;
using System.ComponentModel.DataAnnotations;

namespace InventoryManagement.Web.DTOs;

public class AdminActionDTO
{
    public required Guid UserId { get; set; }
}