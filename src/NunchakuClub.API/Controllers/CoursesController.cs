using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NunchakuClub.Application.Features.Courses.Commands;
using NunchakuClub.Application.Features.Courses.DTOs;
using NunchakuClub.Application.Features.Courses.Queries;
using System;
using System.Threading.Tasks;

namespace NunchakuClub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CoursesController : ControllerBase
{
    private readonly IMediator _mediator;

    public CoursesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetCourses()
    {
        var query = new GetCoursesQuery();
        var result = await _mediator.Send(query);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(result.Error);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetCourseById(Guid id)
    {
        var query = new GetCourseByIdQuery(id);
        var result = await _mediator.Send(query);
        return result.IsSuccess ? Ok(result.Data) : NotFound(result.Error);
    }

    [HttpPost]
    [Authorize(Policy = "RequireAdminArea")]
    public async Task<IActionResult> CreateCourse([FromBody] CreateCourseDto dto)
    {
        var command = new CreateCourseCommand(dto);
        var result = await _mediator.Send(command);
        return result.IsSuccess ? Created("", result.Data) : BadRequest(result.Error);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "RequireAdminArea")]
    public async Task<IActionResult> UpdateCourse(Guid id, [FromBody] UpdateCourseDto dto)
    {
        var command = new UpdateCourseCommand(id, dto);
        var result = await _mediator.Send(command);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(result.Error);
    }
}
