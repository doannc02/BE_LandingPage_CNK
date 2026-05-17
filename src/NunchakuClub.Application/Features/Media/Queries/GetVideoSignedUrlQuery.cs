using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using NunchakuClub.Application.Common.Interfaces;
using NunchakuClub.Application.Common.Models;

namespace NunchakuClub.Application.Features.Media.Queries;

public record GetVideoSignedUrlQuery(
    string PublicId,
    int ExpiresInMinutes = 60) : IRequest<Result<VideoSignedUrlDto>>;

public sealed record VideoSignedUrlDto(string SignedUrl, DateTime ExpiresAt);

public class GetVideoSignedUrlQueryHandler
    : IRequestHandler<GetVideoSignedUrlQuery, Result<VideoSignedUrlDto>>
{
    private readonly IVideoStorageService _videoStorage;

    public GetVideoSignedUrlQueryHandler(IVideoStorageService videoStorage)
    {
        _videoStorage = videoStorage;
    }

    public async Task<Result<VideoSignedUrlDto>> Handle(
        GetVideoSignedUrlQuery request,
        CancellationToken cancellationToken)
    {
        var signedUrl = await _videoStorage.GetSignedVideoUrlAsync(request.PublicId, request.ExpiresInMinutes);
        var dto = new VideoSignedUrlDto(signedUrl, DateTime.UtcNow.AddMinutes(request.ExpiresInMinutes));
        return Result<VideoSignedUrlDto>.Success(dto);
    }
}
