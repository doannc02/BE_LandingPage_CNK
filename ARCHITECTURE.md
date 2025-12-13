# 🏗️ Kiến Trúc Hệ Thống

## Setup Hiện Tại của Bạn

```
┌─────────────────────────────────────────────────┐
│           Frontend (Vercel)                      │
│      https://yourdomain.vercel.app              │
│                  (HTTPS ✅)                      │
└────────────────┬────────────────────────────────┘
                 │
                 │ API Calls
                 ▼
┌─────────────────────────────────────────────────┐
│              VPS Server                          │
│                                                  │
│  ┌──────────────────────────────────────────┐  │
│  │   Nginx Reverse Proxy (Port 80/443)      │  │
│  │   https://api.yourdomain.com             │  │
│  │   - SSL/TLS (Let's Encrypt)              │  │
│  │   - CORS Handling                        │  │
│  │   - Rate Limiting                        │  │
│  └──────────────┬───────────────────────────┘  │
│                 │                                │
│                 │ proxy_pass                     │
│                 ▼                                │
│  ┌──────────────────────────────────────────┐  │
│  │   Docker Container (Port 8080)           │  │
│  │   - .NET 8 Backend API                   │  │
│  │   - network_mode: host                   │  │
│  └──────────────┬───────────────────────────┘  │
│                 │                                │
│                 │ localhost connection           │
│                 ▼                                │
│  ┌──────────────────────────────────────────┐  │
│  │   PostgreSQL (localhost:5432)            │  │
│  │   Database: cnk_hadong                   │  │
│  └──────────────────────────────────────────┘  │
│                                                  │
│  ┌──────────────────────────────────────────┐  │
│  │   Redis (localhost:6379)                 │  │
│  │   Cache                                  │  │
│  └──────────────────────────────────────────┘  │
└──────────────────────────────────────────────────┘

         │
         │ S3 API Calls
         ▼
┌──────────────────────┐
│    AWS S3            │
│    Media Storage     │
└──────────────────────┘
```

## 🔄 Flow Hoạt Động

### 1. Request Flow
```
User Browser
  → Frontend (Vercel HTTPS)
    → API (VPS HTTPS via Nginx)
      → Docker Container
        → PostgreSQL/Redis (localhost)
```

### 2. Response Flow
```
PostgreSQL → Docker Container
  → Nginx
    → Frontend
      → User Browser
```

## ⚙️ Chi Tiết Components

### Frontend (Vercel)
- **URL**: `https://yourdomain.vercel.app`
- **Protocol**: HTTPS (bắt buộc)
- **Deployment**: Tự động từ Git
- **CDN**: Cloudflare/Vercel Edge Network

### VPS Server
- **OS**: Ubuntu 20.04+
- **Services**:
  - Nginx (reverse proxy)
  - Docker (containerization)
  - PostgreSQL (database)
  - Redis (cache)

### Nginx Layer
- **Port**: 80 (HTTP) → Redirect 301 → 443 (HTTPS)
- **SSL**: Let's Encrypt (auto-renewal)
- **Features**:
  - CORS headers
  - Rate limiting
  - Security headers
  - Compression
  - Static file serving (nếu cần)

### Docker Container
- **Image**: .NET 8 Runtime
- **Network**: Host mode (truy cập localhost VPS)
- **Port**: 8080 (internal)
- **Volumes**:
  - `/app/Logs` → Logs persistence
  - `/app/appsettings.json` → Configuration

### Database Layer
- **PostgreSQL**: Main database
- **Redis**: Cache & session storage
- **Connection**: localhost (vì Docker dùng host network)

## 🔐 Security Layers

```
Internet
  ↓
Cloudflare/DNS (DDoS protection)
  ↓
Firewall (UFW) - Only 80, 443, 22
  ↓
Nginx (Rate limiting, SSL)
  ↓
Docker Container (Isolated environment)
  ↓
Application (JWT Auth, Role-based)
  ↓
Database (PostgreSQL auth)
```

## 📡 Network Ports

| Service | Port | Access | Protocol |
|---------|------|--------|----------|
| Nginx | 80 | Public | HTTP (→ 443) |
| Nginx | 443 | Public | HTTPS ✅ |
| API (Docker) | 8080 | localhost only | HTTP |
| PostgreSQL | 5432 | localhost only | PostgreSQL |
| Redis | 6379 | localhost only | Redis |
| SSH | 22 | Admin only | SSH |

## 🌍 Domain Setup

### Option 1: Subdomain (Khuyến nghị)
```
Frontend: https://yourdomain.com (Vercel)
Backend:  https://api.yourdomain.com (VPS)
```

**DNS Records:**
```
yourdomain.com        → CNAME → vercel-alias.vercel-dns.com
api.yourdomain.com    → A     → VPS_IP_ADDRESS
```

### Option 2: Khác Domain
```
Frontend: https://myapp.vercel.app
Backend:  https://api-myapp.com
```

**DNS Records:**
```
api-myapp.com → A → VPS_IP_ADDRESS
```

## 🔄 Deployment Flow

```bash
# 1. Code changes
git push origin main

# 2. Frontend auto-deploy (Vercel)
Vercel detects push → Build → Deploy → HTTPS

# 3. Backend manual deploy (VPS)
ssh vps
cd /path/to/project
git pull
docker compose up -d --build
```

## 📊 Monitoring Points

### 1. Frontend (Vercel Dashboard)
- Build status
- Deployment logs
- Analytics
- Performance metrics

### 2. Backend (VPS)
```bash
# Nginx logs
sudo tail -f /var/log/nginx/access.log
sudo tail -f /var/log/nginx/error.log

# API logs
docker compose logs -f api

# Database logs
sudo journalctl -u postgresql -f

# System resources
htop
docker stats
```

### 3. SSL Certificate
```bash
# Check expiry
sudo certbot certificates

# Auto-renewal test
sudo certbot renew --dry-run
```

## 🚀 Performance Optimization

### Nginx Caching
```nginx
# Cache static assets
location ~* \.(jpg|jpeg|png|gif|ico|css|js)$ {
    expires 1y;
    add_header Cache-Control "public, immutable";
}
```

### Compression
```nginx
gzip on;
gzip_types text/plain text/css application/json application/javascript;
gzip_min_length 1000;
```

### API Response Cache (Redis)
- Implement Redis cache trong application
- Cache GET requests
- Invalidate on PUT/POST/DELETE

## 🔧 Environment Variables

### VPS Environment
```bash
# PostgreSQL
DB_NAME=cnk_hadong
DB_USER=postgres
DB_PASSWORD=your_secure_password

# Nginx
DOMAIN=api.yourdomain.com
```

### Application (appsettings.json)
```json
{
  "ConnectionStrings": {...},
  "JwtSettings": {...},
  "AwsS3": {...},
  "CorsOrigins": "https://yourdomain.vercel.app"
}
```

## 📝 Checklist Triển Khai

### Backend VPS
- [ ] Docker installed
- [ ] PostgreSQL running
- [ ] Redis running (optional)
- [ ] Database created
- [ ] API deployed via Docker
- [ ] Nginx configured
- [ ] SSL certificate installed
- [ ] Firewall configured
- [ ] CORS configured

### Frontend Vercel
- [ ] Connected to Git
- [ ] Environment variables set
- [ ] API_URL configured
- [ ] HTTPS enabled
- [ ] Custom domain (optional)

### DNS
- [ ] A record for API subdomain
- [ ] DNS propagated (5-60 minutes)

### Testing
- [ ] API accessible via HTTPS
- [ ] Frontend can call API
- [ ] No CORS errors
- [ ] SSL certificate valid
- [ ] Database connections work
- [ ] File uploads work (S3)

## 🆘 Common Issues

### CORS Error
**Problem**: Browser blocks API requests
**Solution**: Check `CorsOrigins` in appsettings.json

### Mixed Content Error
**Problem**: HTTPS frontend calling HTTP backend
**Solution**: Setup SSL on backend with Nginx

### 502 Bad Gateway
**Problem**: Nginx can't reach API
**Solution**: Check `docker compose ps`, ensure API running on 8080

### Database Connection Failed
**Problem**: API can't connect to PostgreSQL
**Solution**: Verify PostgreSQL running, check connection string

## 📚 Documentation Files

- `DOCKER_DEPLOY.md` - Quick start deployment
- `NGINX_SETUP.md` - Nginx + SSL setup
- `DEPLOYMENT.md` - Full deployment guide
- `ARCHITECTURE.md` - This file (system overview)
