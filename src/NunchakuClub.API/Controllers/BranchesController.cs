using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NunchakuClub.Application.Features.Branches.Commands;
using NunchakuClub.Application.Features.Branches.DTOs;
using NunchakuClub.Application.Features.Branches.Queries;
using System;
using System.Threading.Tasks;

namespace NunchakuClub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BranchesController : ControllerBase
{
    private readonly IMediator _mediator;

    public BranchesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetBranches([FromQuery] bool? isActive)
    {
        var result = await _mediator.Send(new GetBranchListQuery(isActive));
        return result.IsSuccess ? Ok(result.Data) : BadRequest(result.Error);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetBranchDetail(Guid id)
    {
        var result = await _mediator.Send(new GetBranchDetailQuery(id));
        return result.IsSuccess ? Ok(result.Data) : NotFound(result.Error);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateBranch([FromBody] CreateBranchDto dto)
    {
        var result = await _mediator.Send(new CreateBranchCommand(dto));
        return result.IsSuccess ? Created("", result.Data) : BadRequest(result.Error);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateBranch(Guid id, [FromBody] UpdateBranchDto dto)
    {
        var result = await _mediator.Send(new UpdateBranchCommand(id, dto));
        return result.IsSuccess ? Ok(result.Data) : BadRequest(result.Error);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteBranch(Guid id)
    {
        var result = await _mediator.Send(new DeleteBranchCommand(id));
        return result.IsSuccess ? Ok(result.Data) : BadRequest(result.Error);
    }
}
