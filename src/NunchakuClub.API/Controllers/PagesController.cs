using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NunchakuClub.Application.Features.Pages.Commands;
using NunchakuClub.Application.Features.Pages.DTOs;
using NunchakuClub.Application.Features.Pages.Queries;
using System;
using System.Threading.Tasks;

namespace NunchakuClub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PagesController : ControllerBase
{
    private readonly IMediator _mediator;

    public PagesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetPages([FromQuery] bool? isPublished = null)
    {
        var query = new GetPagesQuery(isPublished);
        var result = await _mediator.Send(query);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(result.Error);
    }

    [HttpGet("slug/{slug}")]
    public async Task<IActionResult> GetPageBySlug(string slug)
    {
        var query = new GetPageBySlugQuery(slug);
        var result = await _mediator.Send(query);
        return result.IsSuccess ? Ok(result.Data) : NotFound(result.Error);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreatePage([FromBody] CreatePageDto dto)
    {
        var command = new CreatePageCommand(dto);
        var result = await _mediator.Send(command);
        return result.IsSuccess ? Created("", result.Data) : BadRequest(result.Error);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdatePage(Guid id, [FromBody] UpdatePageDto dto)
    {
        var command = new UpdatePageCommand(id, dto);
        var result = await _mediator.Send(command);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(result.Error);
    }
}
