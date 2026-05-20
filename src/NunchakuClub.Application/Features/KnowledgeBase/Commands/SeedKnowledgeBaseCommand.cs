using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using NunchakuClub.Application.Common.Interfaces;
using NunchakuClub.Application.Common.Models;

namespace NunchakuClub.Application.Features.KnowledgeBase.Commands;

public record SeedKnowledgeBaseCommand : IRequest<Result<IReadOnlyList<KnowledgeDocumentDto>>>;

public class SeedKnowledgeBaseCommandHandler
    : IRequestHandler<SeedKnowledgeBaseCommand, Result<IReadOnlyList<KnowledgeDocumentDto>>>
{
    private readonly IKnowledgeBaseService _kb;

    public SeedKnowledgeBaseCommandHandler(IKnowledgeBaseService kb)
    {
        _kb = kb;
    }

    public async Task<Result<IReadOnlyList<KnowledgeDocumentDto>>> Handle(
        SeedKnowledgeBaseCommand request,
        CancellationToken cancellationToken)
    {
        await _kb.SeedDefaultAsync(cancellationToken);
        var docs = await _kb.GetAllAsync(cancellationToken);
        return Result<IReadOnlyList<KnowledgeDocumentDto>>.Success(docs);
    }
}
