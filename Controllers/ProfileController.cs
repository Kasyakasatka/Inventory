using InventoryManagement.Web.Services.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using InventoryManagement.Web.ViewModels;
using Microsoft.AspNetCore.Identity;
using InventoryManagement.Web.Data.Models;
using InventoryManagement.Web.DTOs;
using InventoryManagement.Web.Services.Implementations;

namespace InventoryManagement.Web.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly IUserService _userService;
        private readonly ILogger<ProfileController> _logger;
        private readonly ISalesforceService _salesforceService;
        private readonly UserManager<User> _userManager;
        private readonly ICurrentUserService _currentUserService;


        public ProfileController(
            IUserService userService,
            ILogger<ProfileController> logger,
            ISalesforceService salesforceService,
            UserManager<User> userManager,
            ICurrentUserService currentUserService)
        {
            _userService = userService;
            _logger = logger;
            _salesforceService = salesforceService;
            _userManager = userManager;
            _currentUserService = currentUserService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var currentUserId = _currentUserService.GetCurrentUserId();
            var currentUser = await _userManager.GetUserAsync(User);
            _logger.LogInformation("User {UserId} is viewing their profile page.", currentUserId);
            var ownedInventories = await _userService.GetOwnedInventoriesAsync(currentUserId);
            var writeAccessInventories = await _userService.GetWriteAccessInventoriesAsync(currentUserId);
            var viewModel = new ProfileViewModel
            {
                OwnedInventories = ownedInventories,
                WriteAccessInventories = writeAccessInventories,
                IsSalesforceConnected = currentUser?.IsSalesforceConnected ?? false,
                SalesforceProfile = new SalesforceCreateProfileDTO
                {
                    Email = currentUser?.Email ?? string.Empty,
                    FirstName = string.Empty,
                    LastName = string.Empty,
                    CompanyName = string.Empty
                }
            };
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult StartSalesforceConnect(ProfileViewModel model)
        {
            var authUrl = _salesforceService.GenerateAuthorizationUrl(model.SalesforceProfile, out _);
            return Redirect(authUrl);
        }

        [HttpGet]
        public async Task<IActionResult> SalesforceCallback(string code, string state)
        {
            _logger.LogInformation("Salesforce callback received. Code: {Code}, State: {State}", code, state);
            if (string.IsNullOrEmpty(code))
            {
                _logger.LogError("No code returned from Salesforce. Make sure the callback URL in Connected App matches exactly.");
                return RedirectToAction(nameof(Index));
            }
            try
            {
                var redirectUri = Url.Action(nameof(SalesforceCallback), "Profile", null, Request.Scheme);
                var accessToken = await _salesforceService.ExchangeAuthCodeForTokenAsync(code, redirectUri, null);
                var profile = JsonSerializer.Deserialize<SalesforceCreateProfileDTO>(System.Web.HttpUtility.UrlDecode(state));
                await _salesforceService.CreateAccountWithContactAsync(profile!, accessToken);
                _logger.LogInformation("Profile created successfully for {Email}", profile?.Email);
                TempData["SuccessMessage"] = "Profile created successfully";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Integration failed");
                TempData["ErrorMessage"] = "Integration failed";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
