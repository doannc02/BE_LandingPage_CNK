using System.Threading;
using System.Threading.Tasks;
using MediatR;
using NunchakuClub.Application.Common.Interfaces;
using NunchakuClub.Application.Common.Models;

namespace NunchakuClub.Application.Features.KnowledgeBase.Commands;

public record AddKnowledgeDocumentCommand(
    string Content,
    string Source,
    string? Title) : IRequest<Result<KnowledgeDocumentDto>>;

public class AddKnowledgeDocumentCommandHandler
    : IRequestHandler<AddKnowledgeDocumentCommand, Result<KnowledgeDocumentDto>>
{
    private readonly IKnowledgeBaseService _kb;

    public AddKnowledgeDocumentCommandHandler(IKnowledgeBaseService kb)
    {
        _kb = kb;
    }

    public async Task<Result<KnowledgeDocumentDto>> Handle(
        AddKnowledgeDocumentCommand request,
        CancellationToken cancellationToken)
    {
        var doc = await _kb.AddAsync(request.Content, request.Source, request.Title, cancellationToken);
        return Result<KnowledgeDocumentDto>.Success(doc);
    }
}
