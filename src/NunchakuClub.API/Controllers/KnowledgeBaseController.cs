using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NunchakuClub.Application.Features.KnowledgeBase.Commands;
using NunchakuClub.Application.Features.KnowledgeBase.Queries;

namespace NunchakuClub.API.Controllers;

/// <summary>
/// Admin-only endpoints for managing the RAG knowledge base.
/// All write operations auto-generate embeddings via Google text-embedding-004.
/// </summary>
[ApiController]
[Route("api/knowledge-base")]
[Authorize(Policy = "RequireAdminArea")]
public sealed class KnowledgeBaseController : ControllerBase
{
    private readonly IMediator _mediator;

    public KnowledgeBaseController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>List all knowledge documents.</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetAllKnowledgeDocumentsQuery(), ct);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(result.Error);
    }

    /// <summary>Add a new document and generate its embedding.</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Add([FromBody] UpsertDocumentRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Content))
            return BadRequest(new { isSuccess = false, error = "content is required." });

        if (string.IsNullOrWhiteSpace(req.Source))
            return BadRequest(new { isSuccess = false, error = "source is required." });

        var result = await _mediator.Send(
            new AddKnowledgeDocumentCommand(req.Content.Trim(), req.Source.Trim(), req.Title?.Trim()), ct);

        return result.IsSuccess
            ? CreatedAtAction(nameof(GetAll), null, result.Data)
            : BadRequest(new { isSuccess = false, error = result.Error });
    }

    /// <summary>Update document content and regenerate its embedding.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpsertDocumentRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Content))
            return BadRequest(new { isSuccess = false, error = "content is required." });

        if (string.IsNullOrWhiteSpace(req.Source))
            return BadRequest(new { isSuccess = false, error = "source is required." });

        var result = await _mediator.Send(
            new UpdateKnowledgeDocumentCommand(id, req.Content.Trim(), req.Source.Trim(), req.Title?.Trim()), ct);

        return result.IsSuccess ? Ok(result.Data) : NotFound(new { isSuccess = false, error = result.Error });
    }

    /// <summary>Delete a knowledge document.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new DeleteKnowledgeDocumentCommand(id), ct);
        return result.IsSuccess ? NoContent() : NotFound(new { isSuccess = false, error = result.Error });
    }

    /// <summary>
    /// Seed the default CLB knowledge documents (idempotent — skips if already seeded).
    /// </summary>
    [HttpPost("seed")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Seed(CancellationToken ct)
    {
        var result = await _mediator.Send(new SeedKnowledgeBaseCommand(), ct);
        return result.IsSuccess
            ? Ok(new { isSuccess = true, message = "Seed completed.", data = result.Data })
            : BadRequest(result.Error);
    }
}

public sealed class UpsertDocumentRequest
{
    public string Content { get; init; } = string.Empty;
    public string Source { get; init; } = string.Empty;
    public string? Title { get; init; }
}
