using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using NunchakuClub.Application.Common.Interfaces;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NunchakuClub.Infrastructure.Services.CloudStorage;

public class AwsS3StorageService : ICloudStorageService
{
    private readonly IAmazonS3 _s3Client;
    private readonly AwsS3Settings _settings;
    
    public AwsS3StorageService(IAmazonS3 s3Client, IOptions<AwsS3Settings> settings)
    {
        _s3Client = s3Client;
        _settings = settings.Value;
    }
    
    public async Task<CloudStorageResult> UploadAsync(IFormFile file, string folder, CancellationToken cancellationToken = default)
    {
        try
        {
            var fileName = $"{Guid.NewGuid()}_{file.FileName}";
            var key = $"{folder}/{fileName}";
            
            using var stream = file.OpenReadStream();
            
            var uploadRequest = new TransferUtilityUploadRequest
            {
                InputStream = stream,
                Key = key,
                BucketName = _settings.BucketName,
                CannedACL = S3CannedACL.PublicRead,
                ContentType = file.ContentType
            };
            
            var transferUtility = new TransferUtility(_s3Client);
            await transferUtility.UploadAsync(uploadRequest, cancellationToken);
            
            var url = $"https://{_settings.BucketName}.s3.{_settings.Region}.amazonaws.com/{key}";
            
            return new CloudStorageResult
            {
                Success = true,
                Url = url,
                FileName = fileName,
                FileSize = file.Length
            };
        }
        catch (Exception ex)
        {
            return new CloudStorageResult { Success = false, Error = ex.Message };
        }
    }
    
    public async Task<CloudStorageResult> UploadAsync(Stream stream, string fileName, string folder, string contentType, CancellationToken cancellationToken = default)
    {
        try
        {
            var key = $"{folder}/{fileName}";
            
            var uploadRequest = new TransferUtilityUploadRequest
            {
                InputStream = stream,
                Key = key,
                BucketName = _settings.BucketName,
                CannedACL = S3CannedACL.PublicRead,
                ContentType = contentType
            };
            
            var transferUtility = new TransferUtility(_s3Client);
            await transferUtility.UploadAsync(uploadRequest, cancellationToken);
            
            var url = $"https://{_settings.BucketName}.s3.{_settings.Region}.amazonaws.com/{key}";
            
            return new CloudStorageResult { Success = true, Url = url, FileName = fileName };
        }
        catch (Exception ex)
        {
            return new CloudStorageResult { Success = false, Error = ex.Message };
        }
    }
    
    public async Task<bool> DeleteAsync(string fileUrl, CancellationToken cancellationToken = default)
    {
        try
        {
            var key = ExtractKeyFromUrl(fileUrl);
            await _s3Client.DeleteObjectAsync(new DeleteObjectRequest
            {
                BucketName = _settings.BucketName,
                Key = key
            }, cancellationToken);
            return true;
        }
        catch
        {
            return false;
        }
    }
    
    public async Task<string> GetPresignedUrlAsync(string fileUrl, int expiresInMinutes = 60)
    {
        var key = ExtractKeyFromUrl(fileUrl);
        var request = new GetPreSignedUrlRequest
        {
            BucketName = _settings.BucketName,
            Key = key,
            Expires = DateTime.UtcNow.AddMinutes(expiresInMinutes)
        };
        return await Task.FromResult(_s3Client.GetPreSignedURL(request));
    }
    
    private string ExtractKeyFromUrl(string url)
    {
        var uri = new Uri(url);
        return uri.AbsolutePath.TrimStart('/');
    }
}

public class AwsS3Settings
{
    public string BucketName { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
}
