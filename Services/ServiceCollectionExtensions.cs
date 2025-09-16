using FluentValidation;
using Microsoft.EntityFrameworkCore;
using InventoryManagement.Web.Data;
using InventoryManagement.Web.Services.Abstractions;
using InventoryManagement.Web.Services.Implementations;
using InventoryManagement.Web.Data.Models;
using Microsoft.AspNetCore.Identity;
using InventoryManagement.Web.Data.Configurations;
using FluentValidation.AspNetCore;
using InventoryManagement.Web.Models.Configurations;
using Amazon.S3;
using Amazon;

namespace InventoryManagement.Web.Services;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<ICloudStorageService, AmazonService>();
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IInventoryService, InventoryService>();
        services.AddScoped<IItemService, ItemService>();
        services.AddScoped<IAdminService, AdminService>();
        services.AddScoped<IItemLikeCommentService, ItemLikeCommentService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<ICustomIdGeneratorService, CustomIdGeneratorService>();
        services.AddScoped<IInventoryStatsService, InventoryStatsService>();
        services.AddScoped<IExportService, ExportService>();
        services.AddScoped<ICustomIdGeneratorService, CustomIdGeneratorService>();
        services.AddScoped<IApiTokenService, ApiTokenService>();

        services.AddHttpClient<IZapierService, ZapierService>();
        services.AddHttpClient<ISalesforceService, SalesforceService>();
        
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));
        var cookieExpireTimeSpanMinutes = configuration.GetValue<int>("Authentication:Cookie:ExpireTimeSpanMinutes");

        services.AddAuthentication(IdentityConstants.ApplicationScheme)
            .AddCookie(options =>
            {
                options.LoginPath = "/Auth/Login";
                options.LogoutPath = "/Auth/Logout";
                options.AccessDeniedPath = "/Auth/AccessDenied";
                options.SlidingExpiration = true;
                options.ExpireTimeSpan = TimeSpan.FromMinutes(cookieExpireTimeSpanMinutes);
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.Always;
            })
            .AddGoogle(options =>
            {
                options.ClientId = configuration["Authentication:Google:ClientId"]!;
                options.ClientSecret = configuration["Authentication:Google:ClientSecret"]!;
            })
            .AddDiscord(options =>
            {
                options.ClientId = configuration["Authentication:Discord:ClientId"]!; 
                options.ClientSecret = configuration["Authentication:Discord:ClientSecret"]!;
                options.Scope.Add("email");
            });

        services.AddIdentity<User, IdentityRole<Guid>>(options =>
        {
            configuration.GetSection("IdentityOptions:Password").Bind(options.Password);
            options.Lockout.AllowedForNewUsers = false;
            options.Lockout.DefaultLockoutTimeSpan = Constants.IdentityConstants.DefaultLockoutTimeSpan;
            options.Lockout.MaxFailedAccessAttempts = Constants.IdentityConstants.DefaultMaxFailedAccessAttempts;
        })
       .AddEntityFrameworkStores<ApplicationDbContext>()
       .AddDefaultTokenProviders();
        services.AddAutoMapper(typeof(Program));
        services.AddValidatorsFromAssembly(typeof(Program).Assembly);

        services.AddFluentValidationClientsideAdapters();

        services.Configure<SalesforceSettings>(configuration.GetSection("SalesforceSettings"));
        services.Configure<AwsSettings>(configuration.GetSection("AwsSettings"));
        services.Configure<AppConfiguration>(configuration.GetSection("AppConfiguration"));
        services.Configure<IdentityConfig>(configuration.GetSection("IdentityOptions"));
        services.Configure<SmtpSettings>(configuration.GetSection("SmtpSettings"));
        services.Configure<ZapierSettings>(configuration.GetSection("ZapierSettings"));

        services.AddSingleton<IAmazonS3>(sp => new AmazonS3Client(
            configuration["AwsSettings:AccessKey"],
            configuration["AwsSettings:SecretKey"],
            RegionEndpoint.GetBySystemName(configuration["AwsSettings:Region"])
        ));
        return services;
    }
}