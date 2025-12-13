# 🚀 Quick Start - Deploy Full Stack

## Domain của bạn: **54860.vpsvinahost.vn**

## 📋 Checklist Trước Khi Deploy

- [ ] PostgreSQL đang chạy trên VPS
- [ ] Database `cnk_hadong` đã được tạo
- [ ] File `appsettings.json` đã cấu hình đúng connection string
- [ ] Docker đã được cài đặt

## 🎯 Deploy Trong 3 Bước

### Bước 1: Deploy Backend API (Docker)

```bash
# Chạy script deploy
chmod +x deploy.sh
./deploy.sh
```

✅ API sẽ chạy tại: `http://localhost:8080`

### Bước 2: Setup Nginx Reverse Proxy

```bash
# Chạy script cài Nginx
chmod +x install-nginx.sh
./install-nginx.sh
```

✅ API có thể truy cập qua: `http://54860.vpsvinahost.vn`

### Bước 3: Cài SSL Certificate (HTTPS)

```bash
# Chạy script cài SSL
chmod +x install-ssl.sh
./install-ssl.sh
```

✅ API có thể truy cập qua: `https://54860.vpsvinahost.vn` 🔒

## ✅ Hoàn Tất!

API của bạn giờ chạy tại:
- 🌐 **HTTPS URL**: https://54860.vpsvinahost.vn
- 📖 **Swagger Docs**: https://54860.vpsvinahost.vn/swagger
- 🔐 **SSL**: Let's Encrypt (Auto-renewal)

## 🔧 Cấu Hình Frontend (Vercel)

Trong frontend project, set environment variable:

```env
NEXT_PUBLIC_API_URL=https://54860.vpsvinahost.vn
```

Hoặc trong code:
```javascript
const API_URL = 'https://54860.vpsvinahost.vn';
```

## 📝 Update CORS

```bash
# Edit appsettings.json
nano src/NunchakuClub.API/appsettings.json
```

Thêm domain Vercel vào CORS:
```json
{
  "CorsOrigins": "https://your-app.vercel.app;https://54860.vpsvinahost.vn"
}
```

Restart API:
```bash
docker compose restart api
```

## 🧪 Test API

### Từ VPS (curl)
```bash
# Test HTTP
curl http://localhost:8080/swagger

# Test HTTPS
curl https://54860.vpsvinahost.vn/swagger

# Test API endpoint
curl https://54860.vpsvinahost.vn/api/posts
```

### Từ Browser
- Swagger UI: https://54860.vpsvinahost.vn/swagger
- API Endpoint: https://54860.vpsvinahost.vn/api/posts

### Từ Frontend (JavaScript)
```javascript
fetch('https://54860.vpsvinahost.vn/api/posts')
  .then(res => res.json())
  .then(data => console.log(data))
  .catch(err => console.error('Error:', err));
```

## 📊 Useful Commands

### Docker
```bash
# Xem logs
docker compose logs -f api

# Restart
docker compose restart api

# Stop
docker compose down

# Rebuild
docker compose up -d --build
```

### Nginx
```bash
# Xem logs
sudo tail -f /var/log/nginx/cnk-api-access.log
sudo tail -f /var/log/nginx/cnk-api-error.log

# Test config
sudo nginx -t

# Restart
sudo systemctl restart nginx
```

### SSL Certificate
```bash
# Check certificate
sudo certbot certificates

# Test renewal
sudo certbot renew --dry-run

# Force renewal
sudo certbot renew --force-renewal
```

### Database
```bash
# Connect to PostgreSQL
psql -U postgres -d cnk_hadong

# Backup
docker compose exec -T postgres pg_dump -U postgres cnk_hadong > backup.sql

# Restore
docker compose exec -T postgres psql -U postgres cnk_hadong < backup.sql
```

## 🔥 Firewall (Khuyến nghị)

```bash
# Cho phép SSH
sudo ufw allow 22/tcp

# Cho phép HTTP/HTTPS
sudo ufw allow 'Nginx Full'

# Chặn port 8080 từ bên ngoài
sudo ufw deny 8080/tcp

# Enable firewall
sudo ufw enable
```

## 📖 Documentation

| File | Mô tả |
|------|-------|
| `QUICK_START.md` | ⭐ Quick start guide (file này) |
| `SETUP_NGINX_VINAHOST.md` | Chi tiết setup Nginx với domain Vinahost |
| `DOCKER_DEPLOY.md` | Hướng dẫn deploy Docker |
| `DEPLOYMENT.md` | Hướng dẫn đầy đủ |
| `ARCHITECTURE.md` | Sơ đồ kiến trúc hệ thống |

## 🏗️ Architecture

```
Frontend (Vercel)
  ↓ HTTPS
Nginx (VPS) - Port 80/443 + SSL
  ↓ Proxy
Docker Container - Port 8080
  ↓ localhost
PostgreSQL + Redis (VPS)
```

## 🆘 Troubleshooting

### API không start
```bash
docker compose logs api
docker compose restart api
```

### Nginx 502 Bad Gateway
```bash
# Kiểm tra API đang chạy
docker compose ps

# Test API
curl http://localhost:8080/swagger
```

### CORS Error
```bash
# Check CORS config
cat src/NunchakuClub.API/appsettings.json | grep CorsOrigins

# Restart API
docker compose restart api
```

### SSL Error
```bash
# Check certificate
sudo certbot certificates

# Check Nginx config
sudo nginx -t

# View errors
sudo tail -f /var/log/nginx/error.log
```

## 💡 Tips

1. **Logs Location**:
   - API logs: `./Logs/`
   - Nginx logs: `/var/log/nginx/`

2. **Backup**:
   - Database: Chạy backup định kỳ
   - appsettings.json: Backup trước khi sửa

3. **Security**:
   - Đổi mật khẩu database
   - Thay JWT secret key
   - Enable firewall
   - Monitor logs thường xuyên

4. **Performance**:
   - Enable Nginx caching nếu cần
   - Optimize database queries
   - Use Redis cache

## 🎉 Done!

Setup hoàn tất! API của bạn giờ có thể:
- ✅ Truy cập qua HTTPS
- ✅ Tự động renew SSL
- ✅ CORS đã cấu hình
- ✅ Bảo mật với firewall
- ✅ Frontend Vercel gọi được API

Chúc mừng! 🚀
