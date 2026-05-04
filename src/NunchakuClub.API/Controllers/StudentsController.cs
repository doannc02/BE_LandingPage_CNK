using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NunchakuClub.Application.Features.Students.Commands;
using NunchakuClub.Application.Features.Students.DTOs;
using NunchakuClub.Application.Features.Students.Queries;
using System;
using System.Threading.Tasks;

namespace NunchakuClub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StudentsController : ControllerBase
{
    private readonly IMediator _mediator;

    public StudentsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // GET: api/students?branchId=guid
    [HttpGet]
    public async Task<IActionResult> GetStudents([FromQuery] Guid? branchId = null)
    {
        var result = await _mediator.Send(new GetStudentsQuery(branchId));
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    // GET: api/students/{id}
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetStudentById(Guid id)
    {
        var result = await _mediator.Send(new GetStudentByIdQuery(id));
        return result.IsSuccess ? Ok(result) : NotFound(result);
    }

    // POST: api/students
    [HttpPost]
    [Authorize(Policy = "RequireAdminArea")]
    public async Task<IActionResult> CreateStudent([FromBody] CreateStudentDto dto)
    {
        var result = await _mediator.Send(new CreateStudentCommand(dto));
        return result.IsSuccess ? Created(string.Empty, result) : BadRequest(result);
    }

    // PUT: api/students/{id}
    [HttpPut("{id:guid}")]
    [Authorize(Policy = "RequireAdminArea")]
    public async Task<IActionResult> UpdateStudent(Guid id, [FromBody] UpdateStudentDto dto)
    {
        var result = await _mediator.Send(new UpdateStudentCommand(id, dto));
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    // DELETE: api/students/{id}
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "RequireAdminArea")]
    public async Task<IActionResult> DeleteStudent(Guid id)
    {
        var result = await _mediator.Send(new DeleteStudentCommand(id));
        return result.IsSuccess ? NoContent() : BadRequest(result);
    }
}
