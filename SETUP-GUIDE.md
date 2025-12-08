# 🚀 SETUP GUIDE

## Prerequisites
- .NET 8 SDK
- PostgreSQL 14+
- Redis (optional)
- AWS S3 or Azure Storage account

## Steps

### 1. Restore Packages
```bash
dotnet restore
```

### 2. Update Configuration
Edit `src/NunchakuClub.API/appsettings.json`:
- Update ConnectionStrings:DefaultConnection
- Update JwtSettings:SecretKey (min 32 characters)
- Update AwsS3 settings

### 3. Database Migration
```bash
cd src/NunchakuClub.Infrastructure
dotnet ef migrations add InitialCreate --startup-project ../NunchakuClub.API
dotnet ef database update --startup-project ../NunchakuClub.API
```

### 4. Run Application
```bash
cd ../NunchakuClub.API
dotnet run
```

### 5. Access Swagger
Open browser: https://localhost:7001/swagger

## Default Admin
- Email: admin@connhikhuchadong.vn  
- Password: Admin@123

**Change password after first login!**

## Build for Production
```bash
dotnet publish -c Release -o ./publish
```

## Docker
```bash
docker build -t nunchaku-club-api .
docker run -p 5000:80 nunchaku-club-api
```

## Troubleshooting

### Issue: Connection to database failed
**Solution**: Check PostgreSQL is running and connection string is correct

### Issue: JWT validation failed
**Solution**: Ensure SecretKey is at least 32 characters

### Issue: Cannot resolve MediatR
**Solution**: Run `dotnet restore` again

For more help, check README.md
