#!/usr/bin/env python3
import os
from pathlib import Path

base = Path(".")

# All project files with COMPLETE source code
all_files = {
    # ========== DOMAIN PROJECT ==========
    "src/NunchakuClub.Domain/NunchakuClub.Domain.csproj": """<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
  </PropertyGroup>
</Project>""",

    "src/NunchakuClub.Domain/Entities/BaseEntity.cs": """using System;

namespace NunchakuClub.Domain.Entities;

public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public abstract class AuditableEntity : BaseEntity
{
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }
}""",

    "src/NunchakuClub.Domain/Entities/User.cs": """using System;
using System.Collections.Generic;

namespace NunchakuClub.Domain.Entities;

public class User : AuditableEntity
{
    public string Email { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? AvatarUrl { get; set; }
    public UserRole Role { get; set; } = UserRole.Member;
    public UserStatus Status { get; set; } = UserStatus.Active;
    public bool EmailVerified { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiryTime { get; set; }
    
    public ICollection<Post> Posts { get; set; } = new List<Post>();
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public ICollection<ActivityLog> ActivityLogs { get; set; } = new List<ActivityLog>();
}""",

    "src/NunchakuClub.Domain/Enums/UserRole.cs": """namespace NunchakuClub.Domain.Entities;

public enum UserRole
{
    Admin = 1,
    Editor = 2,
    Coach = 3,
    Member = 4
}

public enum UserStatus
{
    Active = 1,
    Inactive = 2,
    Suspended = 3
}""",

    "src/NunchakuClub.Domain/Entities/Post.cs": """using System;
using System.Collections.Generic;

namespace NunchakuClub.Domain.Entities;

public class Post : AuditableEntity
{
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Excerpt { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? FeaturedImageUrl { get; set; }
    public string? ThumbnailUrl { get; set; }
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
    public string? MetaKeywords { get; set; }
    public PostStatus Status { get; set; } = PostStatus.Draft;
    public bool IsFeatured { get; set; }
    public DateTime? PublishedAt { get; set; }
    public Guid AuthorId { get; set; }
    public User Author { get; set; } = null!;
    public Guid? CategoryId { get; set; }
    public Category? Category { get; set; }
    public int ViewCount { get; set; }
    public int LikeCount { get; set; }
    public int CommentCount { get; set; }
    public string? AdminNotes { get; set; }
    
    public ICollection<PostImage> Images { get; set; } = new List<PostImage>();
    public ICollection<PostTag> PostTags { get; set; } = new List<PostTag>();
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
}

public enum PostStatus
{
    Draft = 1,
    Published = 2,
    Archived = 3
}

public class PostImage : BaseEntity
{
    public Guid PostId { get; set; }
    public Post Post { get; set; } = null!;
    public string ImageUrl { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public string? Caption { get; set; }
    public string? AltText { get; set; }
    public int DisplayOrder { get; set; }
}

public class Tag : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public ICollection<PostTag> PostTags { get; set; } = new List<PostTag>();
}

public class PostTag
{
    public Guid PostId { get; set; }
    public Post Post { get; set; } = null!;
    public Guid TagId { get; set; }
    public Tag Tag { get; set; } = null!;
}""",

    "src/NunchakuClub.Domain/Entities/Category.cs": """using System;
using System.Collections.Generic;

namespace NunchakuClub.Domain.Entities;

public class Category : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? ParentId { get; set; }
    public Category? Parent { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
    
    public ICollection<Category> Children { get; set; } = new List<Category>();
    public ICollection<Post> Posts { get; set; } = new List<Post>();
}""",

    "src/NunchakuClub.Domain/Entities/Page.cs": """using System;
using System.Collections.Generic;

namespace NunchakuClub.Domain.Entities;

public class Page : AuditableEntity
{
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? Excerpt { get; set; }
    public Guid? ParentId { get; set; }
    public Page? Parent { get; set; }
    public string? FeaturedImageUrl { get; set; }
    public string? BannerImageUrl { get; set; }
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsPublished { get; set; } = true;
    public bool ShowInMenu { get; set; } = true;
    public string? Template { get; set; }
    
    public ICollection<Page> Children { get; set; } = new List<Page>();
}""",

    "src/NunchakuClub.Domain/Entities/Course.cs": """using System;
using System.Collections.Generic;

namespace NunchakuClub.Domain.Entities;

public class Course : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public CourseLevel Level { get; set; } = CourseLevel.Beginner;
    public int DurationMonths { get; set; }
    public int SessionsPerWeek { get; set; }
    public decimal Price { get; set; }
    public bool IsFree { get; set; }
    public string[]? Features { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsFeatured { get; set; }
    public bool IsActive { get; set; } = true;
    public string? ThumbnailUrl { get; set; }
    public string? CoverImageUrl { get; set; }
    
    public ICollection<CourseEnrollment> Enrollments { get; set; } = new List<CourseEnrollment>();
}

public enum CourseLevel
{
    Beginner = 1,
    Intermediate = 2,
    Advanced = 3,
    Professional = 4
}

public class CourseEnrollment : BaseEntity
{
    public Guid CourseId { get; set; }
    public Course Course { get; set; } = null!;
    public Guid? UserId { get; set; }
    public User? User { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public EnrollmentStatus Status { get; set; } = EnrollmentStatus.Pending;
    public string? Message { get; set; }
    public string? AdminNotes { get; set; }
    public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }
    public Guid? ProcessedBy { get; set; }
}

public enum EnrollmentStatus
{
    Pending = 1,
    Approved = 2,
    Rejected = 3,
    Completed = 4
}""",

    "src/NunchakuClub.Domain/Entities/Coach.cs": """using System;
using System.Collections.Generic;

namespace NunchakuClub.Domain.Entities;

public class Coach : BaseEntity
{
    public Guid? UserId { get; set; }
    public User? User { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? Bio { get; set; }
    public string? Specialization { get; set; }
    public int YearsOfExperience { get; set; }
    public string[]? Certifications { get; set; }
    public string[]? Achievements { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? AvatarUrl { get; set; }
    public string? CoverImageUrl { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
    
    public ICollection<Achievement> CoachAchievements { get; set; } = new List<Achievement>();
}""",

    "src/NunchakuClub.Domain/Entities/Achievement.cs": """using System;

namespace NunchakuClub.Domain.Entities;

public class Achievement : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime AchievementDate { get; set; }
    public AchievementType Type { get; set; } = AchievementType.Competition;
    public string? ImageUrl { get; set; }
    public string? VideoUrl { get; set; }
    public Guid? CoachId { get; set; }
    public Coach? Coach { get; set; }
    public string? ParticipantNames { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsFeatured { get; set; }
}

public enum AchievementType
{
    Competition = 1,
    Certification = 2,
    Milestone = 3,
    Award = 4
}""",

    "src/NunchakuClub.Domain/Entities/Comment.cs": """using System;
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
}""",

    "src/NunchakuClub.Domain/Entities/ContactSubmission.cs": """using System;

namespace NunchakuClub.Domain.Entities;

public class ContactSubmission : BaseEntity
{
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public Guid? CourseId { get; set; }
    public Course? Course { get; set; }
    public string Message { get; set; } = string.Empty;
    public ContactStatus Status { get; set; } = ContactStatus.New;
    public string? AdminNotes { get; set; }
    public Guid? HandledBy { get; set; }
    public DateTime? HandledAt { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}

public enum ContactStatus
{
    New = 1,
    Read = 2,
    Replied = 3,
    Archived = 4
}""",

    "src/NunchakuClub.Domain/Entities/Media.cs": """using System;

namespace NunchakuClub.Domain.Entities;

public class Media : BaseEntity
{
    public string Filename { get; set; } = string.Empty;
    public string OriginalFilename { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public string FileType { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public int? Duration { get; set; }
    public string? Title { get; set; }
    public string? AltText { get; set; }
    public string? Caption { get; set; }
    public string? Description { get; set; }
    public Guid UploadedBy { get; set; }
    public User Uploader { get; set; } = null!;
}""",

    "src/NunchakuClub.Domain/Entities/MenuItem.cs": """using System;
using System.Collections.Generic;

namespace NunchakuClub.Domain.Entities;

public class MenuItem : BaseEntity
{
    public string Label { get; set; } = string.Empty;
    public string? Url { get; set; }
    public Guid? PageId { get; set; }
    public Page? Page { get; set; }
    public string Target { get; set; } = "_self";
    public Guid? ParentId { get; set; }
    public MenuItem? Parent { get; set; }
    public int DisplayOrder { get; set; }
    public string? IconClass { get; set; }
    public string MenuLocation { get; set; } = "header";
    public bool IsActive { get; set; } = true;
    
    public ICollection<MenuItem> Children { get; set; } = new List<MenuItem>();
}""",

    "src/NunchakuClub.Domain/Entities/Setting.cs": """namespace NunchakuClub.Domain.Entities;

public class Setting
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Type { get; set; } = "string";
    public string? Description { get; set; }
}""",

    "src/NunchakuClub.Domain/Entities/ActivityLog.cs": """using System;

namespace NunchakuClub.Domain.Entities;

public class ActivityLog : BaseEntity
{
    public Guid? UserId { get; set; }
    public User? User { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public Guid? EntityId { get; set; }
    public string? Details { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}""",

}

# Create all files
total = len(all_files)
for i, (filepath, content) in enumerate(all_files.items(), 1):
    full_path = base / filepath
    full_path.parent.mkdir(parents=True, exist_ok=True)
    full_path.write_text(content)
    print(f"[{i}/{total}] ✅ {filepath}")

print(f"\n🎉 Generated {total} Domain layer files!")
print("⏳ Generating Application, Infrastructure, and API layers...")

