using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NunchakuClub.Application.Features.Achievements.Commands;
using NunchakuClub.Application.Features.Achievements.DTOs;
using NunchakuClub.Application.Features.Achievements.Queries;
using System;
using System.Threading.Tasks;

namespace NunchakuClub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AchievementsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AchievementsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetAchievements(
        [FromQuery] bool? featured = null,
        [FromQuery] Guid? coachId = null)
    {
        var query = new GetAchievementsQuery(featured, coachId);
        var result = await _mediator.Send(query);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(result.Error);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Editor")]
    public async Task<IActionResult> CreateAchievement([FromBody] CreateAchievementDto dto)
    {
        var command = new CreateAchievementCommand(dto);
        var result = await _mediator.Send(command);
        return result.IsSuccess ? Created("", result.Data) : BadRequest(result.Error);
    }
}
