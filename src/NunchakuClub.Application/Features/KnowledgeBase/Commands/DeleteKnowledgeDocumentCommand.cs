using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using NunchakuClub.Application.Common.Interfaces;
using NunchakuClub.Application.Common.Models;

namespace NunchakuClub.Application.Features.KnowledgeBase.Commands;

public record DeleteKnowledgeDocumentCommand(Guid Id) : IRequest<Result>;

public class DeleteKnowledgeDocumentCommandHandler
    : IRequestHandler<DeleteKnowledgeDocumentCommand, Result>
{
    private readonly IKnowledgeBaseService _kb;

    public DeleteKnowledgeDocumentCommandHandler(IKnowledgeBaseService kb)
    {
        _kb = kb;
    }

    public async Task<Result> Handle(
        DeleteKnowledgeDocumentCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            await _kb.DeleteAsync(request.Id, cancellationToken);
            return Result.Success();
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure(ex.Message);
        }
    }
}
