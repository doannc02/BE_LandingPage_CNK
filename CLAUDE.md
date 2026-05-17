# CLAUDE.md

Guidance for Claude Code when working in this repository. **All agents must read this file in full before writing any code.**

---

## Project Overview

.NET 8 backend API for **Câu lạc bộ Côn Nhị Khúc Hà Đông (Nunchaku Club)**, built with Clean Architecture + CQRS (MediatR). PostgreSQL on Aiven, Firebase Realtime DB + FCM for live chat, Gemini 1.5 Flash for AI, pgvector for RAG, AWS S3 for media.

---

## Common Commands

```powershell
dotnet restore
dotnet build
dotnet run --project src/NunchakuClub.API

# EF Core
dotnet ef database update --project src/NunchakuClub.Infrastructure --startup-project src/NunchakuClub.API
dotnet ef migrations add <MigrationName> --project src/NunchakuClub.Infrastructure --startup-project src/NunchakuClub.API

# Docker
docker compose up -d
docker compose down
docker compose logs -f
```

Swagger: `https://localhost:52040/swagger`

---

## Architecture

| Layer | Project | Responsibility |
|---|---|---|
| Domain | `NunchakuClub.Domain` | Entities, enums, value objects — zero external dependencies |
| Application | `NunchakuClub.Application` | CQRS handlers (MediatR), DTOs, interfaces, validators (FluentValidation) |
| Infrastructure | `NunchakuClub.Infrastructure` | EF Core, JWT, BCrypt, AWS S3, Redis, Firebase, Gemini AI |
| API | `NunchakuClub.API` | Controllers, middleware, DI registration, `Program.cs` |

---

## Code Conventions — MUST FOLLOW

### Using Directives

**Always explicit `using` statements at the top of every file.** `ImplicitUsings` is disabled across all projects. Never rely on globally-injected namespaces.

```csharp
// CORRECT — explicit usings at file top
using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using NunchakuClub.Application.Common.Interfaces;

namespace NunchakuClub.Application.Features.Posts.Commands;
```

### File-Scoped Namespaces

Use file-scoped namespace declarations (`namespace Foo.Bar;`) — no nested braces.

### Naming

| Artifact | Convention | Example |
|---|---|---|
| Command/Query record | `<Verb><Noun>Command` / `<Verb><Noun>Query` | `CreateCoachCommand`, `GetPostsQuery` |
| Handler class | `<CommandOrQueryName>Handler` | `CreateCoachCommandHandler` |
| DTO (input) | `Create<Noun>Dto`, `Update<Noun>Dto` | `CreatePostDto` |
| DTO (output) | `<Noun>Dto` or `<Noun>DTOs.cs` (multiple in one file) | `PostDto`, `AchievementDTOs.cs` |
| Interface | `I<PascalCase>` | `IApplicationDbContext` |
| DB table | `snake_case` configured in `IEntityTypeConfiguration` | `"post_tags"`, `"users"` |
| Migration name | `<PascalCaseDescription>` | `AddPgvectorKnowledgeDocuments` |
| Controller | `<PluralNoun>Controller` | `PostsController` |

### No Comments by Default

Only add a comment when the **WHY** is non-obvious — hidden constraint, workaround, subtle invariant. Never comment the WHAT.

---

## Domain Layer

### Entity Base Classes

All entities inherit from one of:

```csharp
// BaseEntity — for entities without audit trail (e.g., PostTag, Tag)
public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

// AuditableEntity — for entities with creator/updater tracking
public abstract class AuditableEntity : BaseEntity
{
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }
}
```

- Place entities in `src/NunchakuClub.Domain/Entities/`
- Related small types (enums, value sub-entities) can live in the same file as the aggregate root
- Default string properties to `string.Empty`, not `null`, unless nullable is intentional

```csharp
// In Domain project
public class Coach : AuditableEntity  // or BaseEntity if no author tracking needed
{
    public string FullName { get; set; } = string.Empty;
    public string? Bio { get; set; }           // nullable = optional
    public bool IsActive { get; set; } = true;
    public ICollection<BranchCoach> BranchCoaches { get; set; } = new List<BranchCoach>();
}
```

---

## Application Layer

### CQRS: Command + Handler in Same File

Command/Query record and its Handler class live in **one file**. Group with visual separators when the file has multiple logical sections.

```csharp
// src/NunchakuClub.Application/Features/<Feature>/Commands/Create<Noun>Command.cs
using MediatR;
using NunchakuClub.Application.Common.Interfaces;
using NunchakuClub.Application.Common.Models;
using NunchakuClub.Application.Features.<Feature>.DTOs;
using NunchakuClub.Domain.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NunchakuClub.Application.Features.<Feature>.Commands;

public record Create<Noun>Command(Create<Noun>Dto Dto, Guid AuthorId) : IRequest<Result<Guid>>;

public class Create<Noun>CommandHandler : IRequestHandler<Create<Noun>Command, Result<Guid>>
{
    private readonly IApplicationDbContext _context;

    public Create<Noun>CommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<Guid>> Handle(Create<Noun>Command request, CancellationToken cancellationToken)
    {
        var entity = new <Noun>
        {
            // map from request.Dto
        };

        _context.<DbSet>.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(entity.Id);
    }
}
```

### Result<T> — API Response Wrapper

**All handlers return `Result<T>` or `Result`.** Never throw exceptions for business failures.

```csharp
// Success cases
return Result<Guid>.Success(entity.Id);
return Result<PostDto>.Success(dto);
return Result.Success();

// Failure cases (business logic)
return Result<PostDto>.Failure("Post not found");
return Result.Failure("Invalid credentials");
```

### PaginatedList<T> — List Queries

Use `PaginatedList<T>` for list queries. Use `ToPaginatedListAsync()` extension.

```csharp
public record GetItemsQuery(
    int PageNumber = 1,
    int PageSize = 10,
    string? SearchTerm = null
) : IRequest<Result<PaginatedList<ItemDto>>>;
```

### Query Pattern — Projection in Select

Always project to DTO **inside** the LINQ query (not after materializing). Use `.AsQueryable()` + filters + `.Select()` + `ToPaginatedListAsync()`.

```csharp
var query = _context.Posts
    .Include(p => p.Author)
    .AsQueryable();

if (!string.IsNullOrWhiteSpace(request.SearchTerm))
    query = query.Where(p => p.Title.Contains(request.SearchTerm));

query = query.OrderByDescending(p => p.PublishedAt ?? p.CreatedAt);

var projected = query.Select(p => new PostDto { ... });
var result = await projected.ToPaginatedListAsync(request.PageNumber, request.PageSize, cancellationToken);
return Result<PaginatedList<PostDto>>.Success(result);
```

### Key Interfaces

Defined in `src/NunchakuClub.Application/Common/Interfaces/`:

| Interface | Purpose |
|---|---|
| `IApplicationDbContext` | EF Core DB access — all DbSets |
| `IJwtTokenGenerator` | JWT access + refresh token creation |
| `IPasswordHasher` | BCrypt verify/hash |
| `ICloudStorageService` | AWS S3 upload/delete |
| `ICacheService` | Redis get/set |
| `IAiChatService` | Gemini streaming chat |
| `IKnowledgeBaseService` | pgvector RAG retrieval |
| `IEmbeddingService` | Gemini text-embedding |
| `IFirebasePresenceService` | Online admin detection |
| `IFirebaseChatService` | Firebase RTDB chat room CRUD |
| `IFcmNotificationService` | FCM push notifications to admins |
| `IFallbackClassifierService` | AI confidence + human-handoff decision |
| `IFirebaseAuthService` | Firebase ID token verification (SSO) |
| `IVideoStorageService` | Video upload |

When adding a new infrastructure service, **always define the interface in Application layer first**, then implement in Infrastructure.

---

## Infrastructure Layer

### EF Core Entity Configuration

Every entity needs a class implementing `IEntityTypeConfiguration<T>` in:
`src/NunchakuClub.Infrastructure/Data/Configuratoins/`  *(note: folder name has a typo — keep it consistent)*

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NunchakuClub.Domain.Entities;

namespace NunchakuClub.Infrastructure.Data.Configurations;

public class CoachConfiguration : IEntityTypeConfiguration<Coach>
{
    public void Configure(EntityTypeBuilder<Coach> builder)
    {
        builder.ToTable("coaches");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.FullName)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(c => c.Bio)
            .HasMaxLength(2000);

        // Enum → string
        builder.Property(c => c.SomeEnum)
            .HasConversion<string>()
            .HasMaxLength(50);

        // Relationships
        builder.HasMany(c => c.BranchCoaches)
            .WithOne(bc => bc.Coach)
            .HasForeignKey(bc => bc.CoachId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

Rules:
- Table names are **snake_case** via `.ToTable("table_name")`
- Enums stored as strings: `.HasConversion<string>()`
- All `IsRequired()` strings must have `HasMaxLength()`
- Partial/optional unique indexes: use `.HasFilter("column IS NOT NULL")`

### IApplicationDbContext — Adding a DbSet

When adding a new entity, add its `DbSet<T>` to the interface and the concrete `ApplicationDbContext`.

---

## API Layer

### Controller Template

Controllers are **thin** — dispatch to MediatR, map `Result<T>` to HTTP responses. No business logic.

```csharp
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NunchakuClub.Application.Features.<Feature>.Commands;
using NunchakuClub.Application.Features.<Feature>.DTOs;
using NunchakuClub.Application.Features.<Feature>.Queries;
using System;
using System.Threading.Tasks;

namespace NunchakuClub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class <Noun>sController : ControllerBase
{
    private readonly IMediator _mediator;

    public <Noun>sController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Get all <nouns> with pagination</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        var result = await _mediator.Send(new Get<Noun>sQuery(pageNumber, pageSize));
        return result.IsSuccess ? Ok(result.Data) : BadRequest(result.Error);
    }

    /// <summary>Get <noun> by ID</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _mediator.Send(new Get<Noun>ByIdQuery(id));
        return result.IsSuccess ? Ok(result.Data) : NotFound(result.Error);
    }

    /// <summary>Create <noun> (Admin only)</summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] Create<Noun>Dto dto)
    {
        var result = await _mediator.Send(new Create<Noun>Command(dto));
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Data }, result.Data)
            : BadRequest(result.Error);
    }

    /// <summary>Update <noun> (Admin only)</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] Update<Noun>Dto dto)
    {
        var result = await _mediator.Send(new Update<Noun>Command(id, dto));
        return result.IsSuccess ? Ok(result.Data) : BadRequest(result.Error);
    }

    /// <summary>Delete <noun> (Admin only)</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _mediator.Send(new Delete<Noun>Command(id));
        return result.IsSuccess ? NoContent() : BadRequest(result.Error);
    }
}
```

### HTTP Status Mapping

| Scenario | Status Code |
|---|---|
| Success with data | `Ok(result.Data)` |
| Created | `CreatedAtAction(...)` |
| Success, no content | `NoContent()` |
| Not found | `NotFound(result.Error)` |
| Business rule failure | `BadRequest(result.Error)` |
| Unauthorized | `Unauthorized()` |

### Auth Roles

Available roles (defined as strings): `Admin`, `Editor`, `Coach`, `Member`, `Guest`

```csharp
[Authorize]                              // any authenticated user
[Authorize(Roles = "Admin")]             // admin only
[Authorize(Roles = "Admin,Editor")]      // admin or editor
[Authorize(Roles = "Admin,Coach")]       // admin or coach
```

Extract the current user's ID from claims:
```csharp
var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
```

### Request DTOs Nested in Controller

For simple request bodies that are only used by one endpoint, nest the DTO class inside the controller:
```csharp
public class AddCommentRequest
{
    public string Content { get; set; } = string.Empty;
    public Guid? ParentId { get; set; }
}
```

---

## Adding a New Feature — Step-by-Step

Follow this order exactly:

1. **Domain** — Add entity class to `src/NunchakuClub.Domain/Entities/<Entity>.cs`
   - Inherit `BaseEntity` or `AuditableEntity`
   - Include navigation properties and `ICollection<>` for relations

2. **Infrastructure: DbContext** — Add `DbSet<Entity>` to `IApplicationDbContext` interface and `ApplicationDbContext`

3. **Infrastructure: Configuration** — Create `src/NunchakuClub.Infrastructure/Data/Configuratoins/<Entity>Configuration.cs` implementing `IEntityTypeConfiguration<Entity>`

4. **Migration** — `dotnet ef migrations add <MigrationName> --project src/NunchakuClub.Infrastructure --startup-project src/NunchakuClub.API`

5. **Application: DTOs** — Create `src/NunchakuClub.Application/Features/<Feature>/DTOs/<Feature>DTOs.cs`

6. **Application: Interface** (if new service needed) — Add interface to `src/NunchakuClub.Application/Common/Interfaces/`

7. **Application: Commands** — Create one file per command in `src/NunchakuClub.Application/Features/<Feature>/Commands/`

8. **Application: Queries** — Create one file per query in `src/NunchakuClub.Application/Features/<Feature>/Queries/`

9. **Infrastructure: Service impl** (if new interface) — Implement in `src/NunchakuClub.Infrastructure/Services/`

10. **API: Controller** — Create `src/NunchakuClub.API/Controllers/<Feature>Controller.cs`

11. **DI Registration** — Register new services in `Program.cs` or the relevant `DependencyInjection.cs` extension

---

## Key Technologies & Integration Points

### Firebase
- **Presence** (`IFirebasePresenceService`): detects which admins are currently online via Firebase RTDB `/presence/` node
- **Chat** (`IFirebaseChatService`): creates chat rooms in RTDB, reads admin workloads for least-loaded routing
- **FCM** (`IFcmNotificationService`): push notifications to all admins when no one is online
- **Auth** (`IFirebaseAuthService`): verifies Firebase ID Token for SSO (`POST /api/auth/exchange-token`)

### AI / Chat Pipeline
- **Gemini 1.5 Flash** via `GeminiChatService` (Semantic Kernel)
- **pgvector** on PostgreSQL for knowledge base embeddings (`KnowledgeDocument` entity, `IEmbeddingService`)
- **RAG pipeline**: `IKnowledgeBaseService.RetrieveAsync()` → embed query → cosine similarity search
- **Confidence threshold**: `0.75f` — below this, route to human

### AWS S3
- Bucket: `nunchaku-club-media`
- Interface: `ICloudStorageService`

### Caching
- `ICacheService` wraps `IMemoryCache` (MemoryCacheService)
- Redis client also available; prefer Redis for shared/multi-instance scenarios

---

## EF Core Patterns

### Async DB Access
Always pass `CancellationToken cancellationToken` through to all async EF calls:
```csharp
await _context.Posts.AnyAsync(p => p.Slug == slug, cancellationToken);
await _context.SaveChangesAsync(cancellationToken);
```

### Concurrency Conflicts
For high-contention writes, use retry pattern:
```csharp
catch (DbUpdateConcurrencyException ex)
{
    foreach (var entry in ex.Entries)
        await entry.ReloadAsync(ct);
    await _db.SaveChangesAsync(ct);
}
```

### No Lazy Loading
Navigation properties are **explicit-load only** via `.Include()`. Never rely on lazy loading.

---

## Configuration

Key `appsettings.json` sections:
- `ConnectionStrings:DefaultConnection` — PostgreSQL (Aiven in prod)
- `JwtSettings` — `Secret`, `Issuer`, `Audience`, access/refresh expiry
- `AwsS3` — `BucketName`, `Region`, credentials
- `GeminiSettings` — `ApiKey`, `ModelId`, `MaxHistoryTurns`, `BackendApiKey`
- `Firebase` — `ProjectId`, `ServiceAccountKey`
- `CorsOrigins` — allowed frontend origins

For Docker: copy `.env.example` → `.env`. For local dev: edit `appsettings.Development.json`.

---

## Deployment

- **VPS**: 54860.vpsvinahost.vn — Docker container behind Nginx reverse proxy
- **DB**: Aiven cloud PostgreSQL
- **Media**: AWS S3 bucket `nunchaku-club-media`
- See `DEPLOYMENT.md` and `QUICK_START.md` for full steps
