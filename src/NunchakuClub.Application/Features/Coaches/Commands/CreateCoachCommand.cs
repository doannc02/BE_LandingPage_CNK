using MediatR;
using NunchakuClub.Application.Common.Interfaces;
using NunchakuClub.Application.Common.Models;
using NunchakuClub.Application.Features.Coaches.DTOs;
using NunchakuClub.Domain.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NunchakuClub.Application.Features.Coaches.Commands;

public record CreateCoachCommand(CreateCoachDto Dto) : IRequest<Result<Guid>>;

public class CreateCoachCommandHandler : IRequestHandler<CreateCoachCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;

    public CreateCoachCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<Guid>> Handle(CreateCoachCommand request, CancellationToken cancellationToken)
    {
        var dto = request.Dto;

        var coach = new Coach
        {
            FullName = dto.FullName,
            Title = dto.Title,
            Bio = dto.Bio,
            Specialization = dto.Specialization,
            YearsOfExperience = dto.YearsOfExperience,
            Certifications = dto.Certifications,
            Achievements = dto.Achievements,
            AvatarUrl = dto.AvatarUrl,
            DisplayOrder = dto.DisplayOrder,
            IsActive = dto.IsActive
        };

        _context.Coaches.Add(coach);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(coach.Id);
    }
}
