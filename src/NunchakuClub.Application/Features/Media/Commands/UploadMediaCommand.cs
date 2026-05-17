using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Http;
using NunchakuClub.Application.Common.Interfaces;
using NunchakuClub.Application.Common.Models;

namespace NunchakuClub.Application.Features.Media.Commands;

public record UploadMediaCommand(IFormFile File) : IRequest<Result<CloudStorageResult>>;

public class UploadMediaCommandHandler : IRequestHandler<UploadMediaCommand, Result<CloudStorageResult>>
{
    private readonly ICloudStorageService _cloudStorage;

    public UploadMediaCommandHandler(ICloudStorageService cloudStorage)
    {
        _cloudStorage = cloudStorage;
    }

    public async Task<Result<CloudStorageResult>> Handle(
        UploadMediaCommand request,
        CancellationToken cancellationToken)
    {
        var result = await _cloudStorage.UploadAsync(request.File, request.File.FileName, cancellationToken);
        return result.Success
            ? Result<CloudStorageResult>.Success(result)
            : Result<CloudStorageResult>.Failure(result.Error ?? "Upload thất bại.");
    }
}
