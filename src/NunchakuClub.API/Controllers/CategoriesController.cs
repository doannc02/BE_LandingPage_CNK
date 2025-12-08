using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NunchakuClub.Application.Features.Categories.Commands;
using NunchakuClub.Application.Features.Categories.DTOs;
using NunchakuClub.Application.Features.Categories.Queries;
using System.Threading.Tasks;

namespace NunchakuClub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    private readonly IMediator _mediator;

    public CategoriesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetCategories([FromQuery] bool includeChildren = false)
    {
        var query = new GetCategoriesQuery(includeChildren);
        var result = await _mediator.Send(query);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(result.Error);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryDto dto)
    {
        var command = new CreateCategoryCommand(dto);
        var result = await _mediator.Send(command);
        return result.IsSuccess ? Created("", result.Data) : BadRequest(result.Error);
    }
}
