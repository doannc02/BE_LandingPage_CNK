using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NunchakuClub.Application.Common.Interfaces;

namespace NunchakuClub.API.Controllers;

/// <summary>
/// Admin-only endpoints for managing the RAG knowledge base.
/// All write operations auto-generate embeddings via Google text-embedding-004.
/// </summary>
[ApiController]
[Route("api/knowledge-base")]
[Authorize(Roles = "Admin")]
public sealed class KnowledgeBaseController : ControllerBase
{
    private readonly IKnowledgeBaseService _kb;

    public KnowledgeBaseController(IKnowledgeBaseService kb) => _kb = kb;

    /// <summary>List all knowledge documents.</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var docs = await _kb.GetAllAsync(ct);
        return Ok(new { isSuccess = true, data = docs });
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

        var doc = await _kb.AddAsync(req.Content.Trim(), req.Source.Trim(), req.Title?.Trim(), ct);
        return CreatedAtAction(nameof(GetAll), new { isSuccess = true, data = doc });
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

        try
        {
            var doc = await _kb.UpdateAsync(id, req.Content.Trim(), req.Source.Trim(), req.Title?.Trim(), ct);
            return Ok(new { isSuccess = true, data = doc });
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { isSuccess = false, error = ex.Message });
        }
    }

    /// <summary>Delete a knowledge document.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        try
        {
            await _kb.DeleteAsync(id, ct);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { isSuccess = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Seed the default CLB knowledge documents (idempotent — skips if already seeded).
    /// Generates embeddings for all 8 default documents.
    /// </summary>
    [HttpPost("seed")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Seed(CancellationToken ct)
    {
        await _kb.SeedDefaultAsync(ct);
        var docs = await _kb.GetAllAsync(ct);
        return Ok(new { isSuccess = true, message = "Seed completed.", data = docs });
    }
}

public sealed class UpsertDocumentRequest
{
    public string Content { get; init; } = string.Empty;
    public string Source { get; init; } = string.Empty;
    public string? Title { get; init; }
}
