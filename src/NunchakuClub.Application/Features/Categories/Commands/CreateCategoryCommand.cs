using MediatR;
using Microsoft.EntityFrameworkCore;
using NunchakuClub.Application.Common.Interfaces;
using NunchakuClub.Application.Common.Models;
using NunchakuClub.Application.Features.Categories.DTOs;
using NunchakuClub.Domain.Entities;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NunchakuClub.Application.Features.Categories.Commands;

public record CreateCategoryCommand(CreateCategoryDto Dto) : IRequest<Result<Guid>>;

public class CreateCategoryCommandHandler : IRequestHandler<CreateCategoryCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;

    public CreateCategoryCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<Guid>> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        var dto = request.Dto;
        var slug = GenerateSlug(dto.Name);
        
        var existingSlug = await _context.Categories.AnyAsync(c => c.Slug == slug, cancellationToken);
        if (existingSlug)
        {
            slug = $"{slug}-{Guid.NewGuid().ToString().Substring(0, 8)}";
        }
        
        var category = new Category
        {
            Name = dto.Name,
            Slug = slug,
            Description = dto.Description,
            ParentId = dto.ParentId,
            DisplayOrder = dto.DisplayOrder,
            IsActive = dto.IsActive
        };
        
        _context.Categories.Add(category);
        await _context.SaveChangesAsync(cancellationToken);
        
        return Result<Guid>.Success(category.Id);
    }
    
    private static string GenerateSlug(string text)
    {
        return text.ToLowerInvariant().Replace(" ", "-");
    }
}
