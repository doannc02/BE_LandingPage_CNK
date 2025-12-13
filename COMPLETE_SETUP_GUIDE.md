# 🎯 Complete Setup Summary

## 🌐 Your Deployment URLs

| Service | URL | Status |
|---------|-----|--------|
| **Frontend** | https://fe-landing-page-cnk.vercel.app | ✅ Deployed on Vercel |
| **Backend** | https://54860.vpsvinahost.vn | 🔄 To be deployed |
| **API Docs** | https://54860.vpsvinahost.vn/swagger | 🔄 To be deployed |

## 📋 Deployment Checklist

### Backend (VPS) - Bạn Cần Làm

- [ ] **Step 1**: Pull code mới nhất
- [ ] **Step 2**: Deploy API với Docker
- [ ] **Step 3**: Setup Nginx reverse proxy
- [ ] **Step 4**: Cài SSL certificate
- [ ] **Step 5**: Test API endpoint

### Frontend (Vercel) - Cấu Hình Sau Khi Backend Xong

- [ ] **Step 6**: Update API URL trong frontend
- [ ] **Step 7**: Test kết nối frontend → backend
- [ ] **Step 8**: Deploy frontend với config mới

---

## 🚀 Backend Deployment Steps (VPS)

### Step 1: Pull Code Mới Nhất

```bash
cd /path/to/BE_LandingPage_CNK
git pull origin claude/explain-codebase-mj2mu3pvql8gdgjk-01QMmegnEoxYAGzx8pZk9X9C
```

✅ **CORS đã được cập nhật** để cho phép:
- `https://fe-landing-page-cnk.vercel.app`
- `https://54860.vpsvinahost.vn`
- `http://localhost:3000` (dev)

### Step 2: Deploy API với Docker

```bash
# Đảm bảo PostgreSQL đang chạy
sudo systemctl status postgresql

# Deploy API
chmod +x deploy.sh
./deploy.sh
```

**Kiểm tra:**
```bash
# Xem logs
docker compose logs -f api

# Test API
curl http://localhost:8080/swagger
```

### Step 3: Setup Nginx Reverse Proxy

```bash
chmod +x install-nginx.sh
./install-nginx.sh
```

**Kiểm tra:**
```bash
# Test qua domain
curl http://54860.vpsvinahost.vn/swagger

# Xem logs
sudo tail -f /var/log/nginx/cnk-api-access.log
```

### Step 4: Cài SSL Certificate

```bash
./install-ssl.sh
```

**Bạn sẽ được hỏi:**
- Email: `your-email@example.com`
- Agree to Terms: `Y`
- Redirect HTTP to HTTPS: `2` (Yes)

**Kiểm tra:**
```bash
# Test HTTPS
curl https://54860.vpsvinahost.vn/swagger

# Xem SSL info
sudo certbot certificates
```

### Step 5: Test API Endpoints

```bash
# Test health
curl https://54860.vpsvinahost.vn/health

# Test posts endpoint
curl https://54860.vpsvinahost.vn/api/posts

# Test auth endpoint
curl -X POST https://54860.vpsvinahost.vn/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@connhikhuchadong.vn","password":"Admin@123"}'
```

---

## 🎨 Frontend Configuration (Vercel)

Sau khi backend đã chạy với HTTPS, update frontend:

### Option 1: Environment Variables (Khuyến nghị)

Trong Vercel Dashboard:
1. Vào project: https://vercel.com/dashboard
2. Settings → Environment Variables
3. Thêm biến:

```env
NEXT_PUBLIC_API_URL=https://54860.vpsvinahost.vn
```

4. Redeploy project

### Option 2: Config File

Nếu frontend dùng config file, update:

```javascript
// config.js hoặc constants.js
export const API_URL = process.env.NEXT_PUBLIC_API_URL || 'https://54860.vpsvinahost.vn';
```

```typescript
// config.ts
export const config = {
  apiUrl: process.env.NEXT_PUBLIC_API_URL || 'https://54860.vpsvinahost.vn',
  apiEndpoints: {
    auth: '/api/auth',
    posts: '/api/posts',
    courses: '/api/courses',
    contact: '/api/contact',
  }
}
```

### Option 3: Axios Instance

```javascript
// api/axios.js
import axios from 'axios';

const instance = axios.create({
  baseURL: process.env.NEXT_PUBLIC_API_URL || 'https://54860.vpsvinahost.vn',
  timeout: 10000,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Request interceptor (để add JWT token)
instance.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem('token');
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => Promise.reject(error)
);

export default instance;
```

Usage:
```javascript
import api from '@/api/axios';

// Get posts
const posts = await api.get('/api/posts');

// Login
const response = await api.post('/api/auth/login', {
  email: 'admin@connhikhuchadong.vn',
  password: 'Admin@123'
});
```

---

## 🧪 Testing Full Stack

### Test 1: API Health Check

```bash
curl https://54860.vpsvinahost.vn/health
```

Expected: Status 200 OK

### Test 2: Get Posts (Frontend)

```javascript
// In browser console on https://fe-landing-page-cnk.vercel.app
fetch('https://54860.vpsvinahost.vn/api/posts')
  .then(res => res.json())
  .then(data => console.log('Posts:', data))
  .catch(err => console.error('Error:', err));
```

Expected: Array of posts (or empty array)

### Test 3: Login (Frontend)

```javascript
fetch('https://54860.vpsvinahost.vn/api/auth/login', {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json',
  },
  body: JSON.stringify({
    email: 'admin@connhikhuchadong.vn',
    password: 'Admin@123'
  })
})
  .then(res => res.json())
  .then(data => console.log('Login success:', data))
  .catch(err => console.error('Login error:', err));
```

Expected: JWT token response

---

## 🔐 Security Checklist

### Backend (VPS)

- [ ] Đổi mật khẩu PostgreSQL mặc định
- [ ] Thay `JWT SecretKey` trong appsettings.json (ít nhất 32 ký tự)
- [ ] Setup firewall (UFW)
  ```bash
  sudo ufw allow 22/tcp
  sudo ufw allow 'Nginx Full'
  sudo ufw deny 8080/tcp
  sudo ufw enable
  ```
- [ ] SSL certificate installed (Let's Encrypt)
- [ ] CORS chỉ cho phép domain chính thức
- [ ] Disable PostgreSQL remote access (chỉ localhost)
  ```bash
  # /etc/postgresql/*/main/postgresql.conf
  listen_addresses = 'localhost'
  ```

### Frontend (Vercel)

- [ ] API URL dùng HTTPS (không HTTP)
- [ ] Không hardcode sensitive data trong code
- [ ] Environment variables được set đúng
- [ ] CORS được config đúng từ backend

---

## 📊 Monitoring & Logs

### Backend Logs

```bash
# API logs
docker compose logs -f api
docker compose logs --tail=100 api

# Nginx access logs
sudo tail -f /var/log/nginx/cnk-api-access.log

# Nginx error logs
sudo tail -f /var/log/nginx/cnk-api-error.log

# PostgreSQL logs
sudo tail -f /var/log/postgresql/postgresql-*-main.log
```

### Frontend Logs

- Vercel Dashboard: https://vercel.com/dashboard → Your Project → Logs
- Browser Console: F12 → Console tab

---

## 🆘 Common Issues & Solutions

### Issue 1: CORS Error

**Error in browser:**
```
Access to fetch at 'https://54860.vpsvinahost.vn/api/posts' from origin 'https://fe-landing-page-cnk.vercel.app' has been blocked by CORS policy
```

**Solution:**
```bash
# 1. Check CORS trong appsettings.json
cat src/NunchakuClub.API/appsettings.json | grep CorsOrigins

# Should show:
# "CorsOrigins": "http://localhost:3000;https://fe-landing-page-cnk.vercel.app;https://54860.vpsvinahost.vn"

# 2. Restart API
docker compose restart api
```

### Issue 2: SSL Certificate Error

**Error:** NET::ERR_CERT_AUTHORITY_INVALID

**Solution:**
```bash
# Re-install SSL
sudo certbot delete --cert-name 54860.vpsvinahost.vn
./install-ssl.sh
```

### Issue 3: API 502 Bad Gateway

**Solution:**
```bash
# Check API is running
docker compose ps

# Check API logs
docker compose logs api

# Restart API
docker compose restart api
```

### Issue 4: Database Connection Failed

**Solution:**
```bash
# Check PostgreSQL
sudo systemctl status postgresql

# Test connection
psql -U postgres -d cnk_hadong -c "SELECT 1;"

# Check connection string in appsettings.json
cat src/NunchakuClub.API/appsettings.json | grep ConnectionStrings
```

---

## 🎉 Success Criteria

Deployment thành công khi:

- ✅ Backend API có thể truy cập qua HTTPS: https://54860.vpsvinahost.vn
- ✅ Swagger UI hoạt động: https://54860.vpsvinahost.vn/swagger
- ✅ Frontend có thể gọi API không bị CORS error
- ✅ Login/Authentication hoạt động
- ✅ Database connection thành công
- ✅ SSL certificate valid (ổ khóa 🔒 màu xanh)
- ✅ Auto-renewal SSL đã setup

---

## 📞 Quick Reference

### Backend URLs
```
API Base URL:    https://54860.vpsvinahost.vn
Swagger Docs:    https://54860.vpsvinahost.vn/swagger
Health Check:    https://54860.vpsvinahost.vn/health
```

### Frontend URL
```
Website:         https://fe-landing-page-cnk.vercel.app
```

### Default Admin Account
```
Email:           admin@connhikhuchadong.vn
Password:        Admin@123
```

### Useful Commands
```bash
# Backend
docker compose logs -f api              # View logs
docker compose restart api              # Restart
docker compose down && docker compose up -d --build  # Rebuild

# Nginx
sudo systemctl restart nginx            # Restart
sudo nginx -t                           # Test config
sudo tail -f /var/log/nginx/cnk-api-access.log  # Logs

# SSL
sudo certbot certificates               # Check SSL
sudo certbot renew --dry-run            # Test renewal
```

---

**Ready to deploy?** Start with Step 1! 🚀
