using System.Threading;
using System.Threading.Tasks;
using MediatR;
using NunchakuClub.Application.Common.Interfaces;
using NunchakuClub.Application.Common.Models;

namespace NunchakuClub.Application.Features.Media.Commands;

public record DeleteLessonVideoCommand(string PublicId) : IRequest<Result>;

public class DeleteLessonVideoCommandHandler : IRequestHandler<DeleteLessonVideoCommand, Result>
{
    private readonly IVideoStorageService _videoStorage;

    public DeleteLessonVideoCommandHandler(IVideoStorageService videoStorage)
    {
        _videoStorage = videoStorage;
    }

    public async Task<Result> Handle(
        DeleteLessonVideoCommand request,
        CancellationToken cancellationToken)
    {
        var success = await _videoStorage.DeleteVideoAsync(request.PublicId, cancellationToken);
        return success ? Result.Success() : Result.Failure("Không thể xóa video. Kiểm tra lại publicId.");
    }
}
