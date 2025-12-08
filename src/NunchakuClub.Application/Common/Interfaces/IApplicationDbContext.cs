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
    DbSet<Comment> Comments { get; }
    DbSet<ContactSubmission> ContactSubmissions { get; }
    DbSet<Media> MediaFiles { get; }
    DbSet<Setting> Settings { get; }
    DbSet<ActivityLog> ActivityLogs { get; }
    
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
