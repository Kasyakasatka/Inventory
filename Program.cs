using Amazon.S3;
using Amazon;
using DotNetEnv;
using InventoryManagement.Web.Data;
using InventoryManagement.Web.Data.Configurations;
using InventoryManagement.Web.Hubs;
using InventoryManagement.Web.Services;
using InventoryManagement.Web.Services.Implementations;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Text;
using InventoryManagement.Web.Services.Abstractions;

//Env.Load();
var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();
builder.Host.UseSerilog((context, loggerConfig) =>
    loggerConfig.ReadFrom.Configuration(context.Configuration));
builder.Services.AddControllersWithViews();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpContextAccessor();
builder.Services.AddApplicationServices(builder.Configuration);
builder.Services.AddResponseCaching();
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Auth/Login";
    options.AccessDeniedPath = "/Auth/AccessDenied";
});
builder.Services.AddSignalR();
builder.Services.AddAuthentication();

var app = builder.Build();
app.UseSerilogRequestLogging();
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
app.Use(async (context, next) =>
{
    context.Request.Scheme = "https";
    await next();
});
app.UseHttpsRedirection();
app.UseResponseCaching();
app.UseStaticFiles();
app.UseRouting();
await using (var scope = app.Services.CreateAsyncScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<ApplicationDbContext>();
    var configuration = services.GetRequiredService<IConfiguration>();
    // await DataSeeder.EnsureSeedData(context, services, configuration);
}
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<ExceptionHandlerMiddleware>();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapControllers();
app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
    endpoints.MapHub<CommentsHub>("/commentsHub");
});
app.Run();
