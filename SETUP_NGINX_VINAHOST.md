# 🚀 Setup Nginx + SSL cho Domain Vinahost

Domain của bạn: **54860.vpsvinahost.vn**

## Bước 1: Cài Nginx

```bash
# Update và cài Nginx
sudo apt update
sudo apt install -y nginx

# Kiểm tra Nginx đã chạy
sudo systemctl status nginx
```

## Bước 2: Tạo Nginx Config

```bash
# Tạo file config
sudo nano /etc/nginx/sites-available/cnk-api
```

**Copy nội dung sau vào file:**

```nginx
server {
    listen 80;
    server_name 54860.vpsvinahost.vn;

    location / {
        proxy_pass http://localhost:8080;
        proxy_http_version 1.1;

        # Headers cơ bản
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection 'upgrade';
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_cache_bypass $http_upgrade;

        # Timeouts cho file upload
        proxy_read_timeout 300;
        proxy_connect_timeout 300;
        proxy_send_timeout 300;
    }

    # Max upload size
    client_max_body_size 100M;
}
```

**Lưu file:** `Ctrl + O`, Enter, `Ctrl + X`

## Bước 3: Enable Site

```bash
# Tạo symbolic link
sudo ln -s /etc/nginx/sites-available/cnk-api /etc/nginx/sites-enabled/

# Xóa default site (optional)
sudo rm /etc/nginx/sites-enabled/default

# Test cấu hình
sudo nginx -t

# Restart Nginx
sudo systemctl restart nginx
```

## Bước 4: Test HTTP

```bash
# Test từ VPS
curl http://54860.vpsvinahost.vn/swagger

# Hoặc mở browser:
# http://54860.vpsvinahost.vn/swagger
```

Nếu thấy Swagger UI → **Thành công!** ✅

## Bước 5: Cài SSL (Let's Encrypt)

```bash
# Cài Certbot
sudo apt install -y certbot python3-certbot-nginx

# Tạo SSL certificate
sudo certbot --nginx -d 54860.vpsvinahost.vn
```

**Trả lời các câu hỏi:**
```
Email: your-email@example.com
Agree to ToS: Y
Share email: N (hoặc Y)
Redirect HTTP to HTTPS: 2 (chọn Yes)
```

Certbot sẽ tự động:
- ✅ Tạo SSL certificate
- ✅ Cấu hình HTTPS trong Nginx
- ✅ Redirect HTTP → HTTPS
- ✅ Setup auto-renewal

## Bước 6: Test HTTPS

```bash
# Test từ VPS
curl https://54860.vpsvinahost.vn/swagger

# Check SSL
sudo certbot certificates
```

**Mở browser:** https://54860.vpsvinahost.vn/swagger

Nếu thấy ổ khóa 🔒 → **SSL hoạt động!** ✅

## Bước 7: Cấu hình CORS trong Backend

```bash
# Edit appsettings.json
nano src/NunchakuClub.API/appsettings.json
```

**Update CORS Origins:**
```json
{
  "CorsOrigins": "https://yourdomain.vercel.app;https://54860.vpsvinahost.vn"
}
```

**Restart API:**
```bash
docker compose restart api
```

## Bước 8: Cấu hình Firewall

```bash
# Cho phép Nginx
sudo ufw allow 'Nginx Full'

# Cho phép SSH (QUAN TRỌNG - không bị khóa SSH!)
sudo ufw allow 22/tcp

# Chặn truy cập trực tiếp port 8080 từ bên ngoài
# (chỉ cho phép localhost - Nginx)
sudo ufw deny 8080/tcp

# Enable firewall
sudo ufw enable
# Gõ 'y' để confirm

# Check status
sudo ufw status
```

## ✅ Hoàn Tất!

API của bạn giờ có thể truy cập tại:
- 🌐 **HTTPS**: https://54860.vpsvinahost.vn
- 📖 **Swagger**: https://54860.vpsvinahost.vn/swagger
- 🔐 **SSL**: ✅ Let's Encrypt

## 🧪 Test với Frontend Vercel

Trong frontend Vercel, cấu hình API URL:

```javascript
// .env hoặc config
NEXT_PUBLIC_API_URL=https://54860.vpsvinahost.vn

// Trong code
const API_URL = process.env.NEXT_PUBLIC_API_URL || 'https://54860.vpsvinahost.vn';

// Test API call
fetch(`${API_URL}/api/posts`)
  .then(res => res.json())
  .then(data => console.log(data))
  .catch(err => console.error('API Error:', err));
```

## 📊 Monitoring

### Xem Nginx Logs
```bash
# Access logs (requests)
sudo tail -f /var/log/nginx/access.log

# Error logs
sudo tail -f /var/log/nginx/error.log
```

### Xem API Logs
```bash
docker compose logs -f api
```

### Check SSL Auto-Renewal
```bash
# Test renewal (dry run)
sudo certbot renew --dry-run
```

SSL sẽ tự động renew trước khi hết hạn.

## 🆘 Troubleshooting

### Nginx không start

```bash
# Xem lỗi
sudo nginx -t
sudo systemctl status nginx

# Xem logs
sudo tail -f /var/log/nginx/error.log
```

### 502 Bad Gateway

```bash
# Kiểm tra API đang chạy
docker compose ps

# Test API trực tiếp
curl http://localhost:8080/swagger

# Xem logs
docker compose logs api
```

### CORS Error

```bash
# 1. Check appsettings.json có đúng domain Vercel
cat src/NunchakuClub.API/appsettings.json | grep CorsOrigins

# 2. Restart API
docker compose restart api

# 3. Clear browser cache và thử lại
```

### SSL Certificate Error

```bash
# Xem thông tin certificate
sudo certbot certificates

# Force renew
sudo certbot renew --force-renewal
```

## 🎯 One-Line Setup (Copy & Paste)

Nếu muốn chạy tất cả lệnh một lần:

```bash
# Deploy API + Setup Nginx + SSL (chạy từng dòng)
cd /path/to/BE_LandingPage_CNK && \
./deploy.sh && \
sudo apt update && \
sudo apt install -y nginx certbot python3-certbot-nginx && \
sudo tee /etc/nginx/sites-available/cnk-api > /dev/null <<'EOF'
server {
    listen 80;
    server_name 54860.vpsvinahost.vn;
    location / {
        proxy_pass http://localhost:8080;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection 'upgrade';
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_cache_bypass $http_upgrade;
        proxy_read_timeout 300;
        proxy_connect_timeout 300;
        proxy_send_timeout 300;
    }
    client_max_body_size 100M;
}
EOF
sudo ln -s /etc/nginx/sites-available/cnk-api /etc/nginx/sites-enabled/ && \
sudo rm -f /etc/nginx/sites-enabled/default && \
sudo nginx -t && \
sudo systemctl restart nginx && \
echo "✅ Nginx configured! Now run: sudo certbot --nginx -d 54860.vpsvinahost.vn"
```

Sau đó chạy riêng lệnh SSL:
```bash
sudo certbot --nginx -d 54860.vpsvinahost.vn
```

## 📝 Summary

| Item | Value |
|------|-------|
| Domain | https://54860.vpsvinahost.vn |
| Swagger | https://54860.vpsvinahost.vn/swagger |
| API Endpoint | https://54860.vpsvinahost.vn/api |
| SSL Provider | Let's Encrypt (Free) |
| SSL Auto-Renew | Yes |
| HTTPS Redirect | Yes |

Xong! Frontend Vercel giờ có thể gọi API qua HTTPS không bị lỗi Mixed Content. 🎉
