using InventoryManagement.Web.DTOs;

public interface ISalesforceService
{
    string GenerateAuthorizationUrl(SalesforceCreateProfileDTO profile, out string codeVerifier);
    Task<string> ExchangeAuthCodeForTokenAsync(string code, string redirectUri, string codeVerifier);
    Task CreateAccountWithContactAsync(SalesforceCreateProfileDTO userProfile, string accessToken);
}
