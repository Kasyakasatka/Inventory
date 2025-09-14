using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using InventoryManagement.Web.Data.Configurations;
using InventoryManagement.Web.Models.Configurations;
using InventoryManagement.Web.Services.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Threading.Tasks;

namespace InventoryManagement.Web.Services.Implementations;

public class AmazonService : ICloudStorageService
{
    private readonly IAmazonS3 _s3Client;
    private readonly ILogger<AmazonService> _logger;
    private readonly AwsSettings _awsSettings;

    public AmazonService(IAmazonS3 s3Client, ILogger<AmazonService> logger, IOptions<AwsSettings> awsSettings)
    {
        _s3Client = s3Client;
        _logger = logger;
        _awsSettings = awsSettings.Value;
    }

    public async Task<string> UploadImageAsync(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            _logger.LogWarning("Attempted to upload a null or empty file to S3.");
            return null;
        }
        try
        {
            using var transferUtility = new TransferUtility(_s3Client);
            var fileExtension = Path.GetExtension(file.FileName);
            var key = $"{Guid.NewGuid()}{fileExtension}";
            _logger.LogInformation("Starting upload of file {FileName} to S3 bucket {BucketName}.", file.FileName, _awsSettings.BucketName);
            await transferUtility.UploadAsync(file.OpenReadStream(), _awsSettings.BucketName, key);
            var request = new GetPreSignedUrlRequest
            {
                BucketName = _awsSettings.BucketName,
                Key = key,
                Expires = DateTime.UtcNow.AddMinutes(_awsSettings.SignedUrlExpiresInMinutes)
            };
            var imageUrl = _s3Client.GetPreSignedURL(request);
            _logger.LogInformation("File uploaded successfully. Signed URL: {Url}", imageUrl);
            return imageUrl;
        }
        catch (AmazonS3Exception ex)
        {
            _logger.LogError(ex, "Failed to upload file to S3. AWS Error Code: {ErrorCode}", ex.ErrorCode);
            throw new InvalidOperationException("Failed to upload file to S3.", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred during file upload.");
            throw;
        }
    }
}