using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using NunchakuClub.Infrastructure.Data.Contexts;
using NunchakuClub.Application.Common.Interfaces;
using NunchakuClub.Infrastructure.Services.Authentication;
using NunchakuClub.Infrastructure.Services.AI;
using NunchakuClub.Infrastructure.Services.CloudStorage;
using NunchakuClub.Infrastructure.Services.Firebase;
using NunchakuClub.Infrastructure.Services.AI;
using System.Linq;
using NunchakuClub.Infrastructure.Services.Caching;
using Amazon.S3;
using Amazon;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Npgsql;
using Pgvector;
using Pgvector.EntityFrameworkCore;
using Serilog;
using System.Text;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Swagger
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "Nunchaku Club API", 
        Version = "v1",
        Description = "API for Nunchaku Club CMS"
    });
    
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference 
                { 
                    Type = ReferenceType.SecurityScheme, 
                    Id = "Bearer" 
                }
            },
            Array.Empty<string>()
        }
    });
});

// Database — use NpgsqlDataSource so pgvector type mapper is registered globally
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
dataSourceBuilder.UseVector();
var dataSource = dataSourceBuilder.Build();

builder.Services.AddSingleton(dataSource);
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(dataSource, o => o.UseVector())
           .UseSnakeCaseNamingConvention());

builder.Services.AddScoped<IApplicationDbContext>(provider => 
    provider.GetRequiredService<ApplicationDbContext>());

// JWT Settings
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
builder.Services.Configure<JwtSettings>(jwtSettings);

// Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var secretKey = jwtSettings["SecretKey"];
        if (string.IsNullOrEmpty(secretKey))
            throw new InvalidOperationException("JWT SecretKey is not configured");
            
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            ValidateIssuer = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidateAudience = true,
            ValidAudience = jwtSettings["Audience"],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// Application Services
builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();

// Cloud Storage
builder.Services.Configure<AwsS3Settings>(builder.Configuration.GetSection("AwsS3"));
var awsSection = builder.Configuration.GetSection("AwsS3");
builder.Services.AddSingleton<IAmazonS3>(_ =>
{
    var accessKey = awsSection["AccessKey"] ?? string.Empty;
    var secretKey = awsSection["SecretKey"] ?? string.Empty;
    var region = awsSection["Region"] ?? "ap-southeast-1";
    return new AmazonS3Client(accessKey, secretKey, RegionEndpoint.GetBySystemName(region));
});
builder.Services.AddScoped<ICloudStorageService, AwsS3StorageService>();

// Cache
builder.Services.AddMemoryCache();
builder.Services.AddScoped<ICacheService, MemoryCacheService>();

// AI / RAG Services
builder.Services.Configure<GeminiSettings>(builder.Configuration.GetSection("GeminiSettings"));
builder.Services.AddHttpClient("embedding", c =>
{
    c.Timeout = TimeSpan.FromSeconds(30);
});
builder.Services.AddScoped<IEmbeddingService, GoogleEmbeddingService>();
builder.Services.AddScoped<IKnowledgeBaseService, KnowledgeBaseService>();
builder.Services.AddScoped<IAiChatService, GeminiChatService>();

// Firebase — khởi tạo FirebaseApp singleton (phải trước khi đăng ký services)
// Skip nếu service account không tồn tại (EF design-time, CI không cần Firebase)
var firebaseSection = builder.Configuration.GetSection("Firebase");
builder.Services.Configure<FirebaseSettings>(firebaseSection);
builder.Services.AddHttpClient("firebase-presence", c =>
{
    c.Timeout = TimeSpan.FromSeconds(10);
});

var firebaseServiceAccountPath = firebaseSection["ServiceAccountPath"] ?? string.Empty;
var firebaseEnabled = File.Exists(firebaseServiceAccountPath);

if (firebaseEnabled && FirebaseApp.DefaultInstance is null)
{
#pragma warning disable CS0618 // GoogleCredential.FromFile vẫn hoạt động — deprecated chỉ advisory
    FirebaseApp.Create(new AppOptions
    {
        Credential = GoogleCredential.FromFile(firebaseServiceAccountPath),
        ProjectId = firebaseSection["ProjectId"]
    });
#pragma warning restore CS0618
    Log.Information("Firebase initialized from {Path}", firebaseServiceAccountPath);
}
else if (!firebaseEnabled)
{
    Log.Warning("Firebase service account not found at '{Path}' — Firebase features disabled", firebaseServiceAccountPath);
}

builder.Services.AddSingleton<IFirebasePresenceService, FirebasePresenceService>();
builder.Services.AddSingleton<IFcmNotificationService, FcmNotificationService>();
builder.Services.AddSingleton<IFirebaseChatService, FirebaseChatService>();

// Fallback Classifier — Scoped vì dùng IKnowledgeBaseService (Scoped)
builder.Services.AddScoped<IFallbackClassifierService, FallbackClassifierService>();

// MediatR - Register from Application assembly
var applicationAssembly = Assembly.Load("NunchakuClub.Application");
builder.Services.AddMediatR(cfg => 
    cfg.RegisterServicesFromAssembly(applicationAssembly));

// CORS
var corsOrigins = builder.Configuration["CorsOrigins"]?.Split(";") 
    ?? new[] { "http://localhost:3000" };
    
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(corsOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// Build app
var app = builder.Build();

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Nunchaku Club API v1");
    });
}

app.UseSerilogRequestLogging();
app.UseHttpsRedirection();
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Run migrations on startup (development only)
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    try
    {
        var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
        if (pendingMigrations.Any())
        {
            await context.Database.MigrateAsync();
            Log.Information("Database migration completed successfully");
        }
        else
        {
            Log.Information("Database is up to date, no migrations needed");
        }
    }
    catch (Npgsql.PostgresException pgEx) when (pgEx.SqlState == "42P07")
    {
        Log.Warning("Tables already exist — marking migrations as applied");
        var conn = context.Database.GetDbConnection();
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
            VALUES ('20251208091005_InitialCreate', '8.0.0')
            ON CONFLICT DO NOTHING;
            """;
        await cmd.ExecuteNonQueryAsync();
        await conn.CloseAsync();
    }
    catch (Exception ex)
    {
        Log.Error(ex, "An error occurred while migrating the database");
    }
}

// Seed knowledge base (embed CLB documents into pgvector)
// Chỉ chạy khi table rỗng — idempotent, an toàn để chạy mỗi startup
using (var seedScope = app.Services.CreateScope())
{
    var kb = seedScope.ServiceProvider.GetRequiredService<IKnowledgeBaseService>();
    try
    {
        await kb.SeedDefaultAsync();
        Log.Information("Knowledge base seed check completed");
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Knowledge base seed failed — chat will fall back to keyword search");
    }
}

Log.Information("Starting Nunchaku Club API...");
app.Run();
