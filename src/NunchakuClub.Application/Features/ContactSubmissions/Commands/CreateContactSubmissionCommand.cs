using MediatR;
using NunchakuClub.Application.Common.Interfaces;
using NunchakuClub.Application.Common.Models;
using NunchakuClub.Application.Features.ContactSubmissions.DTOs;
using NunchakuClub.Domain.Entities;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NunchakuClub.Application.Features.ContactSubmissions.Commands;

public record CreateContactSubmissionCommand(CreateContactSubmissionDto Dto, string? IpAddress, string? UserAgent) 
    : IRequest<Result<Guid>>;

public class CreateContactSubmissionCommandHandler : IRequestHandler<CreateContactSubmissionCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;

    public CreateContactSubmissionCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<Guid>> Handle(CreateContactSubmissionCommand request, CancellationToken cancellationToken)
    {
        var dto = request.Dto;
        
        var submission = new ContactSubmission
        {
            FullName = dto.FullName,
            Phone = dto.Phone,
            Email = dto.Email,
            CourseId = dto.CourseId,
            Message = dto.Message,
            Status = ContactStatus.New,
            IpAddress = request.IpAddress,
            UserAgent = request.UserAgent
        };
        
        _context.ContactSubmissions.Add(submission);
        await _context.SaveChangesAsync(cancellationToken);
        
        return Result<Guid>.Success(submission.Id);
    }
}
