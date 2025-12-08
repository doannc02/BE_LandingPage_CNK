using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NunchakuClub.Application.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NunchakuClub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,Editor")]
public class MediaController : ControllerBase
{
    private readonly ICloudStorageService _cloudStorage;

    public MediaController(ICloudStorageService cloudStorage)
    {
        _cloudStorage = cloudStorage;
    }

    [HttpPost("upload")]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded");

        var result = await _cloudStorage.UploadAsync(file, file.FileName);
        
        return result.Success 
            ? Ok(new { url = result.Url, thumbnailUrl = result.ThumbnailUrl, fileName = result.FileName })
            : BadRequest(result.Error);
    }

    [HttpPost("upload-multiple")]
    public async Task<IActionResult> UploadMultiple(List<IFormFile> files)
    {
        if (files == null || !files.Any())
            return BadRequest("No files uploaded");
        
        var results = new List<object>();
        
        foreach (var file in files)
        {
            var result = await _cloudStorage.UploadAsync(file, file.FileName);
            if (result.Success)
            {
                results.Add(new { url = result.Url, thumbnailUrl = result.ThumbnailUrl, fileName = result.FileName });
            }
        }
        
        return Ok(results);
    }
}
