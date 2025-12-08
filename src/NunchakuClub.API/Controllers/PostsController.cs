using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NunchakuClub.Application.Features.Posts.Commands;
using NunchakuClub.Application.Features.Posts.DTOs;
using NunchakuClub.Application.Features.Posts.Queries;
using NunchakuClub.Domain.Entities;
using System.Security.Claims;
using System;
using System.Threading.Tasks;


namespace NunchakuClub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PostsController : ControllerBase
{
    private readonly IMediator _mediator;

    public PostsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get all posts with pagination and filters
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetPosts(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? searchTerm = null,
        [FromQuery] Guid? categoryId = null,
        [FromQuery] PostStatus? status = null,
        [FromQuery] bool? isFeatured = null)
    {
        var query = new GetPostsQuery(pageNumber, pageSize, searchTerm, categoryId, status, isFeatured);
        var result = await _mediator.Send(query);
        
        return result.IsSuccess 
            ? Ok(result.Data) 
            : BadRequest(result.Error);
    }

    /// <summary>
    /// Get post by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetPostById(Guid id)
    {
        var query = new GetPostByIdQuery(id);
        var result = await _mediator.Send(query);
        
        return result.IsSuccess 
            ? Ok(result.Data) 
            : NotFound(result.Error);
    }

    /// <summary>
    /// Get post by slug
    /// </summary>
    [HttpGet("slug/{slug}")]
    public async Task<IActionResult> GetPostBySlug(string slug)
    {
        var query = new GetPostBySlugQuery(slug);
        var result = await _mediator.Send(query);
        
        return result.IsSuccess 
            ? Ok(result.Data) 
            : NotFound(result.Error);
    }

    /// <summary>
    /// Create new post (Admin/Editor only)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin,Editor")]
    public async Task<IActionResult> CreatePost([FromBody] CreatePostDto dto)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var command = new CreatePostCommand(dto, userId);
        var result = await _mediator.Send(command);
        
        return result.IsSuccess 
            ? CreatedAtAction(nameof(GetPostById), new { id = result.Data }, result.Data)
            : BadRequest(result.Error);
    }

    /// <summary>
    /// Update post (Admin/Editor only)
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,Editor")]
    public async Task<IActionResult> UpdatePost(Guid id, [FromBody] UpdatePostDto dto)
    {
        var command = new UpdatePostCommand(id, dto);
        var result = await _mediator.Send(command);
        
        return result.IsSuccess 
            ? Ok(result.Data) 
            : BadRequest(result.Error);
    }

    /// <summary>
    /// Delete post (Admin only)
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeletePost(Guid id)
    {
        var command = new DeletePostCommand(id);
        var result = await _mediator.Send(command);
        
        return result.IsSuccess 
            ? NoContent() 
            : BadRequest(result.Error);
    }

    /// <summary>
    /// Publish post (Admin/Editor only)
    /// </summary>
    [HttpPost("{id:guid}/publish")]
    [Authorize(Roles = "Admin,Editor")]
    public async Task<IActionResult> PublishPost(Guid id)
    {
        var command = new PublishPostCommand(id);
        var result = await _mediator.Send(command);
        
        return result.IsSuccess 
            ? Ok(result.Data) 
            : BadRequest(result.Error);
    }
}
