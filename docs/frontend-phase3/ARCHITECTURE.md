# Bubble Chat Support — Kiến trúc & Tài liệu tích hợp Frontend

> Tài liệu này dành cho team **NextJS frontend**.
> Sau khi đọc xong bạn sẽ biết: mỗi API làm gì, Firebase path nào cần subscribe, flow end-to-end như thế nào.

---

## Mục lục

1. [Tổng quan kiến trúc](#1-tổng-quan-kiến-trúc)
2. [Luồng dữ liệu end-to-end](#2-luồng-dữ-liệu-end-to-end)
3. [Backend API Reference](#3-backend-api-reference)
4. [Firebase Realtime Database — Cấu trúc dữ liệu](#4-firebase-realtime-database--cấu-trúc-dữ-liệu)
5. [Frontend — Files & Hooks cần implement](#5-frontend--files--hooks-cần-implement)
6. [Biến môi trường](#6-biến-môi-trường)
7. [Checklist tích hợp](#7-checklist-tích-hợp)

---

## 1. Tổng quan kiến trúc

```
┌─────────────────────────────────────────────────────────────────┐
│                        USER BROWSER                             │
│  BubbleChat.tsx ──POST /api/v1/chat/message──► .NET 8 API       │
│       │                                            │            │
│       │                              ┌─────────────┴──────────┐ │
│       │                              │   ProcessChatHandler   │ │
│       │                              │  (MediatR Orchestrator)│ │
│       │                              └─────────────┬──────────┘ │
│       │                                            │            │
│       │                         confidence ≥ 0.75 ▼            │
│       │◄──────────── AI Answer ─── FallbackClassifierService    │
│       │                                            │            │
│       │                         confidence < 0.75 ▼            │
│       │                              ┌─────────────┴──────────┐ │
│       │                              │ IFirebasePresenceService│ │
│       │                              │ (check admin online?)  │ │
│       │                              └──────┬──────────┬──────┘ │
│       │                            online  │          │ offline │
│       │                                    ▼          ▼         │
│       │◄── HumanOnline ──── CreateChatRoom     SavePending+FCM  │
│       │    (chatRoomId)         Firebase     PendingUserMessage  │
│       │                                            │            │
│       │                              FcmNotificationService     │
│       │                              NotificationRetryService   │
│       │                              (BackgroundService retry)  │
└───────┼──────────────────────────────────────────────┼──────────┘
        │                                              │
        │ Firebase SDK (client)              FCM Push  │
        ▼                                              ▼
┌───────────────────┐                    ┌─────────────────────┐
│ Firebase RTDB     │                    │  ADMIN BROWSER      │
│ /chats/{chatId}   │◄────subscribe──────│  AdminDashboard.tsx │
│ /notifications/.. │                    │  useAdminPresence   │
│ /presence/admins  │◄────write online───│  (sets FCM token)   │
└───────────────────┘                    └─────────────────────┘
```

### Công nghệ

| Layer | Tech |
|---|---|
| Backend | .NET 8, Clean Architecture, CQRS (MediatR) |
| AI | Google Gemini 1.5 Flash (Semantic Kernel) |
| RAG | pgvector (PostgreSQL) + Google Embedding API |
| Realtime | Firebase Realtime Database (REST từ backend, SDK từ frontend) |
| Push | Firebase Cloud Messaging (FCM) |
| Persistence | PostgreSQL (Aiven cloud) |
| Auth | JWT Bearer (HS256) |

---

## 2. Luồng dữ liệu end-to-end

### 2A. User gửi chat → AI trả lời

```
User types message
  → POST /api/v1/chat/message { sessionId, userMessage, history[] }
  → FallbackClassifierService: RAG lookup + Gemini classify
  → confidence ≥ 0.75
  → { type: "AI", answer: "..." }
  → BubbleChat.tsx hiển thị answer
```

### 2B. User gửi chat → Admin online → Realtime chat

```
User types message
  → POST /api/v1/chat/message
  → confidence < 0.75
  → IFirebasePresenceService.GetFirstOnlineAdminAsync()
  → Admin ONLINE → CreateChatRoomAsync()
  → Firebase: /chats/{chatId}/metadata, /chats/{chatId}/messages/[0]
  → { type: "HumanOnline", chatRoomId: "chat_xxx" }

Frontend (user):
  → useChatRoom(chatRoomId) → subscribe onChildAdded /chats/{chatId}/messages
  → User thấy admin messages realtime

Frontend (admin):
  → AdminDashboard.tsx "Live Chats" tab subscribe /chats
  → Admin thấy chat room mới
  → Admin reply → POST /api/admin/chats/{chatId}/message
  → Firebase: /chats/{chatId}/messages/[1]
  → User thấy ngay qua useChatRoom subscription
```

### 2C. User gửi chat → Admin offline → Pending message + FCM

```
User types message
  → POST /api/v1/chat/message
  → confidence < 0.75
  → No admin online
  → PendingUserMessage saved to PostgreSQL
  → FcmNotificationService.NotifyAllAdminsAsync()
    → Primary: FCM tokens từ Firebase /presence/admins
    → Fallback: FCM tokens từ PostgreSQL users.fcm_token
  → { type: "LeftMessage", pendingMessageId: "..." }

Frontend (user):
  → Hiện thông báo "Đã ghi nhận câu hỏi, admin sẽ reply sớm"
  → useUserNotification(sessionId) → subscribe /notifications/{sessionId}
  → Khi admin reply: Firebase /notifications/{sessionId} được set
  → User thấy reply notification (nếu tab còn mở)

Admin (later):
  → Nhận FCM push notification trên browser/device
  → Mở AdminDashboard → GET /api/admin/pending-messages
  → Reply → POST /api/admin/pending-messages/{id}/reply
  → Backend: cập nhật PG status=Replied + push Firebase /notifications/{sessionId}

NotificationRetryService (background):
  → Mỗi 5 phút scan pending messages chưa được reply
  → Retry FCM notification (max 3 lần, exponential backoff: 5→15→60 phút)
```

### 2D. Admin setup (login)

```
Admin login → nhận JWT token
→ Gọi useAdminPresence(): write /presence/admins/{adminId} { online: true, fcmToken, displayName }
   + onDisconnect: set { online: false, lastSeen: timestamp }
→ Gọi useAdminFcm.requestPermission():
   → Browser grant notification
   → getToken(messaging, { vapidKey })
   → POST /api/admin/fcm-token { token } → lưu vào PG users.fcm_token
```

---

## 3. Backend API Reference

Base URL: `https://your-domain.com` (hoặc `https://localhost:52040` dev)

### 3.1 Chat (Public)

#### `POST /api/v1/chat/message`

Gửi message từ user. **Không cần auth.**

**Request:**
```json
{
  "sessionId": "sess_abc123",
  "userMessage": "Học phí khóa cơ bản bao nhiêu?",
  "history": [
    { "role": "user", "content": "Xin chào" },
    { "role": "assistant", "content": "Chào bạn!" }
  ],
  "userId": null
}
```

**Response:**
```json
{
  "isSuccess": true,
  "data": {
    "type": "AI",
    "answer": "Học phí khóa cơ bản là 500.000đ/tháng...",
    "chatRoomId": null,
    "pendingMessageId": null
  },
  "error": null
}
```

| `type` | Ý nghĩa | Frontend action |
|---|---|---|
| `"AI"` | AI đã trả lời | Hiển thị `answer` |
| `"HumanOnline"` | Admin online, chat room đã tạo | Subscribe `chatRoomId` qua useChatRoom |
| `"LeftMessage"` | Admin offline, đã lưu pending | Hiện thông báo "sẽ reply sớm" |

---

### 3.2 Admin — Pending Messages

> Tất cả endpoint `/api/admin/*` yêu cầu `Authorization: Bearer {jwt}` với role `Admin` hoặc `Editor`.

#### `GET /api/admin/pending-messages`

Lấy danh sách pending messages.

**Query params:**
| Param | Type | Default | Mô tả |
|---|---|---|---|
| `status` | int? | null | 0=Pending, 1=Assigned, 2=Replied, 3=Closed. Nếu null → trả về Pending+Assigned |
| `page` | int | 1 | Trang |
| `pageSize` | int | 20 | Số item/trang (max 100) |

**Response:**
```json
{
  "isSuccess": true,
  "data": [
    {
      "id": "550e8400-e29b-41d4-a716-446655440000",
      "sessionId": "sess_abc123",
      "userMessage": "Học phí bao nhiêu?",
      "status": "Pending",
      "adminReply": null,
      "assignedAdminId": null,
      "createdAt": "2026-04-01T10:00:00Z",
      "repliedAt": null
    }
  ],
  "error": null
}
```

#### `POST /api/admin/pending-messages/{id}/reply`

Admin reply một pending message.

**Request:**
```json
{ "text": "Học phí khóa cơ bản là 500.000đ/tháng." }
```

**Side effects:**
- `PendingUserMessage.Status` → `Replied`
- Ghi `AdminReply` + `RepliedAt` vào PostgreSQL
- Push Firebase `/notifications/{sessionId}` → user thấy ngay nếu tab mở

**Response:** `{ "isSuccess": true, "data": true }`

#### `POST /api/admin/pending-messages/{id}/close`

Đóng pending message (bỏ qua, không reply).

**Response:** `{ "isSuccess": true, "data": true }`

#### `GET /api/admin/pending-count`

Số lượng pending messages chưa xử lý (badge đỏ trên icon).

**Response:**
```json
{ "isSuccess": true, "data": { "count": 5 } }
```

> Poll endpoint này mỗi 30 giây để cập nhật badge. Hoặc dùng Firebase để realtime.

---

### 3.3 Admin — Active Firebase Chat Rooms

#### `POST /api/admin/chats/{chatId}/message`

Admin gửi message vào chat room đang active.

**Request:**
```json
{ "text": "Chào bạn! Mình có thể giúp gì?" }
```

**Side effects:** Ghi vào Firebase `/chats/{chatId}/messages` → user thấy ngay qua useChatRoom.

#### `POST /api/admin/chats/{chatId}/close`

Đóng chat room (status → "closed" trong Firebase).

**Response:** `{ "isSuccess": true, "data": true }`

---

### 3.4 Admin — FCM Token

#### `POST /api/admin/fcm-token`

Lưu FCM device token của admin vào PostgreSQL (fallback khi offline).

**Request:**
```json
{ "token": "fFeSM2Vn...FCM_TOKEN_HERE" }
```

> Gọi endpoint này sau khi `getToken(messaging, { vapidKey })` thành công.

---

## 4. Firebase Realtime Database — Cấu trúc dữ liệu

```
/
├── presence/
│   └── admins/
│       └── {adminId}/
│           ├── online: boolean
│           ├── fcmToken: string
│           ├── displayName: string
│           └── lastSeen: number (unix ms)
│
├── chats/
│   └── {chatId}/               ← chatId = "chat_{timestamp}_{8chars}"
│       ├── metadata/
│       │   ├── userId: string  (sessionId)
│       │   ├── adminId: string
│       │   ├── status: "open" | "closed"
│       │   ├── userQuestion: string
│       │   ├── createdAt: number (unix ms)
│       │   └── closedAt: number (unix ms, nếu closed)
│       └── messages/
│           └── {pushKey}/
│               ├── sender: "user" | "admin:{adminId}"
│               ├── text: string
│               ├── timestamp: number (unix ms)
│               └── read: boolean
│
└── notifications/
    └── {sessionId}/
        ├── reply: string       (admin reply text)
        ├── adminId: string
        ├── repliedAt: number (unix ms)
        └── read: boolean
```

### Firebase Security Rules (đề xuất)

```json
{
  "rules": {
    "presence": {
      "admins": {
        "$adminId": {
          ".read": "auth != null",
          ".write": "auth != null && auth.uid == $adminId"
        }
      }
    },
    "chats": {
      "$chatId": {
        ".read": "auth != null",
        ".write": "auth != null",
        "messages": {
          ".read": true,
          ".write": true
        }
      }
    },
    "notifications": {
      "$sessionId": {
        ".read": true,
        ".write": "auth != null"
      }
    }
  }
}
```

---

## 5. Frontend — Files & Hooks cần implement

Tất cả reference files nằm trong `docs/frontend-phase3/`.

### 5.1 Cấu trúc thư mục

```
src/
├── lib/
│   └── firebase.ts              ← Firebase client init
├── hooks/
│   ├── useChatRoom.ts           ← Subscribe Firebase chat messages (user)
│   ├── useAdminPresence.ts      ← Set presence + onDisconnect (admin)
│   ├── useAdminFcm.ts           ← Get FCM token + save to backend
│   └── useUserNotification.ts  ← Subscribe admin reply notification (user)
├── components/
│   ├── chat/
│   │   └── BubbleChat.tsx       ← Widget chat cho user
│   └── admin/
│       └── AdminChatDashboard.tsx ← Dashboard quản lý chat (admin)
└── public/
    └── firebase-messaging-sw.js ← Service worker FCM background
```

### 5.2 Tóm tắt mỗi hook

#### `lib/firebase.ts`
- Init `FirebaseApp` với env vars
- Export `getFirebaseMessaging()` (lazy, chỉ chạy client-side)
- Export `db` (getDatabase) để dùng trong hooks

#### `hooks/useChatRoom.ts`
```typescript
// Dùng khi response type = "HumanOnline"
const { messages } = useChatRoom(chatRoomId);
// Subscribe onChildAdded /chats/{chatId}/messages
// messages: Array<{ sender, text, timestamp, read }>
```

#### `hooks/useAdminPresence.ts`
```typescript
// Gọi trong admin layout
useAdminPresence(adminId, displayName, fcmToken);
// Writes /presence/admins/{adminId} { online: true, fcmToken, displayName }
// onDisconnect: { online: false, lastSeen: Date.now() }
```

#### `hooks/useAdminFcm.ts`
```typescript
const { fcmToken, notificationPermission, requestPermission } = useAdminFcm();
// requestPermission():
//   1. Notification.requestPermission()
//   2. getToken(messaging, { vapidKey })
//   3. POST /api/admin/fcm-token { token }
//   4. return token
```

#### `hooks/useUserNotification.ts`
```typescript
// Dùng khi response type = "LeftMessage"
const { notification } = useUserNotification(sessionId);
// Subscribe onValue /notifications/{sessionId}
// notification: { reply, adminId, repliedAt } | null
```

### 5.3 BubbleChat.tsx — State Machine

```
IDLE
  → user types + sends
  → loading...
  → POST /api/v1/chat/message

AI_RESPONSE
  → hiển thị answer text
  → user có thể hỏi tiếp (gửi lại)

HUMAN_ONLINE
  → hiển thị "Đang kết nối với admin..."
  → useChatRoom(chatRoomId) active
  → user & admin chat realtime

LEFT_MESSAGE
  → hiển thị "Đã ghi nhận! Admin sẽ phản hồi sớm."
  → useUserNotification(sessionId) active
  → khi notification arrives: hiển thị reply
```

### 5.4 AdminChatDashboard.tsx — Tabs

**Tab "Pending":**
- Poll `GET /api/admin/pending-count` mỗi 30s → hiển thị badge
- Fetch `GET /api/admin/pending-messages` khi mở tab
- Mỗi item: hiển thị message + nút Reply / Close
- Reply → `POST /api/admin/pending-messages/{id}/reply`
- Close → `POST /api/admin/pending-messages/{id}/close`

**Tab "Live Chats":**
- Subscribe Firebase `/chats` → list all open rooms
- Mở room → useChatRoom(chatId) → hiển thị messages
- Gửi message → `POST /api/admin/chats/{chatId}/message`
- Close → `POST /api/admin/chats/{chatId}/close`

---

## 6. Biến môi trường

### NextJS `.env.local`

```env
# Firebase Client SDK
NEXT_PUBLIC_FIREBASE_API_KEY=AIza...
NEXT_PUBLIC_FIREBASE_AUTH_DOMAIN=your-project.firebaseapp.com
NEXT_PUBLIC_FIREBASE_PROJECT_ID=your-project-id
NEXT_PUBLIC_FIREBASE_STORAGE_BUCKET=your-project.appspot.com
NEXT_PUBLIC_FIREBASE_MESSAGING_SENDER_ID=123456789
NEXT_PUBLIC_FIREBASE_APP_ID=1:123456789:web:abc123
NEXT_PUBLIC_FIREBASE_DATABASE_URL=https://your-project-default-rtdb.firebaseio.com
NEXT_PUBLIC_FIREBASE_VAPID_KEY=BNab...  # From Firebase Console → Project Settings → Cloud Messaging → Web Push certificates

# Backend API
NEXT_PUBLIC_API_BASE_URL=https://localhost:52040
```

### .NET Backend `appsettings.json`

```json
{
  "Firebase": {
    "ProjectId": "your-project-id",
    "ServiceAccountPath": "secrets/firebase-service-account.json",
    "DatabaseUrl": "https://your-project-default-rtdb.firebaseio.com"
  },
  "GeminiSettings": {
    "ApiKey": "AIza...",
    "ChatModel": "gemini-1.5-flash",
    "EmbeddingModel": "text-embedding-004",
    "ConfidenceThreshold": 0.75
  }
}
```

---

## 7. Checklist tích hợp

### Backend (đã hoàn thành ✅)

- [x] **Phase 1**: PendingUserMessage entity + migration, IFirebasePresenceService, IFcmNotificationService
- [x] **Phase 2**: FallbackClassifierService (RAG + Gemini), ProcessChatHandler (MediatR), POST /api/v1/chat/message
- [x] **Phase 3**: FirebaseChatService (CreateRoom, SendMessage, Close, NotifyReply), AdminChatController (pending + live chat endpoints)
- [x] **Phase 4**: User.FcmToken + migration, FcmNotificationService PG fallback, POST /api/admin/fcm-token
- [x] **Phase 5**: NotificationRetryService (BackgroundService, exponential backoff), GET /api/admin/pending-count, POST /api/admin/pending-messages/{id}/close

### Frontend (cần implement)

- [ ] Copy `lib/firebase.ts` → configure với env vars
- [ ] Copy `public/firebase-messaging-sw.js` → update firebaseConfig
- [ ] Copy `hooks/useChatRoom.ts`
- [ ] Copy `hooks/useAdminPresence.ts`
- [ ] Copy `hooks/useAdminFcm.ts`
- [ ] Copy `hooks/useUserNotification.ts`
- [ ] Copy `components/chat/BubbleChat.tsx` → tích hợp vào layout
- [ ] Copy `components/admin/AdminChatDashboard.tsx` → tích hợp vào admin layout
- [ ] Admin layout: gọi `useAdminPresence` + `useAdminFcm.requestPermission()` khi login
- [ ] Test flow: gửi message → AI reply
- [ ] Test flow: gửi message (low confidence) + admin online → HumanOnline realtime chat
- [ ] Test flow: gửi message (low confidence) + admin offline → LeftMessage + FCM notification
- [ ] Test flow: admin reply pending → user thấy notification

---

## 8. Sơ đồ sequence chi tiết

### Flow 2B: HumanOnline

```
User                    Backend (.NET)              Firebase RTDB          Admin
 │                            │                          │                    │
 ├─POST /chat/message─────────►│                          │                    │
 │                            │─GET /presence/admins────►│                    │
 │                            │◄────── admin online ──────┤                    │
 │                            │─PUT /chats/{id}/metadata─►│                    │
 │                            │─POST /chats/{id}/messages►│                    │
 │◄─{ type:HumanOnline, chatRoomId }──────────────────────│                    │
 │                            │                          │◄─subscribe /chats──┤
 │─subscribe onChildAdded─────────────────────────────────►│                    │
 │    /chats/{chatId}/msgs    │                          │                    │
 │                            │                          │────notify new room─►│
 │                            │                          │                    │
 │ types reply                │                          │                    │
 │────────────────────────────────────────────────────────────POST /admin/chats/{id}/msg
 │                            │◄───────────────────────────────────────────────┤
 │                            │─POST /chats/{id}/messages►│                    │
 │◄─── onChildAdded fires ────────────────────────────────┤│                    │
 │  (shows admin message)     │                          │                    │
```

### Flow 2C: LeftMessage + Retry

```
User               Backend (.NET)          PostgreSQL          Admin Device
 │                      │                      │                    │
 ├─POST /chat/msg───────►│                      │                    │
 │                      │─check presence───────► (no admin online)  │
 │                      │─INSERT PendingMsg────►│                    │
 │                      │─FCM notify────────────────────────────────►│
 │◄──{ type:LeftMessage }│                      │                    │
 │                      │                      │                    │
 │ (5 min later)        │                      │                    │
 │            NotificationRetryService polls   │                    │
 │                      │─SELECT pending───────►│                    │
 │                      │─FCM retry─────────────────────────────────►│
 │                      │─UPDATE NextNotificationAt─►│                    │
 │                      │                      │                    │
 │                      │                (Admin opens dashboard)     │
 │                      │◄─GET /admin/pending-messages────────────────┤
 │                      │──────────────────────────────────────────►│
 │                      │◄─POST /admin/pending-messages/{id}/reply───┤
 │                      │─UPDATE status=Replied────►│                │
 │                      │─PUT /notifications/{session}─►Firebase     │
 │◄──onValue fires────────────────────────────────────────────────── │
 │   (shows admin reply)│                      │                    │
```
