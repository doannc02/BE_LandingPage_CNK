using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NunchakuClub.Application.Features.Inventory.Commands;
using NunchakuClub.Application.Features.Inventory.DTOs;
using NunchakuClub.Application.Features.Inventory.Queries;
using System;
using System.Threading.Tasks;

namespace NunchakuClub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InventoryController : ControllerBase
{
    private readonly IMediator _mediator;

    public InventoryController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetInventoryList([FromQuery] Guid? branchId, [FromQuery] Guid? categoryId, [FromQuery] string? status)
    {
        var result = await _mediator.Send(new GetInventoryListQuery(branchId, categoryId, status));
        return result.IsSuccess ? Ok(result.Data) : BadRequest(result.Error);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetInventoryDetail(Guid id)
    {
        var result = await _mediator.Send(new GetInventoryDetailQuery(id));
        return result.IsSuccess ? Ok(result.Data) : NotFound(result.Error);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateInventoryItem([FromBody] CreateInventoryItemDto dto)
    {
        var result = await _mediator.Send(new CreateInventoryItemCommand(dto));
        return result.IsSuccess ? Created("", result.Data) : BadRequest(result.Error);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateInventoryItem(Guid id, [FromBody] UpdateInventoryItemDto dto)
    {
        var result = await _mediator.Send(new UpdateInventoryItemCommand(id, dto));
        return result.IsSuccess ? Ok(result.Data) : BadRequest(result.Error);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteInventoryItem(Guid id)
    {
        var result = await _mediator.Send(new DeleteInventoryItemCommand(id));
        return result.IsSuccess ? Ok(result.Data) : BadRequest(result.Error);
    }

    [HttpPost("{id:guid}/adjust")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AdjustStock(Guid id, [FromBody] AdjustStockDto dto)
    {
        var result = await _mediator.Send(new AdjustStockCommand(id, dto));
        return result.IsSuccess ? Ok(result.Data) : BadRequest(result.Error);
    }
}
