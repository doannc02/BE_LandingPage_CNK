using System;
using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NunchakuClub.Application.Features.Auth.Commands;
using NunchakuClub.Application.Features.Users.Commands;
using NunchakuClub.Application.Features.Users.Queries;
using System.Threading.Tasks;

namespace NunchakuClub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;

    public UsersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private Guid CurrentUserId =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("Missing nameid claim"));

    private bool IsAdminArea =>
        User.IsInRole("SuperAdmin") || User.IsInRole("SubAdmin");

    // ── GET /api/users (Admin area) ───────────────────────────────────────────

    /// <summary>
    /// [RequireAdminArea] Lấy danh sách người dùng có phân trang.
    /// Query params: pageNumber, pageSize, searchTerm, role, status
    /// </summary>
    [HttpGet]
    [Authorize(Policy = "RequireAdminArea")]
    public async Task<IActionResult> GetUsers(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? searchTerm = null,
        [FromQuery] string? role = null,
        [FromQuery] string? status = null)
    {
        var query = new GetUsersQuery(pageNumber, pageSize, searchTerm, role, status);
        var result = await _mediator.Send(query);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(result.Error);
    }

    // ── GET /api/users/me ─────────────────────────────────────────────────────

    /// <summary>
    /// [Authenticated] Lấy thông tin cá nhân của user đang đăng nhập.
    /// </summary>
    [HttpGet("me")]
    public async Task<IActionResult> GetMe()
    {
        var result = await _mediator.Send(new GetUserByIdQuery(CurrentUserId));
        return result.IsSuccess ? Ok(result.Data) : BadRequest(result.Error);
    }

    // ── GET /api/users/{id} ───────────────────────────────────────────────────

    /// <summary>
    /// [Admin: bất kỳ user | Student: chỉ được xem chính mình]
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        // Student chỉ được xem chính mình
        if (!IsAdminArea && id != CurrentUserId)
            return Forbid();

        var result = await _mediator.Send(new GetUserByIdQuery(id));
        return result.IsSuccess ? Ok(result.Data) : NotFound(result.Error);
    }

    // ── PUT /api/users/{id} ───────────────────────────────────────────────────

    /// <summary>
    /// [Admin: bất kỳ user | Student: chỉ chính mình]
    /// Cập nhật fullName, phone, avatarUrl.
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateProfile(Guid id, [FromBody] UpdateProfileRequest request)
    {
        if (!IsAdminArea && id != CurrentUserId)
            return Forbid();

        var command = new UpdateUserProfileCommand(id, request.FullName, request.Phone, request.AvatarUrl);
        var result = await _mediator.Send(command);
        return result.IsSuccess ? Ok(result.Data) : BadRequest(result.Error);
    }

    // ── PATCH /api/users/{id}/status ─────────────────────────────────────────

    /// <summary>
    /// [RequireAdminArea] Thay đổi trạng thái tài khoản: Active | Inactive | Suspended.
    /// </summary>
    [HttpPatch("{id:guid}/status")]
    [Authorize(Policy = "RequireAdminArea")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateStatusRequest request)
    {
        var command = new UpdateUserStatusCommand(id, request.Status);
        var result = await _mediator.Send(command);
        return result.IsSuccess ? Ok() : BadRequest(result.Error);
    }

    // ── POST /api/users/{id}/assign-role ────────────────────────────────────

    /// <summary>
    /// [RequireSuperAdmin] Phân quyền cho người dùng: SuperAdmin | SubAdmin | Student.
    /// </summary>
    [HttpPost("{id:guid}/assign-role")]
    [Authorize(Policy = "RequireSuperAdmin")]
    public async Task<IActionResult> AssignRole(Guid id, [FromBody] AssignRoleRequest request)
    {
        var command = new AssignRoleCommand(id, request.Role);
        var result = await _mediator.Send(command);
        return result.IsSuccess ? Ok() : BadRequest(result.Error);
    }

    // ── DELETE /api/users/{id} ────────────────────────────────────────────────

    /// <summary>
    /// [RequireSuperAdmin] Xóa người dùng. Không thể xóa chính mình hoặc SuperAdmin khác.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "RequireSuperAdmin")]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        var command = new DeleteUserCommand(id, CurrentUserId);
        var result = await _mediator.Send(command);
        return result.IsSuccess ? NoContent() : BadRequest(result.Error);
    }

    // ── Request DTOs ──────────────────────────────────────────────────────────

    public class UpdateProfileRequest
    {
        public string FullName { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? AvatarUrl { get; set; }
    }

    public class UpdateStatusRequest
    {
        /// <summary>Active | Inactive | Suspended</summary>
        public string Status { get; set; } = string.Empty;
    }

    public class AssignRoleRequest
    {
        /// <summary>SuperAdmin | SubAdmin | Student</summary>
        public string Role { get; set; } = string.Empty;
    }
}
