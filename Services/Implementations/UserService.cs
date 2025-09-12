using AutoMapper;
using InventoryManagement.Web.Data;
using InventoryManagement.Web.Data.Models;
using InventoryManagement.Web.DTOs;
using InventoryManagement.Web.Services.Abstractions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NpgsqlTypes;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace InventoryManagement.Web.Services.Implementations;

public class UserService : IUserService
{
    private readonly UserManager<User> _userManager;
    private readonly IMapper _mapper;
    private readonly ILogger<UserService> _logger;
    private readonly ApplicationDbContext _context;

    public UserService(UserManager<User> userManager, IMapper mapper, ILogger<UserService> logger, ApplicationDbContext context)
    {
        _userManager = userManager;
        _mapper = mapper;
        _logger = logger;
        _context = context;
    }
    public async Task<IEnumerable<UserSearchDTO>> SearchUsersAsync(string query, Guid? excludeUserId)
    {
        _logger.LogInformation("Searching for users with query: {Query}.", query);
        if (string.IsNullOrWhiteSpace(query))
        {
            return Enumerable.Empty<UserSearchDTO>();
        }
        var cleanedQuery = Regex.Replace(query.Trim(), @"[^\w\s]", "");
        var searchTerms = cleanedQuery.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                               .Where(term => !string.IsNullOrWhiteSpace(term))
                               .Select(term => $"{term}:*");
        var formattedQuery = string.Join(" & ", searchTerms);
        if (string.IsNullOrWhiteSpace(formattedQuery))
        {
            return Enumerable.Empty<UserSearchDTO>();
        }
        var users = await _context.Users
            .Where(u => u.SearchVector != null && u.SearchVector.Matches(EF.Functions.ToTsQuery("english", formattedQuery)))
            .ToListAsync();
        var userDTOs = users
            .Select(u => _mapper.Map<UserSearchDTO>(u))
            .ToList();
        _logger.LogInformation("Found {Count} users for query.", userDTOs.Count);
        return userDTOs;
    }
}