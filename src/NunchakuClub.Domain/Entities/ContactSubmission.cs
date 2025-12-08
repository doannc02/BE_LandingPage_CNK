using System;

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
}