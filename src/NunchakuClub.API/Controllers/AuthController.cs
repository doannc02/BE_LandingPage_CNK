using MediatR;
using Microsoft.AspNetCore.Mvc;
using NunchakuClub.Application.Features.Auth.Commands;
using System.Threading.Tasks;

namespace NunchakuClub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterCommand command)
    {
        var result = await _mediator.Send(command);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginCommand command)
    {
        var result = await _mediator.Send(command);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request)
    {
        var command = new RefreshTokenCommand(request.RefreshToken);
        var result = await _mediator.Send(command);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// SSO Identity Exchange — đổi Firebase ID Token lấy JWT nội bộ.
    /// Tự động tạo user mới với role Guest nếu là lần đăng nhập đầu tiên.
    /// </summary>
    [HttpPost("exchange-token")]
    public async Task<IActionResult> ExchangeToken([FromBody] ExchangeTokenRequest request)
    {
        var command = new ExchangeTokenCommand(request.FirebaseIdToken);
        var result = await _mediator.Send(command);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    // ── Request DTOs ──────────────────────────────────────────────────────────

    public class RefreshTokenRequest
    {
        public string RefreshToken { get; set; } = string.Empty;
    }

    public class ExchangeTokenRequest
    {
        public string FirebaseIdToken { get; set; } = string.Empty;
    }
}
