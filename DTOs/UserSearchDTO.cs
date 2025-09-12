using System;
using System.ComponentModel.DataAnnotations;

namespace InventoryManagement.Web.DTOs;

public class UserSearchDTO
{
    public required Guid Id { get; set; }
    public required string Username { get; set; }
    public required string Email { get; set; }
}