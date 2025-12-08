using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NunchakuClub.Application.Features.ContactSubmissions.Commands;
using NunchakuClub.Application.Features.ContactSubmissions.DTOs;
using NunchakuClub.Application.Features.ContactSubmissions.Queries;
using System.Threading.Tasks;

namespace NunchakuClub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ContactController : ControllerBase
{
    private readonly IMediator _mediator;

    public ContactController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> Submit([FromBody] CreateContactSubmissionDto dto)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = Request.Headers["User-Agent"].ToString();
        
        var command = new CreateContactSubmissionCommand(dto, ipAddress, userAgent);
        var result = await _mediator.Send(command);
        
        return result.IsSuccess ? Ok(result.Data) : BadRequest(result.Error);
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetSubmissions([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
    {
        var query = new GetContactSubmissionsQuery(pageNumber, pageSize);
        var result = await _mediator.Send(query);
        
        return result.IsSuccess ? Ok(result.Data) : BadRequest(result.Error);
    }
}
