# API Documentation — Auth & User Management

**Base URL:** `https://api.dangcapnc.io.vn/api`  
**Content-Type:** `application/json`  
**Authentication:** Bearer Token (JWT)

---

## Mục lục

- [Cấu trúc Response chung](#cấu-trúc-response-chung)
- [RBAC — Phân quyền hệ thống](#rbac--phân-quyền-hệ-thống)
- [Auth API](#auth-api)
  - [Đăng ký tài khoản](#1-đăng-ký-tài-khoản)
  - [Đăng nhập](#2-đăng-nhập)
  - [SSO Exchange Token (Firebase)](#3-sso-exchange-token-firebase)
  - [Refresh Token](#4-refresh-token)
  - [Phân quyền người dùng](#5-phân-quyền-người-dùng-superadmin-only)
- [Users API](#users-api)
  - [Lấy danh sách người dùng](#1-lấy-danh-sách-người-dùng)
  - [Lấy thông tin cá nhân](#2-lấy-thông-tin-cá-nhân-me)
  - [Lấy thông tin theo ID](#3-lấy-thông-tin-theo-id)
  - [Cập nhật profile](#4-cập-nhật-profile)
  - [Thay đổi trạng thái tài khoản](#5-thay-đổi-trạng-thái-tài-khoản)
  - [Phân quyền qua Users route](#6-phân-quyền-qua-users-route)
  - [Xóa người dùng](#7-xóa-người-dùng)

---

## Cấu trúc Response chung

Tất cả API đều trả về wrapper `Result<T>`:

```json
{
  "isSuccess": true,
  "data": { ... },
  "error": null
}
```

| Field | Type | Mô tả |
|---|---|---|
| `isSuccess` | boolean | `true` nếu thành công |
| `data` | object / null | Dữ liệu trả về |
| `error` | string / null | Thông báo lỗi nếu `isSuccess = false` |

**Paginated Response** (cho danh sách):

```json
{
  "isSuccess": true,
  "data": {
    "items": [ ... ],
    "pageNumber": 1,
    "pageSize": 20,
    "totalCount": 45
  }
}
```

---

## RBAC — Phân quyền hệ thống

| Role | RoleID | Quyền |
|---|---|---|
| `SuperAdmin` | 1 | Toàn quyền — phân quyền, xóa user, quản lý hệ thống |
| `SubAdmin` | 2 | Võ sư — quản lý Chat Rooms, học viên, RAG Knowledge Base |
| `Student` | 3 | Học viên — chỉ đọc/ghi thông tin cá nhân của chính mình |

**JWT Claims** (payload sau khi decode):

```json
{
  "sub": "uuid-của-user",
  "nameid": "uuid-của-user",
  "email": "user@example.com",
  "unique_name": "username",
  "role": "Student",
  "jti": "uuid-jwt-id",
  "exp": 1234567890
}
```

**Authorization Policies:**

| Policy | Roles được phép |
|---|---|
| `RequireSuperAdmin` | SuperAdmin |
| `RequireAdminArea` | SuperAdmin, SubAdmin |
| `RequireStudent` | Bất kỳ user đã xác thực |

---

## Auth API

### 1. Đăng ký tài khoản

> **POST** `/api/auth/register`  
> Không yêu cầu xác thực. User mới tự động nhận role `Student`.

**Request Body:**

```json
{
  "email": "user@example.com",
  "username": "nguyenvana",
  "password": "Password@123",
  "fullName": "Nguyễn Văn A"
}
```

**Response 200:**

```json
{
  "isSuccess": true,
  "data": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "error": null
}
```

**Response 400 (email/username đã tồn tại):**

```json
{
  "isSuccess": false,
  "data": null,
  "error": "Email already exists"
}
```

---

### 2. Đăng nhập

> **POST** `/api/auth/login`  
> Không yêu cầu xác thực.

**Request Body:**

```json
{
  "email": "user@example.com",
  "password": "Password@123"
}
```

**Response 200:**

```json
{
  "isSuccess": true,
  "data": {
    "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "dGhpcyBpcyBhIHJlZnJlc2ggdG9rZW4...",
    "expiresAt": "2026-04-05T02:00:00Z",
    "user": {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "email": "user@example.com",
      "username": "nguyenvana",
      "fullName": "Nguyễn Văn A",
      "role": "Student",
      "avatarUrl": null
    }
  }
}
```

**Response 400 (sai mật khẩu / tài khoản bị khóa):**

```json
{
  "isSuccess": false,
  "data": null,
  "error": "Invalid credentials"
}
```

---

### 3. SSO Exchange Token (Firebase)

> **POST** `/api/auth/exchange-token`  
> Không yêu cầu xác thực.  
> Dùng cho flow đăng nhập Google / Email qua Firebase từ Frontend.

**Flow:**

```
Frontend                    Backend                     Firebase
   |                           |                           |
   |-- signInWithGoogle() ---->|                           |
   |<-- FirebaseIdToken -------|                           |
   |                           |                           |
   |-- POST exchange-token --->|                           |
   |   { firebaseIdToken }     |-- verifyIdToken() ------->|
   |                           |<-- { uid, email, name } --|
   |                           |                           |
   |                           |-- find/create User in DB  |
   |                           |-- setCustomUserClaims() ->|
   |                           |   { role: "Student" }     |
   |                           |                           |
   |<-- { accessToken, ... } --|                           |
```

**Request Body:**

```json
{
  "firebaseIdToken": "eyJhbGci..."
}
```

**Response 200:**

```json
{
  "isSuccess": true,
  "data": {
    "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "dGhpcyBpcyBhIHJlZnJlc2ggdG9rZW4...",
    "expiresAt": "2026-04-05T02:00:00Z",
    "user": {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "email": "user@gmail.com",
      "username": "nguyenvana",
      "fullName": "Nguyễn Văn A",
      "role": "Student",
      "avatarUrl": "https://lh3.googleusercontent.com/..."
    }
  }
}
```

**Response 400:**

```json
{
  "isSuccess": false,
  "data": null,
  "error": "Invalid or expired Firebase token"
}
```

> **Lưu ý:** Lần đăng nhập đầu tiên sẽ tự động tạo tài khoản với role `Student`. Những lần sau sẽ cập nhật `lastLoginAt` và phát JWT mới.

---

### 4. Refresh Token

> **POST** `/api/auth/refresh`  
> Không yêu cầu xác thực. Access token hết hạn sau 60 phút, dùng endpoint này để lấy token mới mà không cần đăng nhập lại.

**Request Body:**

```json
{
  "refreshToken": "dGhpcyBpcyBhIHJlZnJlc2ggdG9rZW4..."
}
```

**Response 200:**

```json
{
  "isSuccess": true,
  "data": {
    "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "refreshToken": "bmV3UmVmcmVzaFRva2Vu...",
    "expiresAt": "2026-04-05T03:00:00Z",
    "user": { ... }
  }
}
```

**Response 400:**

```json
{
  "isSuccess": false,
  "data": null,
  "error": "Invalid or expired refresh token"
}
```

> **Lưu ý:** Refresh token có thời hạn 7 ngày. Mỗi lần refresh sẽ cấp token mới (rotating refresh token).

---

### 5. Phân quyền người dùng (SuperAdmin only)

> **POST** `/api/auth/assign-role`  
> **Requires:** `RequireSuperAdmin`

**Request Body:**

```json
{
  "targetUserId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "role": "SubAdmin"
}
```

| Field | Type | Giá trị hợp lệ |
|---|---|---|
| `targetUserId` | guid | ID của user cần đổi role |
| `role` | string | `"SuperAdmin"` \| `"SubAdmin"` \| `"Student"` |

**Response 200:**

```json
{ "isSuccess": true, "data": null, "error": null }
```

**Response 403:** User không phải SuperAdmin.

---

## Users API

> **Base route:** `/api/users`  
> **Tất cả endpoint yêu cầu JWT** (trừ khi có ghi chú khác).

### 1. Lấy danh sách người dùng

> **GET** `/api/users`  
> **Requires:** `RequireAdminArea` (SuperAdmin hoặc SubAdmin)

**Query Parameters:**

| Param | Type | Mặc định | Mô tả |
|---|---|---|---|
| `pageNumber` | int | `1` | Trang hiện tại |
| `pageSize` | int | `20` | Số item mỗi trang (max 100) |
| `searchTerm` | string | — | Tìm theo email, username, hoặc fullName |
| `role` | string | — | Filter: `SuperAdmin` \| `SubAdmin` \| `Student` |
| `status` | string | — | Filter: `Active` \| `Inactive` \| `Suspended` |

**Ví dụ:** `GET /api/users?searchTerm=nguyen&role=Student&pageSize=10`

**Response 200:**

```json
{
  "isSuccess": true,
  "data": {
    "items": [
      {
        "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        "email": "user@example.com",
        "username": "nguyenvana",
        "fullName": "Nguyễn Văn A",
        "phone": "0912345678",
        "avatarUrl": "https://...",
        "role": "Student",
        "status": "Active",
        "emailVerified": true,
        "firebaseUid": "abc123xyz",
        "lastLoginAt": "2026-04-05T01:00:00Z",
        "createdAt": "2026-01-01T00:00:00Z",
        "updatedAt": "2026-04-05T01:00:00Z"
      }
    ],
    "pageNumber": 1,
    "pageSize": 20,
    "totalCount": 45
  }
}
```

---

### 2. Lấy thông tin cá nhân (Me)

> **GET** `/api/users/me`  
> **Requires:** Bất kỳ user đã xác thực

**Response 200:** Trả về `UserDetailDto` của user đang đăng nhập.

```json
{
  "isSuccess": true,
  "data": {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "email": "user@example.com",
    "username": "nguyenvana",
    "fullName": "Nguyễn Văn A",
    "phone": null,
    "avatarUrl": null,
    "role": "Student",
    "status": "Active",
    "emailVerified": true,
    "firebaseUid": "firebase-uid-here",
    "lastLoginAt": "2026-04-05T01:00:00Z",
    "createdAt": "2026-01-01T00:00:00Z",
    "updatedAt": "2026-04-05T01:00:00Z"
  }
}
```

---

### 3. Lấy thông tin theo ID

> **GET** `/api/users/{id}`  
> **Requires:** Admin lấy bất kỳ user. Student chỉ lấy được chính mình (403 nếu sai ID).

**Response 200:** Trả về `UserDetailDto`.

**Response 403:** Student cố xem user khác.

**Response 404:**

```json
{
  "isSuccess": false,
  "data": null,
  "error": "Không tìm thấy người dùng"
}
```

---

### 4. Cập nhật profile

> **PUT** `/api/users/{id}`  
> **Requires:** Admin cập nhật bất kỳ user. Student chỉ được cập nhật chính mình.

**Request Body:**

```json
{
  "fullName": "Nguyễn Văn B",
  "phone": "0987654321",
  "avatarUrl": "https://cdn.example.com/avatar.jpg"
}
```

| Field | Type | Bắt buộc | Mô tả |
|---|---|---|---|
| `fullName` | string | Có | Họ và tên |
| `phone` | string / null | Không | Số điện thoại |
| `avatarUrl` | string / null | Không | URL ảnh đại diện. Truyền `null` để không thay đổi |

**Response 200:**

```json
{
  "isSuccess": true,
  "data": {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "email": "user@example.com",
    "username": "nguyenvana",
    "fullName": "Nguyễn Văn B",
    "phone": "0987654321",
    "avatarUrl": "https://cdn.example.com/avatar.jpg",
    "role": "Student",
    "status": "Active",
    "emailVerified": true,
    "lastLoginAt": "2026-04-05T01:00:00Z",
    "createdAt": "2026-01-01T00:00:00Z"
  }
}
```

---

### 5. Thay đổi trạng thái tài khoản

> **PATCH** `/api/users/{id}/status`  
> **Requires:** `RequireAdminArea`

**Request Body:**

```json
{
  "status": "Suspended"
}
```

| Giá trị | Mô tả |
|---|---|
| `Active` | Tài khoản hoạt động bình thường |
| `Inactive` | Tạm khóa — không thể đăng nhập |
| `Suspended` | Đình chỉ — vi phạm nội quy |

**Response 200:**

```json
{ "isSuccess": true, "data": null, "error": null }
```

**Response 400:**

```json
{
  "isSuccess": false,
  "data": null,
  "error": "Status không hợp lệ: 'Banned'. Giá trị hợp lệ: Active, Inactive, Suspended"
}
```

---

### 6. Phân quyền qua Users route

> **POST** `/api/users/{id}/assign-role`  
> **Requires:** `RequireSuperAdmin`

**Request Body:**

```json
{
  "role": "SubAdmin"
}
```

| Giá trị | Mô tả |
|---|---|
| `SuperAdmin` | Nâng lên Super Admin |
| `SubAdmin` | Nâng lên Võ sư / Sub Admin |
| `Student` | Hạ về Học viên |

**Response 200:**

```json
{ "isSuccess": true, "data": null, "error": null }
```

> **Lưu ý:** Sau khi đổi role, Firebase custom claim `role` sẽ được cập nhật tự động. Frontend cần force-refresh Firebase ID Token để nhận claim mới:
>
> ```ts
> await auth.currentUser?.getIdToken(/* forceRefresh */ true);
> ```

---

### 7. Xóa người dùng

> **DELETE** `/api/users/{id}`  
> **Requires:** `RequireSuperAdmin`

**Ràng buộc:**
- Không thể xóa chính tài khoản đang đăng nhập
- Không thể xóa tài khoản SuperAdmin

**Response 204:** Xóa thành công (No Content).

**Response 400:**

```json
{
  "isSuccess": false,
  "data": null,
  "error": "Không thể xóa chính tài khoản đang đăng nhập"
}
```

---

## Tổng hợp Endpoints

| Method | Endpoint | Policy | Mô tả |
|---|---|---|---|
| POST | `/api/auth/register` | Public | Đăng ký tài khoản |
| POST | `/api/auth/login` | Public | Đăng nhập bằng email/password |
| POST | `/api/auth/exchange-token` | Public | SSO — đổi Firebase Token lấy JWT |
| POST | `/api/auth/refresh` | Public | Làm mới Access Token |
| POST | `/api/auth/assign-role` | SuperAdmin | Phân quyền (route cũ) |
| GET | `/api/users` | AdminArea | Danh sách người dùng + filter |
| GET | `/api/users/me` | Authenticated | Thông tin cá nhân |
| GET | `/api/users/{id}` | Authenticated* | Chi tiết user (Student: chỉ chính mình) |
| PUT | `/api/users/{id}` | Authenticated* | Cập nhật profile (Student: chỉ chính mình) |
| PATCH | `/api/users/{id}/status` | AdminArea | Kích hoạt / Khóa tài khoản |
| POST | `/api/users/{id}/assign-role` | SuperAdmin | Phân quyền |
| DELETE | `/api/users/{id}` | SuperAdmin | Xóa người dùng |

---

## Hướng dẫn Frontend

### Cách đính kèm JWT vào mỗi request

```ts
// app/lib/api/client.ts
apiClient.interceptors.request.use((config) => {
  const token = localStorage.getItem("accessToken");
  if (token) config.headers.Authorization = `Bearer ${token}`;
  return config;
});
```

### Flow đăng nhập Google (SSO)

```ts
import { useAuth } from "@/app/hooks/useAuth";

const { loginWithGoogle, user, error } = useAuth();

// 1. Người dùng bấm "Đăng nhập với Google"
await loginWithGoogle();

// 2. Hook tự động:
//    - Gọi Firebase signInWithPopup()
//    - Lấy FirebaseIdToken
//    - POST /api/auth/exchange-token
//    - Lưu accessToken + refreshToken vào localStorage
//    - Cập nhật state user

// 3. Kiểm tra role
const { isSuperAdmin, isAdminArea } = useAuth();
```

### Kiểm tra quyền trước khi render

```tsx
export default function AdminPage() {
  const { isAdminArea, isLoading } = useAuth();

  if (isLoading) return <Spinner />;
  if (!isAdminArea) return <Forbidden />;

  return <AdminDashboard />;
}
```

### Lấy danh sách học viên (SubAdmin)

```ts
import { apiClient } from "@/app/lib/api/client";

const res = await apiClient.get("/users", {
  params: {
    role: "Student",
    status: "Active",
    pageSize: 50
  }
});

const { items, totalCount } = res.data.data;
```

### Phân quyền học viên thành Võ sư (SuperAdmin)

```ts
await apiClient.post(`/users/${userId}/assign-role`, {
  role: "SubAdmin"
});
```

---

## HTTP Status Codes

| Code | Ý nghĩa |
|---|---|
| 200 | Thành công |
| 204 | Thành công, không có nội dung trả về (DELETE) |
| 400 | Dữ liệu không hợp lệ hoặc lỗi nghiệp vụ |
| 401 | Chưa xác thực — thiếu hoặc JWT hết hạn |
| 403 | Không đủ quyền |
| 404 | Không tìm thấy tài nguyên |
