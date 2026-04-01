# Frontend Phase 3 — Setup Guide

Copy các file trong thư mục này vào NextJS project theo cấu trúc:

```
your-nextjs-project/
├── src/
│   ├── lib/
│   │   └── firebase.ts               ← từ lib/firebase.ts
│   ├── hooks/
│   │   ├── useChatRoom.ts            ← từ hooks/useChatRoom.ts
│   │   ├── useAdminPresence.ts       ← từ hooks/useAdminPresence.ts
│   │   ├── useAdminFcm.ts            ← từ hooks/useAdminFcm.ts
│   │   └── useUserNotification.ts   ← từ hooks/useUserNotification.ts
│   └── components/
│       ├── chat/
│       │   └── BubbleChat.tsx        ← từ components/chat/BubbleChat.tsx
│       └── admin/
│           └── AdminChatDashboard.tsx ← từ components/admin/AdminChatDashboard.tsx
└── public/
    └── firebase-messaging-sw.js      ← từ public/firebase-messaging-sw.js
```

## 1. Cài Firebase SDK

```bash
npm install firebase
```

## 2. Tạo .env.local

```env
NEXT_PUBLIC_FIREBASE_API_KEY=...
NEXT_PUBLIC_FIREBASE_AUTH_DOMAIN=...
NEXT_PUBLIC_FIREBASE_DATABASE_URL=https://your-project-default-rtdb.firebaseio.com
NEXT_PUBLIC_FIREBASE_PROJECT_ID=...
NEXT_PUBLIC_FIREBASE_STORAGE_BUCKET=...
NEXT_PUBLIC_FIREBASE_MESSAGING_SENDER_ID=...
NEXT_PUBLIC_FIREBASE_APP_ID=...
NEXT_PUBLIC_FIREBASE_VAPID_KEY=...   # Firebase Console → Cloud Messaging → Web Push certificates

NEXT_PUBLIC_API_URL=https://your-backend-domain.com
```

## 3. Điền config vào firebase-messaging-sw.js

File `public/firebase-messaging-sw.js` KHÔNG dùng được env vars.
Paste trực tiếp Firebase config vào file đó.

## 4. Thêm BubbleChat vào layout user

```tsx
// app/layout.tsx (hoặc pages/_app.tsx)
import BubbleChat from '@/components/chat/BubbleChat';

export default function RootLayout({ children }) {
  return (
    <html>
      <body>
        {children}
        <BubbleChat />   {/* Thêm vào đây */}
      </body>
    </html>
  );
}
```

## 5. Thêm AdminChatDashboard vào trang admin

```tsx
// app/admin/chat/page.tsx
'use client';
import AdminChatDashboard from '@/components/admin/AdminChatDashboard';
import { useAuth } from '@/hooks/useAuth';  // hook auth của bạn

export default function AdminChatPage() {
  const { user, token } = useAuth();   // lấy từ auth context
  if (!user) return null;

  return (
    <AdminChatDashboard
      adminId={user.id}
      displayName={user.name}
      jwtToken={token}
    />
  );
}
```

## 6. Firebase Realtime Database Rules

Vào Firebase Console → Realtime Database → Rules, set:

```json
{
  "rules": {
    "chats": {
      "$chatId": {
        ".read": true,
        ".write": true
      }
    },
    "presence": {
      "admins": {
        "$adminId": {
          ".read": true,
          ".write": true
        }
      }
    },
    "notifications": {
      "$sessionId": {
        ".read": true,
        ".write": true
      }
    }
  }
}
```

> **Production:** Thêm auth rules sau khi đã test xong.

## 7. Kiểm tra end-to-end

1. Mở trang web user → Click chat bubble → Nhập câu hỏi
2. Nếu AI đủ confidence → Thấy answer ngay (type: AI)
3. Nếu AI không đủ → Backend check presence
   - Admin online (trong dashboard) → type: HumanOnline → Chat realtime mở
   - Admin offline → type: LeftMessage → User thấy "Sẽ phản hồi sớm"
4. Admin đăng nhập dashboard → Thấy pending message → Click reply → User thấy reply ngay

## Firebase Database Structure

```
/chats/{chatId}/
  metadata/
    userId: "sess_123"
    adminId: "admin_456"
    status: "open" | "closed"
    userQuestion: "..."
    createdAt: 1711900000000

  messages/{msgId}/
    sender: "user" | "admin:adminId"
    text: "..."
    timestamp: 1711900000000
    read: false

/presence/admins/{adminId}/
  online: true
  lastSeen: 1711900000000
  displayName: "Admin Chất"
  fcmToken: "FCM_TOKEN"

/notifications/{sessionId}/
  reply: "Admin reply text"
  adminId: "admin_456"
  repliedAt: 1711900000000
  read: false
```
