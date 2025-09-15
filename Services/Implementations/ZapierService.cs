using InventoryManagement.Web.Data.Configurations;
using InventoryManagement.Web.DTOs;
using InventoryManagement.Web.Services.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System;

namespace InventoryManagement.Web.Services.Implementations
{
    public class ZapierService : IZapierService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ZapierService> _logger;
        private readonly IConfiguration _configuration;

        public ZapierService(HttpClient httpClient, ILogger<ZapierService> logger, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<bool> SendSupportTicketAsync(SupportTicketDTO ticket)
        {
            try
            {
                string jsonContent = JsonSerializer.Serialize(ticket);
                string webhookUrl = _configuration["ZapierSettings:WebhookUrl"];
                if (string.IsNullOrEmpty(webhookUrl))
                {
                    _logger.LogError("Zapier Webhook URL is not configured.");
                    return false;
                }
                var response = await _httpClient.PostAsync(webhookUrl, new StringContent(jsonContent, Encoding.UTF8, "application/json"));
                response.EnsureSuccessStatusCode();
                _logger.LogInformation("Support ticket submitted to Zapier successfully.");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while sending support ticket to Zapier.");
                return false;
            }
        }
    }
}