# Hướng Dẫn Deploy Backend lên VPS

## Yêu Cầu Hệ Thống

- VPS với Ubuntu 20.04+ hoặc Debian 11+
- Docker 20.10+
- Docker Compose 2.0+
- Ít nhất 2GB RAM
- Ít nhất 20GB dung lượng đĩa

## Bước 1: Cài Đặt Docker trên VPS

Nếu chưa cài Docker, chạy các lệnh sau:

```bash
# Update package index
sudo apt update

# Install required packages
sudo apt install -y apt-transport-https ca-certificates curl software-properties-common

# Add Docker GPG key
curl -fsSL https://download.docker.com/linux/ubuntu/gpg | sudo gpg --dearmor -o /usr/share/keyrings/docker-archive-keyring.gpg

# Add Docker repository
echo "deb [arch=$(dpkg --print-architecture) signed-by=/usr/share/keyrings/docker-archive-keyring.gpg] https://download.docker.com/linux/ubuntu $(lsb_release -cs) stable" | sudo tee /etc/apt/sources.list.d/docker.list > /dev/null

# Install Docker
sudo apt update
sudo apt install -y docker-ce docker-ce-cli containerd.io docker-compose-plugin

# Add user to docker group
sudo usermod -aG docker $USER

# Logout and login again to apply group changes
```

## Bước 2: Cấu Hình Environment Variables

1. Copy file .env.example thành .env:
```bash
cp .env.example .env
```

2. Chỉnh sửa file .env với thông tin thực của bạn:
```bash
nano .env
```

**Quan trọng:** Thay đổi các giá trị sau:

- `DB_PASSWORD`: Mật khẩu PostgreSQL (sử dụng mật khẩu mạnh)
- `REDIS_PASSWORD`: Mật khẩu Redis (sử dụng mật khẩu mạnh)
- `JWT_SECRET_KEY`: Secret key cho JWT (ít nhất 32 ký tự, ngẫu nhiên)
- `AWS_ACCESS_KEY`: AWS Access Key của bạn
- `AWS_SECRET_KEY`: AWS Secret Key của bạn
- `AWS_S3_BUCKET_NAME`: Tên S3 bucket của bạn
- `CORS_ORIGINS`: Domain frontend của bạn (ví dụ: https://connhikhuchadong.vn)
- `API_PORT`: Port cho API (mặc định: 8080)

### Tạo JWT Secret Key Ngẫu Nhiên

```bash
# Tạo JWT secret key 64 ký tự ngẫu nhiên
openssl rand -base64 64 | tr -d '\n'
```

## Bước 3: Deploy Application

### Cách 1: Sử dụng Script Tự Động (Khuyến Nghị)

```bash
# Cấp quyền thực thi cho script
chmod +x deploy.sh

# Chạy deployment
./deploy.sh
```

### Cách 2: Deploy Thủ Công

```bash
# Build và start containers
docker compose up -d --build

# Xem logs
docker compose logs -f api
```

## Bước 4: Kiểm Tra Deployment

1. **Kiểm tra containers đang chạy:**
```bash
docker compose ps
```

Tất cả containers phải có status là `Up` hoặc `Up (healthy)`.

2. **Kiểm tra logs:**
```bash
# Xem logs của API
docker compose logs -f api

# Xem logs của PostgreSQL
docker compose logs -f postgres

# Xem logs của Redis
docker compose logs -f redis
```

3. **Test API:**
```bash
# Health check
curl http://localhost:8080/health

# Swagger UI
# Mở trình duyệt: http://YOUR_VPS_IP:8080/swagger
```

## Bước 5: Cấu Hình Nginx Reverse Proxy (Khuyến Nghị)

Để sử dụng domain và HTTPS, cài Nginx làm reverse proxy:

### 5.1. Cài Đặt Nginx

```bash
sudo apt update
sudo apt install -y nginx
```

### 5.2. Tạo Nginx Configuration

```bash
sudo nano /etc/nginx/sites-available/cnk-api
```

Thêm nội dung:

```nginx
server {
    listen 80;
    server_name api.yourdomain.com;  # Thay bằng domain của bạn

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

        # Tăng timeout cho uploads
        proxy_read_timeout 300;
        proxy_connect_timeout 300;
        proxy_send_timeout 300;
    }

    # Tăng kích thước upload
    client_max_body_size 100M;
}
```

### 5.3. Enable Site

```bash
sudo ln -s /etc/nginx/sites-available/cnk-api /etc/nginx/sites-enabled/
sudo nginx -t
sudo systemctl restart nginx
```

### 5.4. Cài Đặt SSL với Let's Encrypt

```bash
# Cài Certbot
sudo apt install -y certbot python3-certbot-nginx

# Tạo SSL certificate
sudo certbot --nginx -d api.yourdomain.com

# Auto-renewal
sudo systemctl enable certbot.timer
```

## Các Lệnh Quản Lý Hữu Ích

### Xem Logs
```bash
# Tất cả services
docker compose logs -f

# Chỉ API
docker compose logs -f api

# 100 dòng cuối
docker compose logs --tail=100 api
```

### Quản Lý Containers
```bash
# Stop tất cả
docker compose down

# Start lại
docker compose up -d

# Restart một service
docker compose restart api

# Rebuild và restart
docker compose up -d --build
```

### Database Management
```bash
# Kết nối vào PostgreSQL
docker compose exec postgres psql -U postgres -d cnk_hadong

# Backup database
docker compose exec postgres pg_dump -U postgres cnk_hadong > backup.sql

# Restore database
docker compose exec -T postgres psql -U postgres cnk_hadong < backup.sql
```

### Redis Management
```bash
# Kết nối vào Redis
docker compose exec redis redis-cli -a your_redis_password

# Clear cache
docker compose exec redis redis-cli -a your_redis_password FLUSHALL
```

### Monitoring
```bash
# Resource usage
docker stats

# Disk usage
docker system df
```

## Troubleshooting

### API không start được

1. Kiểm tra logs:
```bash
docker compose logs api
```

2. Kiểm tra PostgreSQL có chạy không:
```bash
docker compose ps postgres
```

3. Kiểm tra kết nối database:
```bash
docker compose exec postgres psql -U postgres -d cnk_hadong -c "SELECT 1;"
```

### Database migration lỗi

```bash
# Xóa database và tạo lại
docker compose down -v
docker compose up -d
```

### Port bị chiếm

```bash
# Kiểm tra port đang được sử dụng
sudo lsof -i :8080

# Thay đổi API_PORT trong .env
```

### Out of Memory

```bash
# Tăng swap
sudo fallocate -l 2G /swapfile
sudo chmod 600 /swapfile
sudo mkswap /swapfile
sudo swapon /swapfile
echo '/swapfile none swap sw 0 0' | sudo tee -a /etc/fstab
```

## Bảo Mật

1. **Firewall:**
```bash
# Chỉ mở port cần thiết
sudo ufw allow 22/tcp   # SSH
sudo ufw allow 80/tcp   # HTTP
sudo ufw allow 443/tcp  # HTTPS
sudo ufw enable
```

2. **Thay đổi mật khẩu mặc định** trong .env

3. **Backup định kỳ:**
```bash
# Tạo cron job backup database hàng ngày
crontab -e

# Thêm dòng:
0 2 * * * cd /path/to/project && docker compose exec -T postgres pg_dump -U postgres cnk_hadong > backups/backup_$(date +\%Y\%m\%d).sql
```

4. **Update thường xuyên:**
```bash
docker compose pull
docker compose up -d
```

## Auto-Start on Boot

```bash
# Enable Docker auto-start
sudo systemctl enable docker

# Containers sẽ tự động start vì có `restart: unless-stopped`
```

## Cấu Hình Production

Đảm bảo các cấu hình sau trong `appsettings.Production.json`:

- Logging level: `Information` hoặc `Warning`
- CORS chỉ cho phép domain chính thức
- JWT secret key mạnh
- Database credentials an toàn

## Contact & Support

Nếu gặp vấn đề, kiểm tra:
1. Logs của containers
2. Firewall settings
3. Docker network connectivity
4. Database connections

---

**Lưu ý:** Đây là production setup. Hãy đảm bảo backup dữ liệu thường xuyên và theo dõi logs để phát hiện vấn đề sớm.
