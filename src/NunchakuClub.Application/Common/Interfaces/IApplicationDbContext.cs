using Microsoft.EntityFrameworkCore;
using NunchakuClub.Domain.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace NunchakuClub.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<Post> Posts { get; }
    DbSet<PostImage> PostImages { get; }
    DbSet<PostTag> PostTags { get; }
    DbSet<Tag> Tags { get; }
    DbSet<Category> Categories { get; }
    DbSet<Page> Pages { get; }
    DbSet<MenuItem> MenuItems { get; }
    DbSet<Course> Courses { get; }
    DbSet<CourseEnrollment> CourseEnrollments { get; }
    DbSet<Coach> Coaches { get; }
    DbSet<Achievement> Achievements { get; }
    DbSet<Branch> Branches { get; }
    DbSet<BeltRank> BeltRanks { get; }
    DbSet<BeltHistory> BeltHistories { get; }
    DbSet<BranchCoach> BranchCoaches { get; }
    DbSet<BranchGallery> BranchGalleries { get; }
    DbSet<Comment> Comments { get; }
    DbSet<ContactSubmission> ContactSubmissions { get; }
    DbSet<StudentProfile> StudentProfiles { get; }
    DbSet<AttendanceSession> AttendanceSessions { get; }
    DbSet<AttendanceRecord> AttendanceRecords { get; }
    DbSet<Media> MediaFiles { get; }
    DbSet<Setting> Settings { get; }
    DbSet<ActivityLog> ActivityLogs { get; }
    DbSet<KnowledgeDocument> KnowledgeDocuments { get; }
    DbSet<PendingUserMessage> PendingUserMessages { get; }
    DbSet<ChatSession> ChatSessions { get; }
    DbSet<ConversationMessage> ConversationMessages { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
