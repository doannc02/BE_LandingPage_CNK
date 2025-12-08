using System;
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
}