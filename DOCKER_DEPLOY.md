# 🚀 Quick Start - Deploy Backend API

## Điều Kiện Tiên Quyết

Trên VPS của bạn cần đã có:
- ✅ **PostgreSQL** đang chạy (port 5432)
- ✅ **Database** `cnk_hadong` đã được tạo
- ✅ **Docker** và **Docker Compose** đã cài đặt
- ✅ File `appsettings.json` đã cấu hình connection string đúng

## 3 Bước Deploy Nhanh

### 1️⃣ Kiểm tra appsettings.json

```bash
nano src/NunchakuClub.API/appsettings.json
```

Đảm bảo connection string đúng:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=cnk_hadong;Username=postgres;Password=your_password"
  }
}
```

### 2️⃣ Build và Run

```bash
# Cấp quyền cho script
chmod +x deploy.sh

# Deploy
./deploy.sh
```

Hoặc thủ công:
```bash
docker compose up -d --build
```

### 3️⃣ Kiểm tra

```bash
# Xem status
docker compose ps

# Xem logs
docker compose logs -f api

# Test API
curl http://localhost:8080/swagger
```

## 🌐 Truy Cập

- **API**: http://YOUR_VPS_IP:8080
- **Swagger**: http://YOUR_VPS_IP:8080/swagger

## 📝 Các Lệnh Hữu Ích

```bash
# Xem logs
docker compose logs -f api

# Restart API
docker compose restart api

# Stop API
docker compose down

# Rebuild
docker compose up -d --build

# Vào container
docker compose exec api bash
```

## ❓ Troubleshooting

### API không kết nối được database

Kiểm tra PostgreSQL:
```bash
# Kiểm tra PostgreSQL đang chạy
sudo systemctl status postgresql

# Kiểm tra port
sudo netstat -tulpn | grep 5432

# Test connection
psql -h localhost -U postgres -d cnk_hadong
```

### Xem chi tiết logs lỗi

```bash
docker compose logs --tail=100 api
```

### API không start

```bash
# Xóa container cũ và rebuild
docker compose down
docker compose up -d --build

# Xem logs chi tiết
docker compose logs -f api
```

## 📖 Hướng Dẫn Chi Tiết

Xem file [DEPLOYMENT.md](./DEPLOYMENT.md) để có hướng dẫn đầy đủ về:
- Cài đặt Docker
- Cấu hình Nginx reverse proxy
- Setup SSL với Let's Encrypt
- Backup & monitoring
- Security best practices

## 🔐 Bảo Mật

Sau khi deploy, nhớ:
1. Đổi mật khẩu database mặc định
2. Thay JWT secret key trong appsettings.json
3. Cấu hình firewall (chỉ mở port 22, 80, 443)
4. Setup Nginx reverse proxy với SSL

## 📞 Hỗ Trợ

Nếu gặp vấn đề:
1. Kiểm tra logs: `docker compose logs -f api`
2. Kiểm tra PostgreSQL đang chạy
3. Kiểm tra appsettings.json đúng cấu hình
4. Xem DEPLOYMENT.md để troubleshooting chi tiết
