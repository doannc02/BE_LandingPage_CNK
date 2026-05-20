using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using NunchakuClub.Application.Common.Interfaces;
using NunchakuClub.Application.Common.Models;

namespace NunchakuClub.Application.Features.KnowledgeBase.Commands;

public record UpdateKnowledgeDocumentCommand(
    Guid Id,
    string Content,
    string Source,
    string? Title) : IRequest<Result<KnowledgeDocumentDto>>;

public class UpdateKnowledgeDocumentCommandHandler
    : IRequestHandler<UpdateKnowledgeDocumentCommand, Result<KnowledgeDocumentDto>>
{
    private readonly IKnowledgeBaseService _kb;

    public UpdateKnowledgeDocumentCommandHandler(IKnowledgeBaseService kb)
    {
        _kb = kb;
    }

    public async Task<Result<KnowledgeDocumentDto>> Handle(
        UpdateKnowledgeDocumentCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var doc = await _kb.UpdateAsync(request.Id, request.Content, request.Source, request.Title, cancellationToken);
            return Result<KnowledgeDocumentDto>.Success(doc);
        }
        catch (InvalidOperationException ex)
        {
            return Result<KnowledgeDocumentDto>.Failure(ex.Message);
        }
    }
}
