using MediatR;
using Microsoft.EntityFrameworkCore;
using NunchakuClub.Application.Common.Interfaces;
using NunchakuClub.Application.Common.Models;
using NunchakuClub.Application.Features.Coaches.DTOs;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NunchakuClub.Application.Features.Coaches.Commands;

public record UpdateCoachCommand(Guid Id, UpdateCoachDto Dto) : IRequest<Result<CoachDto>>;

public class UpdateCoachCommandHandler : IRequestHandler<UpdateCoachCommand, Result<CoachDto>>
{
    private readonly IApplicationDbContext _context;

    public UpdateCoachCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<CoachDto>> Handle(UpdateCoachCommand request, CancellationToken cancellationToken)
    {
        var coach = await _context.Coaches
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

        if (coach == null)
            return Result<CoachDto>.Failure("Coach not found");

        var dto = request.Dto;
        coach.FullName = dto.FullName;
        coach.Title = dto.Title;
        coach.Bio = dto.Bio;
        coach.Specialization = dto.Specialization;
        coach.YearsOfExperience = dto.YearsOfExperience;
        coach.Certifications = dto.Certifications;
        coach.Achievements = dto.Achievements;
        coach.AvatarUrl = dto.AvatarUrl;
        coach.DisplayOrder = dto.DisplayOrder;
        coach.IsActive = dto.IsActive;
        coach.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return Result<CoachDto>.Success(new CoachDto
        {
            Id = coach.Id,
            FullName = coach.FullName,
            Title = coach.Title,
            Bio = coach.Bio,
            Specialization = coach.Specialization,
            YearsOfExperience = coach.YearsOfExperience,
            Certifications = coach.Certifications,
            Achievements = coach.Achievements,
            AvatarUrl = coach.AvatarUrl,
            DisplayOrder = coach.DisplayOrder,
            IsActive = coach.IsActive
        });
    }
}
