# Frontend Migration Guide — Backend v2 Breaking Changes

https://localhost:52040/
Tài liệu này liệt kê tất cả thay đổi phía backend ảnh hưởng đến frontend sau đợt refactor. Đọc từ đầu đến cuối trước khi tích hợp.

---

## Tóm tắt nhanh

| Mức độ          | Số lượng | Loại                                                           |
| --------------- | -------- | -------------------------------------------------------------- |
| 🔴 Breaking     | 2        | HTTP status thay đổi, request body thay đổi                    |
| 🟡 Cần kiểm tra | 6        | Enum fields chuyển từ `int` → `string`                         |
| 🟢 Cải thiện    | 2        | `expiresAt` chính xác hơn, tìm kiếm không phân biệt hoa thường |

---

## 🔴 Breaking Changes

### 1. `POST /api/contact` — HTTP 201 thay vì 200

**Trước:**

```http
HTTP/1.1 200 OK
{ "id": "uuid", ... }
```

**Sau:**

```http
HTTP/1.1 201 Created
{ "id": "uuid", ... }
```

**Action cần làm:** Cập nhật tất cả chỗ xử lý response của `POST /api/contact`. Nếu có điều kiện kiểm tra `status === 200` thì đổi thành `status === 201`, hoặc dùng `response.ok` (2xx) thay cho so sánh cứng.

---

### 2. `POST /api/auth/register` — Request body bây giờ có 4 field bắt buộc

**Trước:** Request body binding trực tiếp vào Command (có thể gây nhầm lẫn field nào là required)

**Sau — Request body rõ ràng:**

```json
POST /api/auth/register
Content-Type: application/json

{
  "email": "user@example.com",
  "username": "nguyen_van_a",
  "password": "StrongPass@123",
  "fullName": "Nguyễn Văn A"
}
```

Tất cả 4 field đều **bắt buộc**. Nếu thiếu bất kỳ field nào sẽ nhận `400 Bad Request`.

---

## 🟡 Enum Fields: `int` → `string`

Trước đây, các cột enum trong DB được lưu dưới dạng **số nguyên**. Sau migration, tất cả được lưu dưới dạng **chuỗi**. API response đã trả về string từ trước (DTO dùng `string`), nhưng **request body** khi tạo/cập nhật nếu bạn đang gửi số cần đổi sang chuỗi.

### Bảng giá trị enum

#### `CourseLevel` — dùng trong `POST/PUT /api/courses`, field `level`

| Trước (int) | Sau (string)     | Ý nghĩa       |
| ----------- | ---------------- | ------------- |
| `1`         | `"Beginner"`     | Cơ bản        |
| `2`         | `"Intermediate"` | Trung cấp     |
| `3`         | `"Advanced"`     | Nâng cao      |
| `4`         | `"Professional"` | Chuyên nghiệp |

**Ví dụ request:**

```json
POST /api/courses
{
  "name": "Khoá Côn Cơ Bản",
  "level": "Beginner",
  ...
}
```

---

#### `EnrollmentStatus` — dùng trong `PATCH /api/course-enrollments/{id}/status`

| Trước (int) | Sau (string)  | Ý nghĩa    |
| ----------- | ------------- | ---------- |
| `1`         | `"Pending"`   | Chờ duyệt  |
| `2`         | `"Approved"`  | Đã duyệt   |
| `3`         | `"Rejected"`  | Từ chối    |
| `4`         | `"Completed"` | Hoàn thành |

**Response `GET /api/course-enrollments`:**

```json
{
  "id": "uuid",
  "courseName": "Khoá Côn Cơ Bản",
  "status": "Pending",
  ...
}
```

---

#### `ContactStatus` — dùng trong `PATCH /api/contact/{id}/status` (Admin)

| Trước (int) | Sau (string) | Ý nghĩa    |
| ----------- | ------------ | ---------- |
| `1`         | `"New"`      | Mới        |
| `2`         | `"Read"`     | Đã đọc     |
| `3`         | `"Replied"`  | Đã trả lời |
| `4`         | `"Archived"` | Đã lưu trữ |

**Response `GET /api/contact`:**

```json
{
  "id": "uuid",
  "fullName": "Nguyễn Văn A",
  "status": "New",
  ...
}
```

---

#### `CommentStatus` — dùng trong admin comment moderation

| Trước (int) | Sau (string) | Ý nghĩa   |
| ----------- | ------------ | --------- |
| `1`         | `"Pending"`  | Chờ duyệt |
| `2`         | `"Approved"` | Đã duyệt  |
| `3`         | `"Spam"`     | Spam      |
| `4`         | `"Trash"`    | Thùng rác |

> **Lưu ý:** API public `GET /api/posts/{id}/comments` chỉ trả về comments có `status = "Approved"` nên frontend public không bị ảnh hưởng. Chỉ Admin dashboard cần cập nhật.

---

#### `AchievementType` — dùng trong `POST/PUT /api/achievements`, field `type`

| Trước (int) | Sau (string)      | Ý nghĩa     |
| ----------- | ----------------- | ----------- |
| `1`         | `"Competition"`   | Giải đấu    |
| `2`         | `"Certification"` | Chứng chỉ   |
| `3`         | `"Milestone"`     | Cột mốc     |
| `4`         | `"Award"`         | Giải thưởng |

**Ví dụ request:**

```json
POST /api/achievements
{
  "title": "HCV Giải Quốc Gia 2025",
  "type": "Competition",
  "achievementDate": "2025-08-10T00:00:00Z",
  ...
}
```

---

#### `CoachTitle` — dùng trong `POST/PUT /api/coaches`, field `title`

| Trước (int) | Sau (string)       | Ý nghĩa       |
| ----------- | ------------------ | ------------- |
| `1`         | `"HeadCoach"`      | HLV trưởng    |
| `2`         | `"AssistantCoach"` | HLV phụ trách |

> ⚠️ **Quan trọng:** Field `title` trong `CoachDto` (response) hiện vẫn trả về **số nguyên** vì DTO dùng kiểu `CoachTitle` (enum). Đây là điểm cần lưu ý riêng — xem mục [Vấn đề còn tồn đọng](#vấn-đề-còn-tồn-đọng) bên dưới.

---

## 🟢 Cải thiện (không breaking)

### 1. `expiresAt` trong auth response chính xác hơn

Trước đây `expiresAt` được hardcode thành `DateTime.UtcNow + 1 giờ`. Bây giờ nó đọc từ `JwtSettings.AccessTokenExpirationMinutes` trong config (mặc định = 60 phút).

**Kết quả:** Nếu admin thay đổi thời hạn token trong config, frontend sẽ nhận được giá trị chính xác thay vì luôn là 1 giờ.

```json
{
  "accessToken": "eyJ...",
  "refreshToken": "abc...",
  "expiresAt": "2026-05-17T19:40:00Z",   // Chính xác theo config
  "user": { ... }
}
```

---

### 2. Tìm kiếm không phân biệt hoa thường

`GET /api/posts?searchTerm=nunchaku` bây giờ trả về cả kết quả chứa "Nunchaku", "NUNCHAKU", "nunchaku". Tương tự với `GET /api/users?searchTerm=...`.

Không cần thay đổi gì từ phía frontend.

---

## Endpoint Reference — Response Schema đầy đủ

### Auth Endpoints

#### `POST /api/auth/login`

**Request:**

```json
{
  "email": "user@example.com",
  "password": "password"
}
```

**Response 200:**

```json
{
  "isSuccess": true,
  "data": {
    "accessToken": "eyJ...",
    "refreshToken": "abc...",
    "expiresAt": "2026-05-17T19:40:00Z",
    "user": {
      "id": "uuid",
      "email": "user@example.com",
      "username": "nguyen_van_a",
      "fullName": "Nguyễn Văn A",
      "role": "Student",
      "avatarUrl": null
    }
  }
}
```

**`role` string values:** `"Guest"` | `"Student"` | `"SubAdmin"` | `"SuperAdmin"`

---

#### `POST /api/auth/register`

**Request:**

```json
{
  "email": "user@example.com",
  "username": "nguyen_van_a",
  "password": "StrongPass@123",
  "fullName": "Nguyễn Văn A"
}
```

**Response 200:** Cùng schema với Login response

---

#### `POST /api/auth/refresh`

**Request:**

```json
{
  "accessToken": "eyJ...",
  "refreshToken": "abc..."
}
```

**Response 200:** Cùng schema với Login response

---

#### `POST /api/auth/exchange-token` (SSO Firebase)

**Request:**

```json
{
  "firebaseIdToken": "firebase_id_token_from_google_sign_in"
}
```

**Response 200:** Cùng schema với Login response. User mới được tạo với `role = "Guest"` — Admin cần nâng lên `"Student"`.

---

### Users Endpoints

#### `GET /api/users/me` — Thông tin user hiện tại

**Response 200:**

```json
{
  "id": "uuid",
  "email": "user@example.com",
  "username": "nguyen_van_a",
  "fullName": "Nguyễn Văn A",
  "phone": null,
  "avatarUrl": null,
  "role": "Student",
  "status": "Active",
  "emailVerified": false,
  "lastLoginAt": "2026-05-17T10:00:00Z",
  "createdAt": "2026-01-01T00:00:00Z"
}
```

**`role` values:** `"Guest"` | `"Student"` | `"SubAdmin"` | `"SuperAdmin"`

**`status` values:** `"Active"` | `"Inactive"` | `"Suspended"`

---

#### `PATCH /api/users/{id}/status` — Đổi trạng thái (Admin)

**Request:**

```json
{ "status": "Suspended" }
```

**Response 200:** `{}` (empty body — không có data trả về)

---

### Posts Endpoints

#### `GET /api/posts`

**Query params:** `pageNumber`, `pageSize`, `searchTerm`, `categoryId`, `status`, `isFeatured`

**`status` query values:** `1` = Draft, `2` = Published, `3` = Archived _(vẫn nhận int query param)_

**Response 200:**

```json
{
  "items": [
    {
      "id": "uuid",
      "title": "Tiêu đề bài viết",
      "slug": "tieu-de-bai-viet",
      "excerpt": "Tóm tắt...",
      "featuredImageUrl": "https://...",
      "thumbnailUrl": "https://...",
      "status": "Published",
      "isFeatured": false,
      "publishedAt": "2026-05-01T00:00:00Z",
      "viewCount": 100,
      "likeCount": 25,
      "commentCount": 5,
      "authorName": "Nguyễn Văn A",
      "categoryName": "Tin tức",
      "createdAt": "2026-05-01T00:00:00Z"
    }
  ],
  "totalCount": 50,
  "pageNumber": 1,
  "pageSize": 10,
  "totalPages": 5
}
```

**`status` string values trong response:** `"Draft"` | `"Published"` | `"Archived"`

---

#### `POST /api/posts/{id}/comments`

**Request:**

```json
{
  "content": "Nội dung bình luận",
  "authorName": "Khách",
  "authorEmail": "guest@example.com",
  "parentId": null
}
```

**Response 200:** `"uuid"` — ID của comment vừa tạo

---

#### `POST /api/posts/{id}/like`

**Response 200:** `25` — số like mới (integer)

---

### Contact Endpoint

#### `POST /api/contact` — ⚠️ Trả về 201

**Request:**

```json
{
  "fullName": "Nguyễn Văn A",
  "phone": "0901234567",
  "email": "user@example.com",
  "courseId": "uuid-hoặc-null",
  "message": "Tôi muốn đăng ký học..."
}
```

**Response 201:**

```json
"uuid"
```

---

### Courses Endpoints

#### `GET /api/courses`

**Response — `level` field:**

```json
{
  "id": "uuid",
  "name": "Khoá Côn Cơ Bản",
  "slug": "khoa-con-co-ban",
  "level": "Beginner",
  "price": 500000.00,
  "isFree": false,
  "features": ["Học 3 buổi/tuần", "Có chứng chỉ"],
  ...
}
```

---

#### `POST /api/courses` (Admin)

**Request:**

```json
{
  "name": "Khoá Côn Cơ Bản",
  "description": "Mô tả...",
  "level": "Beginner",
  "durationMonths": 3,
  "sessionsPerWeek": 3,
  "price": 500000,
  "isFree": false,
  "features": ["Học 3 buổi/tuần"],
  "thumbnailUrl": "https://...",
  "isFeatured": false
}
```

---

### Coaches Endpoints

#### `GET /api/coaches`

> ⚠️ **Vấn đề còn tồn đọng:** Field `title` trong response hiện trả về **số nguyên** (`1` hoặc `2`) do DTO dùng kiểu enum chưa được convert sang string trong serialization.

```json
{
  "id": "uuid",
  "fullName": "Trần Văn B",
  "title": 1,        // 1 = HeadCoach, 2 = AssistantCoach — xem bảng enum
  "bio": "...",
  "certifications": ["Bằng A", "Bằng B"],
  "achievements": ["Giải vàng 2024"],
  "isActive": true,
  ...
}
```

#### `POST/PUT /api/coaches` (Admin)

Khi tạo/cập nhật coach, field `title` nhận cả số nguyên hoặc chuỗi đều được:

```json
{ "title": 1 }      // hoặc
{ "title": "HeadCoach" }
```

---

### Achievements Endpoints

#### `GET /api/achievements`

```json
{
  "id": "uuid",
  "title": "HCV Giải Quốc Gia",
  "type": "Competition",
  "achievementDate": "2025-08-10T00:00:00Z",
  "isFeatured": true,
  ...
}
```

---

## Vấn đề còn tồn đọng

| Vấn đề                            | Endpoint           | Ảnh hưởng                                    | Workaround                                    |
| --------------------------------- | ------------------ | -------------------------------------------- | --------------------------------------------- |
| `CoachDto.Title` serialize là int | `GET /api/coaches` | Frontend nhận `1` hoặc `2`, cần map thủ công | Map `1` → "HLV Trưởng", `2` → "HLV Phụ trách" |

**Cách map phía frontend (JavaScript):**

```js
const COACH_TITLE = {
  1: "HLV Trưởng",
  2: "HLV Phụ trách",
};

const titleLabel = COACH_TITLE[coach.title] ?? "Không xác định";
```

---

## Checklist tích hợp frontend

- [ ] Cập nhật handler cho `POST /api/contact` từ `200` sang `201`
- [ ] Kiểm tra form đăng ký gửi đủ 4 field: `email`, `username`, `password`, `fullName`
- [ ] Thay các giá trị int enum bằng string khi gọi create/update APIs (Courses, Achievements)
- [ ] Cập nhật Admin dashboard hiển thị `status`, `type` dưới dạng string thay vì số
- [ ] Map `coach.title` (int) sang label tiếng Việt phía frontend tạm thời
- [ ] Kiểm tra logic lưu `expiresAt` từ auth response — giờ là dynamic thay vì luôn +1h

---

## Bảng enum tổng hợp

| Field               | Endpoint          | Giá trị hợp lệ                                      |
| ------------------- | ----------------- | --------------------------------------------------- |
| `user.role`         | Auth, Users       | `Guest` `Student` `SubAdmin` `SuperAdmin`           |
| `user.status`       | Users             | `Active` `Inactive` `Suspended`                     |
| `post.status`       | Posts             | `Draft` `Published` `Archived`                      |
| `comment.status`    | Admin             | `Pending` `Approved` `Spam` `Trash`                 |
| `course.level`      | Courses           | `Beginner` `Intermediate` `Advanced` `Professional` |
| `enrollment.status` | CourseEnrollments | `Pending` `Approved` `Rejected` `Completed`         |
| `contact.status`    | Contact           | `New` `Read` `Replied` `Archived`                   |
| `achievement.type`  | Achievements      | `Competition` `Certification` `Milestone` `Award`   |
| `coach.title`       | Coaches           | `1` (HeadCoach) `2` (AssistantCoach) ⚠️ vẫn là int  |
