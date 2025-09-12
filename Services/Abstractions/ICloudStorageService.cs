using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace InventoryManagement.Web.Services.Abstractions;

public interface ICloudStorageService
{
    Task<string> UploadImageAsync(IFormFile file);
}