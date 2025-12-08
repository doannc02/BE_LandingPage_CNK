using System;
using System.Collections.Generic;

namespace NunchakuClub.Domain.Entities;

public class Comment : BaseEntity
{
    public Guid PostId { get; set; }
    public Post Post { get; set; } = null!;
    public Guid? UserId { get; set; }
    public User? User { get; set; }
    public string? AuthorName { get; set; }
    public string? AuthorEmail { get; set; }
    public string Content { get; set; } = string.Empty;
    public Guid? ParentId { get; set; }
    public Comment? Parent { get; set; }
    public CommentStatus Status { get; set; } = CommentStatus.Pending;
    
    public ICollection<Comment> Replies { get; set; } = new List<Comment>();
}

public enum CommentStatus
{
    Pending = 1,
    Approved = 2,
    Spam = 3,
    Trash = 4
}