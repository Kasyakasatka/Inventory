using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using InventoryManagement.Web.Models;
using InventoryManagement.Web.Data.Models;
using InventoryManagement.Web.ViewModels;
using InventoryManagement.Web.DTOs;
using InventoryManagement.Web.Services.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using System.Text.Json;
using System.Linq;
using System;
using System.Threading.Tasks;

namespace InventoryManagement.Web.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly UserManager<User> _userManager;
    private readonly IZapierService _zapierService;

    public HomeController(ILogger<HomeController> logger, UserManager<User> userManager, IZapierService zapierService)
    {
        _logger = logger;
        _userManager = userManager;
        _zapierService = zapierService;
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }


    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateSupportTicket([FromBody] SupportTicketRequestDTO requestDto)
    {
        try
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                _logger.LogError("User is not authenticated.");
                return Unauthorized();
            }
            if (requestDto == null)
            {
                _logger.LogError("Ticket data is missing from the request body.");
                return BadRequest(new { message = "Ticket data is missing." });
            }
            var adminEmails = (await _userManager.GetUsersInRoleAsync("Admin")).Select(u => u.Email).ToList();
            var ticket = new SupportTicketDTO
            {
                Summary = requestDto.Summary,
                Priority = requestDto.Priority,
                ReportedBy = user.UserName!,
                Link = Request.Headers["Referer"].ToString(),
                Inventory = requestDto.Inventory ?? string.Empty,
                AdminEmails = adminEmails!
            };
            var isSent = await _zapierService.SendSupportTicketAsync(ticket);
            if (isSent)
            {
                _logger.LogInformation("Support ticket submitted successfully.");
                return Ok(new { message = "Ticket created successfully!" });
            }
            _logger.LogError("Failed to submit support ticket.");
            return StatusCode(500, new { message = "Failed to submit ticket." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while creating a support ticket.");
            return StatusCode(500, new { message = "An error occurred." });
        }
    }
}