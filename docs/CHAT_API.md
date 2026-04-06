# Chat API — Tài liệu tích hợp Frontend

> **Base URL**: `https://<host>/api/v1/chat` (user endpoints) và `https://<host>/api/admin` (admin endpoints)  
> **Response format**: Tất cả endpoint trả về `{ "isSuccess": bool, "data": ..., "error": string | null }`

---

## Tổng quan luồng

```
User gửi tin nhắn
        │
        ▼
POST /api/v1/chat/message
        │
        ├─── type: "AI"          → Hiển thị answer ngay
        │
        ├─── type: "HumanOnline" → Subscribe Firebase chat room (chatRoomId)
        │                          Nhắn tin realtime qua Firebase SDK
        │
        └─── type: "LeftMessage" → Hiển thị "Chúng tôi sẽ phản hồi sớm"
                                   Listen Firebase /notifications/{sessionId}
                                   Admin sẽ reply → user nhận được qua Firebase
```

---

## Session ID

Mỗi trình duyệt cần một `sessionId` ổn định để hệ thống theo dõi lịch sử và nhận notification.

```typescript
// Tạo hoặc lấy session ID từ localStorage
function getSessionId(): string {
  const key = 'cnk_session_id';
  let id = localStorage.getItem(key);
  if (!id) {
    id = crypto.randomUUID();
    localStorage.setItem(key, id);
  }
  return id;
}
```

---

## Endpoint người dùng

### 1. `POST /api/v1/chat/message`

**Gửi tin nhắn và nhận kết quả routing.**

```
Authorization: Bearer <jwt>   (optional — để liên kết lịch sử với tài khoản)
Content-Type: application/json
```

**Request body**

```typescript
interface ChatMessageRequest {
  sessionId: string;                  // từ localStorage
  message: string;                    // tin nhắn của user
  history: ChatHistoryItem[];         // lịch sử hội thoại (tối đa 10 turns gần nhất)
}

interface ChatHistoryItem {
  role: "user" | "assistant";
  content: string;
}
```

**Response — type: "AI"**

```typescript
{
  "isSuccess": true,
  "data": {
    "type": "AI",
    "answer": "Học phí tháng tại CLB là 350.000đ/tháng...",
    "chatRoomId": null,
    "messageId": null
  }
}
```

→ Hiển thị `answer` trong giao diện chat.

---

**Response — type: "HumanOnline"**

```typescript
{
  "isSuccess": true,
  "data": {
    "type": "HumanOnline",
    "answer": null,
    "chatRoomId": "chat_1712345678_a1b2c3d4",   // Firebase chat room ID
    "messageId": null
  }
}
```

→ Kết nối Firebase Realtime Database, subscribe vào `chatRoomId` để chat realtime với admin.

Xem [Luồng B — Admin Online](#luồng-b--admin-online) bên dưới.

---

**Response — type: "LeftMessage"**

```typescript
{
  "isSuccess": true,
  "data": {
    "type": "LeftMessage",
    "answer": null,
    "chatRoomId": null,
    "messageId": "550e8400-e29b-41d4-a716-446655440000"  // PendingMessage ID
  }
}
```

→ Hiển thị thông báo "Tin nhắn của bạn đã được ghi nhận. Chúng tôi sẽ phản hồi sớm nhất có thể."  
→ Subscribe Firebase `/notifications/{sessionId}` để nhận reply của admin.

Xem [Luồng A — Admin Offline](#luồng-a--admin-offline) bên dưới.

---

**Errors**

| HTTP | Khi nào |
|------|---------|
| 400  | `message` rỗng, `history[].role` không phải "user"/"assistant" |
| 500  | Lỗi server nội bộ (Gemini timeout, DB lỗi, v.v.) |

---

### 2. `GET /api/v1/chat/history?sessionId={sessionId}`

**Lấy lịch sử hội thoại của session.** Dùng khi user refresh trang để khôi phục giao diện.

**Response**

```typescript
{
  "isSuccess": true,
  "data": {
    "sessionId": "a1b2c3...",
    "status": "BotHandling" | "HumanHandoff" | "Closed" | "None",
    "handoffType": null | "Firebase" | "Pending",
    "firebaseChatRoomId": null | "chat_...",
    "pendingMessageId": null | "550e8400-...",
    "messages": [
      {
        "id": "uuid",
        "role": "User" | "Bot" | "Admin",
        "content": "Cho tôi hỏi học phí...",
        "senderAdminId": null,          // null với User và Bot
        "createdAt": "2026-04-06T15:30:00Z"
      }
    ]
  }
}
```

**Cách dùng khi refresh trang:**

```typescript
async function restoreSession(sessionId: string) {
  const res = await fetch(`/api/v1/chat/history?sessionId=${sessionId}`);
  const { data } = await res.json();

  if (data.status === "None" || data.messages.length === 0) return; // Chưa có lịch sử

  // Khôi phục messages vào state
  setMessages(data.messages.map(m => ({
    role: m.role === "User" ? "user" : "assistant",
    content: m.content
  })));

  // Nếu đang trong handoff, subscribe lại
  if (data.status === "HumanHandoff") {
    if (data.handoffType === "Firebase" && data.firebaseChatRoomId) {
      subscribeToFirebaseChatRoom(data.firebaseChatRoomId);
    } else if (data.handoffType === "Pending") {
      subscribeToAdminReply(sessionId);
    }
  }
}
```

---

### 3. `POST /api/v1/chat/stream` (SSE — pure bot, không routing)

**Chỉ dùng khi cần streaming text mà không cần routing hoặc lưu lịch sử.**

```typescript
interface ChatStreamRequest {
  message: string;
  history: ChatHistoryItem[];
}
```

SSE response format:
```
data: {"content":"Học phí"}\n\n
data: {"content":" tháng"}\n\n
data: {"content":" là..."}\n\n
data: [DONE]\n\n
```

Khi lỗi server sẽ trả về:
```
data: {"error":"Xin lỗi, đã xảy ra lỗi kết nối..."}\n\n
data: [DONE]\n\n
```

---

## Luồng A — Admin Offline (LeftMessage)

Khi không có admin nào online, tin nhắn được lưu và FCM push được gửi đến tất cả admin devices.

### Phía user: Subscribe notification Firebase

```typescript
import { ref, onValue, off } from 'firebase/database';
import { database } from '@/lib/firebase';

function subscribeToAdminReply(
  sessionId: string,
  onReply: (reply: AdminReply) => void
): () => void {
  const notifRef = ref(database, `notifications/${sessionId}`);

  const unsubscribe = onValue(notifRef, (snapshot) => {
    const data = snapshot.val();
    if (data && !data.read) {
      onReply({
        text: data.reply,
        adminId: data.adminId,
        repliedAt: new Date(data.repliedAt)
      });
    }
  });

  return () => off(notifRef, 'value', unsubscribe);
}

interface AdminReply {
  text: string;
  adminId: string;
  repliedAt: Date;
}
```

**Firebase RTDB path**: `/notifications/{sessionId}`

```json
{
  "reply": "Chào bạn, học phí tháng...",
  "adminId": "admin-uuid",
  "repliedAt": 1712345678000,
  "read": false
}
```

---

## Luồng B — Admin Online (HumanOnline)

Khi có admin online, một Firebase chat room được tạo. Frontend subscribe realtime vào room đó.

### Phía user: Subscribe và gửi tin nhắn Firebase

```typescript
import { ref, push, onChildAdded, off, serverTimestamp } from 'firebase/database';
import { database } from '@/lib/firebase';

interface FirebaseMessage {
  sender: string;      // "user" | "admin:<adminId>"
  text: string;
  timestamp: number;
  read: boolean;
}

// Subscribe nhận tin nhắn mới
function subscribeToChatRoom(
  chatRoomId: string,
  onMessage: (msg: FirebaseMessage) => void
): () => void {
  const messagesRef = ref(database, `chats/${chatRoomId}/messages`);

  const unsubscribe = onChildAdded(messagesRef, (snapshot) => {
    onMessage(snapshot.val() as FirebaseMessage);
  });

  return () => off(messagesRef, 'child_added', unsubscribe);
}

// Gửi tin nhắn từ user
async function sendUserMessage(chatRoomId: string, text: string): Promise<void> {
  const messagesRef = ref(database, `chats/${chatRoomId}/messages`);
  await push(messagesRef, {
    sender: 'user',
    text,
    timestamp: Date.now(),
    read: false
  });
}

// Subscribe trạng thái chat room (để biết khi admin đóng)
function subscribeChatStatus(
  chatRoomId: string,
  onStatusChange: (status: 'open' | 'closed') => void
): () => void {
  const statusRef = ref(database, `chats/${chatRoomId}/metadata/status`);

  const unsubscribe = onValue(statusRef, (snapshot) => {
    onStatusChange(snapshot.val() as 'open' | 'closed');
  });

  return () => off(statusRef, 'value', unsubscribe);
}
```

**Firebase RTDB structure:**

```
/chats/{chatId}/
  metadata:
    userId: "session-id"
    adminId: "admin-uuid"
    status: "open" | "closed"
    userQuestion: "Cho tôi hỏi..."
    createdAt: 1712345678000
    closedAt: 1712345999000   (khi status = "closed")
  messages:
    -Nabc123:
      sender: "user"
      text: "Cho tôi hỏi..."
      timestamp: 1712345678000
      read: false
    -NaBC456:
      sender: "admin:uuid"
      text: "Xin chào! Tôi có thể giúp gì?"
      timestamp: 1712345700000
      read: false
```

---

## Endpoint Admin (RequireAdminArea)

> Tất cả endpoint admin yêu cầu `Authorization: Bearer <jwt>` của tài khoản SuperAdmin hoặc SubAdmin.

---

### 1. `POST /api/admin/fcm-token`

**Đăng ký FCM device token để nhận push notification khi có tin nhắn mới.**

Gọi endpoint này sau mỗi lần admin đăng nhập và khi `onMessage` callback của FCM báo token thay đổi.

```typescript
interface SaveFcmTokenRequest {
  token: string;   // FCM device token
}
```

```typescript
// Đăng ký service worker và lấy FCM token
import { getToken, onMessage } from 'firebase/messaging';
import { messaging } from '@/lib/firebase';

async function registerFcmToken(jwtToken: string): Promise<void> {
  const fcmToken = await getToken(messaging, {
    vapidKey: process.env.NEXT_PUBLIC_FIREBASE_VAPID_KEY
  });

  await fetch('/api/admin/fcm-token', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${jwtToken}`
    },
    body: JSON.stringify({ token: fcmToken })
  });
}
```

---

### 2. `GET /api/admin/pending-messages`

**Lấy danh sách tin nhắn đang chờ xử lý.**

```
GET /api/admin/pending-messages?status=Pending&page=1&pageSize=20
```

Query params:
| Param | Type | Default | Mô tả |
|-------|------|---------|-------|
| `status` | `Pending\|Assigned\|Replied\|Closed` | `Pending,Assigned` | Lọc theo trạng thái |
| `page` | number | 1 | Trang hiện tại |
| `pageSize` | number | 20 | Số item mỗi trang (max 100) |

**Response**

```typescript
{
  "isSuccess": true,
  "data": [
    {
      "id": "550e8400-e29b-41d4-a716-446655440000",
      "sessionId": "a1b2c3...",
      "userMessage": "Cho tôi hỏi lịch học cuối tuần",
      "status": "Pending",
      "adminReply": null,
      "assignedAdminId": null,
      "createdAt": "2026-04-06T14:30:00Z",
      "repliedAt": null
    }
  ]
}
```

---

### 3. `POST /api/admin/pending-messages/{id}/reply`

**Admin trả lời tin nhắn pending.**

Backend sẽ:
1. Cập nhật status → `Replied` trong PostgreSQL
2. Lưu admin message vào `conversation_messages`
3. Ghi notification vào Firebase `/notifications/{sessionId}` → user nhận được realtime

```typescript
interface ReplyRequest {
  text: string;
}
```

```typescript
await fetch(`/api/admin/pending-messages/${messageId}/reply`, {
  method: 'POST',
  headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${jwt}` },
  body: JSON.stringify({ text: "Xin chào! Lịch học cuối tuần là..." })
});
```

---

### 4. `GET /api/admin/pending-count`

**Lấy số lượng tin nhắn chưa xử lý (badge).**

```typescript
{
  "isSuccess": true,
  "data": { "count": 5 }
}
```

Poll mỗi 30–60 giây, hoặc dùng Firebase Realtime Database để push realtime nếu cần.

---

### 5. `POST /api/admin/pending-messages/{id}/close`

**Bỏ qua / đóng một pending message (không gửi notification cho user).**

---

### 6. `POST /api/admin/chats/{chatId}/message`

**Admin gửi message vào Firebase chat room đang active (luồng B).**

> Tùy chọn: Admin có thể gửi trực tiếp qua Firebase SDK (không qua API này).  
> Dùng API này nếu muốn persist message vào PostgreSQL trước.

```typescript
interface SendChatMessageRequest {
  text: string;
}
```

---

### 7. `POST /api/admin/chats/{chatId}/close`

**Đóng Firebase chat room.** Backend sẽ:
1. Cập nhật Firebase `/chats/{chatId}/metadata/status` → `"closed"`
2. Cập nhật `ChatSession.Status` → `Closed` trong PostgreSQL

User sẽ nhận được sự kiện status change qua Firebase subscription.

---

## Firebase Presence (Admin)

Admin phải tự duy trì presence trong Firebase Realtime Database để hệ thống biết ai đang online.

**Path**: `/presence/admins/{adminId}`

```typescript
import { ref, onDisconnect, set, serverTimestamp } from 'firebase/database';
import { database } from '@/lib/firebase';

async function setAdminOnline(adminId: string, fcmToken: string, displayName: string) {
  const presenceRef = ref(database, `presence/admins/${adminId}`);

  // Khi disconnected → tự động set offline
  await onDisconnect(presenceRef).update({
    online: false,
    lastSeen: serverTimestamp()
  });

  // Set online
  await set(presenceRef, {
    online: true,
    fcmToken,
    displayName,
    lastSeen: serverTimestamp()
  });
}

async function setAdminOffline(adminId: string) {
  const presenceRef = ref(database, `presence/admins/${adminId}`);
  await set(presenceRef, {
    online: false,
    lastSeen: serverTimestamp()
  });
}
```

**Gọi `setAdminOnline` sau khi admin đăng nhập thành công.**  
`onDisconnect` đảm bảo status offline ngay cả khi tab bị đóng đột ngột.

---

## Firebase Security Rules (tham khảo)

```json
{
  "rules": {
    "chats": {
      "$chatId": {
        "metadata": { ".read": true, ".write": "auth != null" },
        "messages": {
          ".read": true,
          ".write": true
        }
      }
    },
    "notifications": {
      "$sessionId": {
        ".read": true,
        ".write": "auth != null && auth.token.role in ['SuperAdmin', 'SubAdmin']"
      }
    },
    "presence": {
      "admins": {
        "$adminId": {
          ".read": true,
          ".write": "auth != null && auth.uid == $adminId"
        }
      }
    }
  }
}
```

---

## Ví dụ tích hợp đầy đủ (React hook)

```typescript
// hooks/useChat.ts

import { useState, useEffect, useRef } from 'react';
import { ref, onValue, onChildAdded, push, off } from 'firebase/database';
import { database } from '@/lib/firebase';

type ChatState = 'idle' | 'loading' | 'ai' | 'human-online' | 'left-message' | 'error';

interface Message {
  role: 'user' | 'assistant' | 'admin';
  content: string;
}

export function useChat() {
  const sessionId = getSessionId();
  const [state, setState] = useState<ChatState>('idle');
  const [messages, setMessages] = useState<Message[]>([]);
  const [chatRoomId, setChatRoomId] = useState<string | null>(null);
  const unsubscribeRef = useRef<(() => void) | null>(null);

  // Khôi phục lịch sử khi mount
  useEffect(() => {
    restoreHistory();
    return () => unsubscribeRef.current?.();
  }, []);

  async function restoreHistory() {
    const res = await fetch(`/api/v1/chat/history?sessionId=${sessionId}`);
    const { data } = await res.json();

    if (data.status === 'None' || data.messages.length === 0) return;

    setMessages(data.messages.map((m: any) => ({
      role: m.role === 'User' ? 'user' : m.role === 'Bot' ? 'assistant' : 'admin',
      content: m.content
    })));

    if (data.status === 'HumanHandoff') {
      if (data.handoffType === 'Firebase' && data.firebaseChatRoomId) {
        subscribeFirebase(data.firebaseChatRoomId);
        setState('human-online');
      } else if (data.handoffType === 'Pending') {
        subscribeNotification();
        setState('left-message');
      }
    } else if (data.status === 'BotHandling') {
      setState('ai');
    }
  }

  async function sendMessage(text: string) {
    setMessages(prev => [...prev, { role: 'user', content: text }]);
    setState('loading');

    // Nếu đang trong human-online → gửi qua Firebase trực tiếp
    if (state === 'human-online' && chatRoomId) {
      const messagesRef = ref(database, `chats/${chatRoomId}/messages`);
      await push(messagesRef, { sender: 'user', text, timestamp: Date.now(), read: false });
      setState('human-online');
      return;
    }

    const history = messages.slice(-10).map(m => ({
      role: m.role === 'admin' ? 'assistant' : m.role,
      content: m.content
    }));

    try {
      const jwt = getAuthToken(); // nullable
      const res = await fetch('/api/v1/chat/message', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          ...(jwt ? { 'Authorization': `Bearer ${jwt}` } : {})
        },
        body: JSON.stringify({ sessionId, message: text, history })
      });

      const { data } = await res.json();

      if (data.type === 'AI') {
        setMessages(prev => [...prev, { role: 'assistant', content: data.answer }]);
        setState('ai');
      } else if (data.type === 'HumanOnline') {
        setChatRoomId(data.chatRoomId);
        subscribeFirebase(data.chatRoomId);
        setState('human-online');
      } else if (data.type === 'LeftMessage') {
        subscribeNotification();
        setState('left-message');
      }
    } catch {
      setState('error');
    }
  }

  function subscribeFirebase(roomId: string) {
    const messagesRef = ref(database, `chats/${roomId}/messages`);
    // Bỏ qua tin nhắn đầu tiên (đã hiển thị từ local state)
    let isFirst = true;

    const handle = onChildAdded(messagesRef, (snapshot) => {
      if (isFirst) { isFirst = false; return; }
      const msg = snapshot.val();
      if (msg.sender.startsWith('admin:')) {
        setMessages(prev => [...prev, { role: 'admin', content: msg.text }]);
      }
    });

    // Subscribe status để biết khi admin đóng
    const statusRef = ref(database, `chats/${roomId}/metadata/status`);
    onValue(statusRef, (snap) => {
      if (snap.val() === 'closed') setState('ai');
    });

    unsubscribeRef.current = () => {
      off(messagesRef, 'child_added', handle);
      off(statusRef);
    };
  }

  function subscribeNotification() {
    const notifRef = ref(database, `notifications/${sessionId}`);
    onValue(notifRef, (snapshot) => {
      const data = snapshot.val();
      if (data?.reply && !data.read) {
        setMessages(prev => [...prev, { role: 'admin', content: data.reply }]);
        setState('ai'); // Sau khi admin reply, quay về state bình thường
      }
    });

    unsubscribeRef.current = () => off(notifRef);
  }

  return { state, messages, sendMessage };
}

function getSessionId(): string {
  const key = 'cnk_session_id';
  let id = localStorage.getItem(key);
  if (!id) { id = crypto.randomUUID(); localStorage.setItem(key, id); }
  return id;
}

function getAuthToken(): string | null {
  return localStorage.getItem('cnk_auth_token');
}
```

---

## Trạng thái session

| `status` (DB) | Ý nghĩa |
|---------------|---------|
| `None` | Chưa có session nào (GET /history trả về) |
| `BotHandling` | Bot đang xử lý, lịch sử đang lưu |
| `HumanHandoff` | Đã chuyển sang admin |
| `Closed` | Phiên đã kết thúc (admin đóng) |

| `handoffType` | Ý nghĩa |
|---------------|---------|
| `null` | Bot đang xử lý |
| `Firebase` | Admin online → chat realtime qua Firebase |
| `Pending` | Admin offline → chờ reply |
