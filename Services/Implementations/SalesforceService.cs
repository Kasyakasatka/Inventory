using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using InventoryManagement.Web.Data.Configurations;
using InventoryManagement.Web.DTOs;
using InventoryManagement.Web.Services.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace InventoryManagement.Web.Services.Implementations
{
    public class SalesforceService : ISalesforceService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<SalesforceService> _logger;
        private readonly SalesforceSettings _settings;

        public SalesforceService(HttpClient httpClient, IOptions<SalesforceSettings> settings, ILogger<SalesforceService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _settings = settings.Value;
        }
        public string GenerateAuthorizationUrl(SalesforceCreateProfileDTO profile, out string unused)
        {
            unused = null; 
            var state = System.Web.HttpUtility.UrlEncode(JsonSerializer.Serialize(profile));
            var url = $"{_settings.AuthUrl}/authorize" +
                      $"?response_type=code" +
                      $"&client_id={_settings.ClientId}" +
                      $"&redirect_uri={System.Web.HttpUtility.UrlEncode(_settings.RedirectUri)}" +
                      $"&state={state}" +
                      $"&prompt=consent" +
                      $"&scope=api refresh_token openid";
            _logger.LogInformation("Generated Salesforce auth URL: {Url}", url);
            return url;
        }

        public async Task<string> ExchangeAuthCodeForTokenAsync(string code, string redirectUri, string unused)
        {
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "authorization_code"),
                new KeyValuePair<string, string>("client_id", _settings.ClientId),
                new KeyValuePair<string, string>("client_secret", _settings.ClientSecret),
                new KeyValuePair<string, string>("redirect_uri", redirectUri),
                new KeyValuePair<string, string>("code", code)
            });
            var response = await _httpClient.PostAsync($"{_settings.AuthUrl}/token", content);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            var tokenResponse = JsonSerializer.Deserialize<JsonElement>(json);
            var accessToken = tokenResponse.GetProperty("access_token").GetString()!;
            _logger.LogInformation("Received Salesforce access token.");
            return accessToken;
        }

        public async Task CreateAccountWithContactAsync(SalesforceCreateProfileDTO profile, string accessToken)
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            var account = new {
                Name = profile.CompanyName,
                Site = profile.AccountSite,
                Phone = profile.Phone 
            };
            var accountJson = JsonSerializer.Serialize(account);
            var accountResp = await _httpClient.PostAsync(
                $"{_settings.InstanceUrl}/services/data/v60.0/sobjects/Account",
                new StringContent(accountJson, Encoding.UTF8, "application/json")
            );
            var accountResult = JsonSerializer.Deserialize<JsonElement>(await accountResp.Content.ReadAsStringAsync());
            if (!accountResp.IsSuccessStatusCode || (accountResult.TryGetProperty("success", out var success) && !success.GetBoolean()))
            {
                var errorMessages = GetErrorMessages(accountResult);
                _logger.LogError("Failed to create Account in Salesforce. Errors: {Errors}", string.Join(", ", errorMessages));
                throw new InvalidOperationException($"Failed to create Account: {string.Join(", ", errorMessages)}");
            }
            var accountId = accountResult.GetProperty("id").GetString();
            _logger.LogInformation("Created Salesforce Account with ID: {AccountId}", accountId);
            var contact = new
            {
                FirstName = profile.FirstName,
                LastName = profile.LastName,
                Email = profile.Email,
                AccountId = accountId
            };
            var contactJson = JsonSerializer.Serialize(contact);
            var contactResp = await _httpClient.PostAsync(
                $"{_settings.InstanceUrl}/services/data/v60.0/sobjects/Contact",
                new StringContent(contactJson, Encoding.UTF8, "application/json")
            );
            var contactResult = JsonSerializer.Deserialize<JsonElement>(await contactResp.Content.ReadAsStringAsync());
            if (!contactResp.IsSuccessStatusCode || (contactResult.TryGetProperty("success", out var contactSuccess) && !contactSuccess.GetBoolean()))
            {
                var errorMessages = GetErrorMessages(contactResult);
                _logger.LogError("Failed to create Contact in Salesforce. Errors: {Errors}", string.Join(", ", errorMessages));
                throw new InvalidOperationException($"Failed to create Contact: {string.Join(", ", errorMessages)}");
            }
            _logger.LogInformation("Created Salesforce Contact for {Email}", profile.Email);
        }

        private IEnumerable<string> GetErrorMessages(JsonElement response)
        {
            var errors = new List<string>();
            if (response.TryGetProperty("errors", out var errorsElement) && errorsElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var errorElement in errorsElement.EnumerateArray())
                {
                    if (errorElement.TryGetProperty("message", out var messageElement))
                    {
                        errors.Add(messageElement.GetString()!);
                    }
                }
            }
            else if (response.TryGetProperty("message", out var singleMessage))
            {
                errors.Add(singleMessage.GetString()!);
            }
            return errors;
        }
    }
}