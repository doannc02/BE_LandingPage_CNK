using Microsoft.AspNetCore.Http;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace NunchakuClub.Application.Common.Interfaces;

public interface ICloudStorageService
{
    Task<CloudStorageResult> UploadAsync(IFormFile file, string folder, CancellationToken cancellationToken = default);
    Task<CloudStorageResult> UploadAsync(Stream stream, string fileName, string folder, string contentType, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string fileUrl, CancellationToken cancellationToken = default);
    Task<string> GetPresignedUrlAsync(string fileUrl, int expiresInMinutes = 60);
}

public class CloudStorageResult
{
    public bool Success { get; set; }
    public string? Url { get; set; }
    public string? ThumbnailUrl { get; set; }
    public string? FileName { get; set; }
    public long FileSize { get; set; }
    public string? Error { get; set; }
}
