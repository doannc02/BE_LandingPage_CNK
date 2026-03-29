using MediatR;
using NunchakuClub.Application.Common.Interfaces;
using NunchakuClub.Application.Common.Models;
using NunchakuClub.Application.Features.Achievements.DTOs;
using NunchakuClub.Domain.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NunchakuClub.Application.Features.Achievements.Commands;

public record CreateAchievementCommand(CreateAchievementDto Dto) : IRequest<Result<Guid>>;

public class CreateAchievementCommandHandler : IRequestHandler<CreateAchievementCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;

    public CreateAchievementCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<Guid>> Handle(CreateAchievementCommand request, CancellationToken cancellationToken)
    {
        var dto = request.Dto;

        var achievement = new Achievement
        {
            Title = dto.Title,
            Description = dto.Description,
            AchievementDate = dto.AchievementDate,
            Type = Enum.Parse<AchievementType>(dto.Type),
            ImageUrl = dto.ImageUrl,
            CoachId = dto.CoachId,
            ParticipantNames = dto.ParticipantNames,
            IsFeatured = dto.IsFeatured,
            DisplayOrder = dto.DisplayOrder
        };

        _context.Achievements.Add(achievement);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(achievement.Id);
    }
}
