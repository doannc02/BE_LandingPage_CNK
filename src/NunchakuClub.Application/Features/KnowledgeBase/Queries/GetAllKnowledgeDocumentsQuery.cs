using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using NunchakuClub.Application.Common.Interfaces;
using NunchakuClub.Application.Common.Models;

namespace NunchakuClub.Application.Features.KnowledgeBase.Queries;

public record GetAllKnowledgeDocumentsQuery : IRequest<Result<IReadOnlyList<KnowledgeDocumentDto>>>;

public class GetAllKnowledgeDocumentsQueryHandler
    : IRequestHandler<GetAllKnowledgeDocumentsQuery, Result<IReadOnlyList<KnowledgeDocumentDto>>>
{
    private readonly IKnowledgeBaseService _kb;

    public GetAllKnowledgeDocumentsQueryHandler(IKnowledgeBaseService kb)
    {
        _kb = kb;
    }

    public async Task<Result<IReadOnlyList<KnowledgeDocumentDto>>> Handle(
        GetAllKnowledgeDocumentsQuery request,
        CancellationToken cancellationToken)
    {
        var docs = await _kb.GetAllAsync(cancellationToken);
        return Result<IReadOnlyList<KnowledgeDocumentDto>>.Success(docs);
    }
}
