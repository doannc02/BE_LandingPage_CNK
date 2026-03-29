using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NunchakuClub.Application.Features.Coaches.Commands;
using NunchakuClub.Application.Features.Coaches.DTOs;
using NunchakuClub.Application.Features.Coaches.Queries;
using System;
using System.Threading.Tasks;

namespace NunchakuClub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CoachesController : ControllerBase
{
    private readonly IMediator _mediator;

    public CoachesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetCoaches([FromQuery] bool? isActive = null)
    {
        var query = new GetCoachesQuery(isActive);
        var result = await _mediator.Send(query);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(result.Error);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetCoachById(Guid id)
    {
        var query = new GetCoachByIdQuery(id);
        var result = await _mediator.Send(query);
        return result.IsSuccess ? Ok(result.Data) : NotFound(result.Error);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Editor")]
    public async Task<IActionResult> CreateCoach([FromBody] CreateCoachDto dto)
    {
        var command = new CreateCoachCommand(dto);
        var result = await _mediator.Send(command);
        return result.IsSuccess ? Created("", result.Data) : BadRequest(result.Error);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,Editor")]
    public async Task<IActionResult> UpdateCoach(Guid id, [FromBody] UpdateCoachDto dto)
    {
        var command = new UpdateCoachCommand(id, dto);
        var result = await _mediator.Send(command);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(result.Error);
    }
}
