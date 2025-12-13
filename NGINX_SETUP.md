# 🌐 Cấu Hình Nginx Reverse Proxy + SSL

## Tại Sao Cần Nginx?

Frontend trên Vercel có HTTPS → Backend phải có HTTPS để tránh lỗi "Mixed Content"

## Bước 1: Cài Đặt Nginx

```bash
sudo apt update
sudo apt install -y nginx
```

## Bước 2: Tạo Nginx Configuration

```bash
sudo nano /etc/nginx/sites-available/cnk-api
```

**Thêm nội dung sau:**

```nginx
server {
    listen 80;
    server_name api.yourdomain.com;  # Thay bằng domain của bạn

    # Redirect HTTP to HTTPS (sau khi cài SSL)
    # return 301 https://$server_name$request_uri;

    location / {
        proxy_pass http://localhost:8080;
        proxy_http_version 1.1;

        # Headers
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection 'upgrade';
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_cache_bypass $http_upgrade;

        # CORS Headers (nếu cần)
        add_header 'Access-Control-Allow-Origin' 'https://yourdomain.vercel.app' always;
        add_header 'Access-Control-Allow-Methods' 'GET, POST, PUT, DELETE, OPTIONS' always;
        add_header 'Access-Control-Allow-Headers' 'Authorization, Content-Type' always;
        add_header 'Access-Control-Allow-Credentials' 'true' always;

        # Handle preflight requests
        if ($request_method = 'OPTIONS') {
            return 204;
        }

        # Timeouts
        proxy_read_timeout 300;
        proxy_connect_timeout 300;
        proxy_send_timeout 300;
    }

    # Max upload size
    client_max_body_size 100M;
}
```

## Bước 3: Enable Site

```bash
# Tạo symbolic link
sudo ln -s /etc/nginx/sites-available/cnk-api /etc/nginx/sites-enabled/

# Test cấu hình
sudo nginx -t

# Restart Nginx
sudo systemctl restart nginx
```

## Bước 4: Cấu Hình DNS

Trỏ subdomain về VPS:

1. Vào DNS provider (Cloudflare, GoDaddy, etc.)
2. Tạo A Record:
   - **Name**: `api` hoặc `@`
   - **Type**: A
   - **Value**: `IP_VPS_CUA_BAN`
   - **TTL**: Auto hoặc 3600

Đợi 5-10 phút để DNS propagate.

## Bước 5: Cài SSL với Let's Encrypt

```bash
# Cài Certbot
sudo apt install -y certbot python3-certbot-nginx

# Tạo SSL certificate
sudo certbot --nginx -d api.yourdomain.com

# Certbot sẽ tự động:
# - Tạo SSL certificate
# - Cấu hình HTTPS trong Nginx
# - Setup auto-renewal
```

Trả lời các câu hỏi:
- Email: `your-email@example.com`
- Agree to terms: `Y`
- Redirect HTTP to HTTPS: `2` (Yes)

## Bước 6: Kiểm Tra

```bash
# Test SSL
curl https://api.yourdomain.com/swagger

# Kiểm tra auto-renewal
sudo certbot renew --dry-run
```

## Bước 7: Cập Nhật CORS trong appsettings.json

```bash
nano src/NunchakuClub.API/appsettings.json
```

Cập nhật CORS:
```json
{
  "CorsOrigins": "https://yourdomain.vercel.app;https://www.yourdomain.com"
}
```

Restart API:
```bash
docker compose restart api
```

## 🔧 Cấu Hình Nâng Cao (Optional)

### Rate Limiting

```nginx
# Thêm vào đầu file /etc/nginx/sites-available/cnk-api
limit_req_zone $binary_remote_addr zone=api_limit:10m rate=10r/s;

server {
    # ... cấu hình khác ...

    location / {
        limit_req zone=api_limit burst=20 nodelay;
        # ... các cấu hình khác ...
    }
}
```

### Security Headers

```nginx
location / {
    # ... cấu hình khác ...

    # Security headers
    add_header X-Frame-Options "SAMEORIGIN" always;
    add_header X-Content-Type-Options "nosniff" always;
    add_header X-XSS-Protection "1; mode=block" always;
    add_header Referrer-Policy "no-referrer-when-downgrade" always;
}
```

## 🔥 Firewall Setup

```bash
# Cho phép Nginx
sudo ufw allow 'Nginx Full'

# Cho phép SSH (quan trọng!)
sudo ufw allow 22/tcp

# Đóng port 8080 (chỉ cho phép localhost access)
sudo ufw deny 8080/tcp

# Enable firewall
sudo ufw enable
```

## ✅ Checklist Hoàn Thành

- [ ] Nginx đã cài và chạy
- [ ] Tạo file config `/etc/nginx/sites-available/cnk-api`
- [ ] Enable site và restart Nginx
- [ ] DNS A record đã trỏ về VPS
- [ ] SSL certificate đã cài (Let's Encrypt)
- [ ] HTTPS hoạt động: `https://api.yourdomain.com`
- [ ] CORS đã cấu hình đúng domain Vercel
- [ ] Firewall đã setup
- [ ] Auto-renewal SSL đã test

## 🧪 Test với Frontend

```javascript
// Trong frontend Vercel
const API_URL = 'https://api.yourdomain.com';

fetch(`${API_URL}/api/posts`)
  .then(res => res.json())
  .then(data => console.log(data));
```

## 📊 Monitoring

```bash
# Xem Nginx logs
sudo tail -f /var/log/nginx/access.log
sudo tail -f /var/log/nginx/error.log

# Xem API logs
docker compose logs -f api

# Check SSL expiry
sudo certbot certificates
```

## 🆘 Troubleshooting

### CORS vẫn bị lỗi

1. Kiểm tra CORS trong `appsettings.json`
2. Restart API: `docker compose restart api`
3. Clear browser cache
4. Kiểm tra Nginx config có CORS headers

### SSL không hoạt động

```bash
# Kiểm tra Nginx config
sudo nginx -t

# Restart Nginx
sudo systemctl restart nginx

# Xem lỗi
sudo tail -f /var/log/nginx/error.log
```

### 502 Bad Gateway

```bash
# Kiểm tra API đang chạy
docker compose ps

# Kiểm tra API có response không
curl http://localhost:8080/swagger

# Xem logs
docker compose logs api
```

## 📝 Kết Luận

Sau khi setup xong:
- ✅ Frontend (Vercel): `https://yourdomain.vercel.app`
- ✅ Backend (VPS): `https://api.yourdomain.com`
- ✅ HTTPS everywhere, không lỗi Mixed Content
- ✅ CORS được cấu hình đúng
- ✅ Bảo mật với SSL, firewall, rate limiting
