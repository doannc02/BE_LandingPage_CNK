using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NunchakuClub.Application.Features.InventoryCategories.Commands;
using NunchakuClub.Application.Features.InventoryCategories.DTOs;
using NunchakuClub.Application.Features.InventoryCategories.Queries;
using System;
using System.Threading.Tasks;

namespace NunchakuClub.API.Controllers;

[ApiController]
[Route("api/inventory-categories")]
public class InventoryCategoriesController : ControllerBase
{
    private readonly IMediator _mediator;

    public InventoryCategoriesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] bool? isActive)
    {
        var result = await _mediator.Send(new GetInventoryCategoryListQuery(isActive));
        return result.IsSuccess ? Ok(result.Data) : BadRequest(result.Error);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateInventoryCategoryDto dto)
    {
        var result = await _mediator.Send(new CreateInventoryCategoryCommand(dto));
        return result.IsSuccess ? Created("", result.Data) : BadRequest(result.Error);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateInventoryCategoryDto dto)
    {
        var result = await _mediator.Send(new UpdateInventoryCategoryCommand(id, dto));
        return result.IsSuccess ? Ok(result.Data) : BadRequest(result.Error);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _mediator.Send(new DeleteInventoryCategoryCommand(id));
        return result.IsSuccess ? Ok(result.Data) : BadRequest(result.Error);
    }
}
