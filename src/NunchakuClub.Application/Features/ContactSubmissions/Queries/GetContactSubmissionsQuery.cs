using MediatR;
using Microsoft.EntityFrameworkCore;
using NunchakuClub.Application.Common.Extensions;
using NunchakuClub.Application.Common.Interfaces;
using NunchakuClub.Application.Common.Models;
using NunchakuClub.Application.Features.ContactSubmissions.DTOs;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NunchakuClub.Application.Features.ContactSubmissions.Queries;

public record GetContactSubmissionsQuery(int PageNumber = 1, int PageSize = 20) 
    : IRequest<Result<PaginatedList<ContactSubmissionDto>>>;

public class GetContactSubmissionsQueryHandler 
    : IRequestHandler<GetContactSubmissionsQuery, Result<PaginatedList<ContactSubmissionDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetContactSubmissionsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<PaginatedList<ContactSubmissionDto>>> Handle(
        GetContactSubmissionsQuery request, 
        CancellationToken cancellationToken)
    {
        var query = _context.ContactSubmissions
            .Include(c => c.Course)
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new ContactSubmissionDto
            {
                Id = c.Id,
                FullName = c.FullName,
                Phone = c.Phone,
                Email = c.Email,
                CourseName = c.Course != null ? c.Course.Name : null,
                Message = c.Message,
                Status = c.Status.ToString(),
                CreatedAt = c.CreatedAt
            });

        var result = await query.ToPaginatedListAsync(request.PageNumber, request.PageSize, cancellationToken);
        return Result<PaginatedList<ContactSubmissionDto>>.Success(result);
    }
}
