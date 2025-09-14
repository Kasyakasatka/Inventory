namespace InventoryManagement.Web.DTOs;

public class SalesforceAccountDTO
{
    public required string Name { get; set; }
}

public class SalesforceContactDTO
{
    public required string LastName { get; set; }
    public string? FirstName { get; set; }
    public string? Email { get; set; }
    public string? AccountId { get; set; }
}

public class SalesforceCreateProfileDTO
{
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string Email { get; set; }
    public required string CompanyName { get; set; }
    public string? AccountSite { get; set; }
    public string? Phone { get; set; }
}