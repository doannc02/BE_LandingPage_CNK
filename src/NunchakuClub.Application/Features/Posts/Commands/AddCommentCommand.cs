using MediatR;
using Microsoft.EntityFrameworkCore;
using NunchakuClub.Application.Common.Interfaces;
using NunchakuClub.Application.Common.Models;
using NunchakuClub.Domain.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NunchakuClub.Application.Features.Posts.Commands;

public record AddCommentCommand(
    Guid PostId,
    string Content,
    string AuthorName,
    string AuthorEmail,
    Guid? ParentId = null) : IRequest<Result<Guid>>;

public class AddCommentCommandHandler : IRequestHandler<AddCommentCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;

    public AddCommentCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<Guid>> Handle(
        AddCommentCommand request,
        CancellationToken cancellationToken)
    {
        var post = await _context.Posts.FindAsync(new object[] { request.PostId }, cancellationToken);
        if (post == null)
            return Result<Guid>.Failure("Bài viết không tồn tại");

        if (request.ParentId.HasValue)
        {
            var parentComment = await _context.Comments
                .FindAsync(new object[] { request.ParentId.Value }, cancellationToken);

            if (parentComment == null || parentComment.PostId != request.PostId)
                return Result<Guid>.Failure("Comment cha không hợp lệ");
        }

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == request.AuthorEmail, cancellationToken);

        if (user == null)
        {
            // Tạo user tạm thời cho comment
            user = new User
            {
                Id = Guid.NewGuid(),
                FullName = request.AuthorName,
                Email = request.AuthorEmail,
                Status = UserStatus.Inactive,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
        }

        // Tạo comment
        var comment = new Comment
        {
            Id = Guid.NewGuid(),
            Content = request.Content,
            PostId = request.PostId,
            AuthorName = request.AuthorName,
            AuthorEmail = request.AuthorEmail,
            UserId = user.Id,
            Status = CommentStatus.Pending,
            ParentId = request.ParentId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Comments.Add(comment);

        // Tăng comment count của bài viết
        post.CommentCount++;

        await _context.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(comment.Id);
    }
}