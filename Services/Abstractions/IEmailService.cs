namespace InventoryManagement.Web.Services.Abstractions;

public interface IEmailService
{
    Task SendEmailAsync(string toEmail, string subject, string body);
}