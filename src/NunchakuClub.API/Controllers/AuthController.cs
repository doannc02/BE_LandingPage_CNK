using System;
using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
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
        return result.IsSuccess ? Ok(result.Data) : BadRequest(result.Error);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginCommand command)
    {
        var result = await _mediator.Send(command);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(result.Error);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request)
    {
        var command = new RefreshTokenCommand(request.RefreshToken);
        var result = await _mediator.Send(command);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(result.Error);
    }

    /// <summary>
    /// SSO Identity Exchange — đổi Firebase ID Token lấy JWT nội bộ.
    /// Tự động tạo user mới với role Student nếu là lần đăng nhập đầu tiên.
    /// </summary>
    [HttpPost("exchange-token")]
    public async Task<IActionResult> ExchangeToken([FromBody] ExchangeTokenRequest request)
    {
        var command = new ExchangeTokenCommand(request.FirebaseIdToken);
        var result = await _mediator.Send(command);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(result.Error);
    }

    /// <summary>
    /// Phân quyền — chỉ SuperAdmin được phép gọi endpoint này.
    /// Nâng/hạ role của người dùng khác và đồng bộ xuống Firebase custom claims.
    /// </summary>
    [HttpPost("assign-role")]
    [Authorize(Policy = "RequireSuperAdmin")]
    public async Task<IActionResult> AssignRole([FromBody] AssignRoleRequest request)
    {
        var command = new AssignRoleCommand(request.TargetUserId, request.Role);
        var result = await _mediator.Send(command);
        return result.IsSuccess ? Ok() : BadRequest(result.Error);
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

    public class AssignRoleRequest
    {
        public Guid TargetUserId { get; set; }
        /// <summary>SuperAdmin | SubAdmin | Student</summary>
        public string Role { get; set; } = string.Empty;
    }
}
