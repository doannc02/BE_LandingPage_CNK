using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Http;
using NunchakuClub.Application.Common.Interfaces;
using NunchakuClub.Application.Common.Models;
using NunchakuClub.Domain.Entities;

namespace NunchakuClub.Application.Features.Media.Commands;

public record UploadLessonVideoCommand(
    IFormFile File,
    string LessonTitle,
    CourseLevel Level) : IRequest<Result<VideoUploadResult>>;

public class UploadLessonVideoCommandHandler
    : IRequestHandler<UploadLessonVideoCommand, Result<VideoUploadResult>>
{
    private readonly IVideoStorageService _videoStorage;

    public UploadLessonVideoCommandHandler(IVideoStorageService videoStorage)
    {
        _videoStorage = videoStorage;
    }

    public async Task<Result<VideoUploadResult>> Handle(
        UploadLessonVideoCommand request,
        CancellationToken cancellationToken)
    {
        var result = await _videoStorage.UploadLessonVideoAsync(
            request.File, request.LessonTitle, request.Level, cancellationToken);

        return result.Success
            ? Result<VideoUploadResult>.Success(result)
            : Result<VideoUploadResult>.Failure(result.Error ?? "Upload video thất bại.");
    }
}
