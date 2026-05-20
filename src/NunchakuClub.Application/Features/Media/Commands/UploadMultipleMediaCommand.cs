using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Http;
using NunchakuClub.Application.Common.Interfaces;
using NunchakuClub.Application.Common.Models;

namespace NunchakuClub.Application.Features.Media.Commands;

public record UploadMultipleMediaCommand(IReadOnlyList<IFormFile> Files)
    : IRequest<Result<IReadOnlyList<CloudStorageResult>>>;

public class UploadMultipleMediaCommandHandler
    : IRequestHandler<UploadMultipleMediaCommand, Result<IReadOnlyList<CloudStorageResult>>>
{
    private readonly ICloudStorageService _cloudStorage;

    public UploadMultipleMediaCommandHandler(ICloudStorageService cloudStorage)
    {
        _cloudStorage = cloudStorage;
    }

    public async Task<Result<IReadOnlyList<CloudStorageResult>>> Handle(
        UploadMultipleMediaCommand request,
        CancellationToken cancellationToken)
    {
        var results = new List<CloudStorageResult>();

        foreach (var file in request.Files)
        {
            var result = await _cloudStorage.UploadAsync(file, file.FileName, cancellationToken);
            if (result.Success)
                results.Add(result);
        }

        return Result<IReadOnlyList<CloudStorageResult>>.Success(results);
    }
}
