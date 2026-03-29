# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

.NET 8 backend API for "Câu lạc bộ Côn Nhị Khúc Hà Đông" (Nunchaku Club), built with Clean Architecture and CQRS using MediatR.

## Common Commands

```bash
# Restore dependencies
dotnet restore

# Build
dotnet build

# Run (development)
dotnet run --project src/NunchakuClub.API

# Run EF Core migrations
dotnet ef database update --project src/NunchakuClub.Infrastructure --startup-project src/NunchakuClub.API

# Add a new migration
dotnet ef migrations add <MigrationName> --project src/NunchakuClub.Infrastructure --startup-project src/NunchakuClub.API

# Docker
docker compose up -d
docker compose down
docker compose logs -f
```

Swagger UI is available at `https://localhost:52040/swagger` when running locally.

## Architecture

Clean Architecture with 4 layers:

| Layer | Project | Responsibility |
|---|---|---|
| Domain | `NunchakuClub.Domain` | Entities, enums, base classes — no external dependencies |
| Application | `NunchakuClub.Application` | CQRS handlers (MediatR), DTOs, interfaces, validators (FluentValidation) |
| Infrastructure | `NunchakuClub.Infrastructure` | EF Core (PostgreSQL), JWT, BCrypt, AWS S3, Redis |
| API | `NunchakuClub.API` | Controllers, middleware, DI registration, `Program.cs` |

### CQRS Pattern

All business logic lives in `src/NunchakuClub.Application/Features/` organized by domain. Each feature folder contains Commands and Queries handled by MediatR. Controllers are thin — they dispatch to MediatR and return HTTP results.

### API Response Convention

All endpoints return a `Result<T>` wrapper:
```json
{ "isSuccess": true, "data": { ... }, "error": null }
```

### Authentication

JWT Bearer tokens (HS256). Role-based authorization with roles: `Admin`, `Editor`, `Coach`, `Member`. Use `[Authorize(Roles = "Admin,Editor")]` on protected endpoints.

### Database

PostgreSQL via EF Core with Npgsql. The `ApplicationDbContext` is in `src/NunchakuClub.Infrastructure/Data/Contexts/`. Entity configurations are in `Data/Configurations/`. Auto-migration runs on startup in the Development environment.

### Key Interfaces (Application Layer)

Defined in `src/NunchakuClub.Application/Common/Interfaces/`:
- `IApplicationDbContext` — database access
- `IJwtTokenGenerator` — token creation
- `IPasswordHasher` — BCrypt wrapper
- `ICloudStorageService` — AWS S3 uploads
- `ICacheService` — Redis caching

### Adding a New Feature

1. Add entity to `NunchakuClub.Domain/Entities/`
2. Add `DbSet` to `ApplicationDbContext` and entity configuration
3. Create migration
4. Add Command/Query + Handler in `NunchakuClub.Application/Features/<FeatureName>/`
5. Add controller endpoint in `NunchakuClub.API/Controllers/`

## Configuration

Copy `.env.example` to `.env` for Docker deployments. For local development, update `src/NunchakuClub.API/appsettings.json` directly. Key sections: `ConnectionStrings`, `JwtSettings`, `AwsS3`, `CorsOrigins`.

## Deployment

Production runs as a Docker container behind Nginx reverse proxy on VPS (54860.vpsvinahost.vn). The database is hosted on Aiven (cloud PostgreSQL). Media files go to AWS S3 bucket `nunchaku-club-media`. See `DEPLOYMENT.md` and `QUICK_START.md` for deployment steps.
