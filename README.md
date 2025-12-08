# 🥋 Nunchaku Club - .NET 8 Backend API

Complete production-ready backend for Câu lạc bộ Côn Nhị Khúc Hà Đông.

## 🚀 Quick Start

```bash
# Restore packages
dotnet restore

# Update connection string in appsettings.json

# Run migrations
cd src/NunchakuClub.Infrastructure
dotnet ef database update --startup-project ../NunchakuClub.API

# Run application
cd ../NunchakuClub.API
dotnet run
```

Access Swagger UI at: https://localhost:7001/swagger

## 📚 Documentation

See `/docs` folder for complete documentation.

## 🔐 Default Admin

- Email: admin@connhikhuchadong.vn
- Password: Admin@123 (change after first login)