// public/firebase-messaging-sw.js
// Service Worker xử lý FCM push notification khi admin không mở tab.
// File NÀY PHẢI đặt tại public/firebase-messaging-sw.js (root của NextJS public/)

importScripts('https://www.gstatic.com/firebasejs/10.12.0/firebase-app-compat.js');
importScripts('https://www.gstatic.com/firebasejs/10.12.0/firebase-messaging-compat.js');

// Điền config giống trong firebase.ts (không dùng env vars trong SW)
firebase.initializeApp({
  apiKey:            'PASTE_YOUR_API_KEY',
  authDomain:        'PASTE_YOUR_AUTH_DOMAIN',
  databaseURL:       'PASTE_YOUR_DATABASE_URL',
  projectId:         'PASTE_YOUR_PROJECT_ID',
  storageBucket:     'PASTE_YOUR_STORAGE_BUCKET',
  messagingSenderId: 'PASTE_YOUR_MESSAGING_SENDER_ID',
  appId:             'PASTE_YOUR_APP_ID',
});

const messaging = firebase.messaging();

// Xử lý push notification khi tab đóng / background
messaging.onBackgroundMessage((payload) => {
  console.log('[SW] Background FCM:', payload);

  const { title = 'Tin nhắn mới', body = '' } = payload.notification ?? {};
  const { url = '/admin/messages', pendingMessageId = '' } = payload.data ?? {};

  self.registration.showNotification(title, {
    body,
    icon:  '/logo.png',
    badge: '/badge.png',
    tag:   `cnk-chat-${pendingMessageId}`,   // deduplicate cùng message
    data:  { url },
    actions: [
      { action: 'open',    title: '📩 Xem ngay' },
      { action: 'dismiss', title: 'Bỏ qua' },
    ],
  });
});

// Click notification → mở tab admin dashboard
self.addEventListener('notificationclick', (event) => {
  event.notification.close();

  if (event.action === 'dismiss') return;

  const targetUrl = event.notification.data?.url ?? '/admin/messages';

  event.waitUntil(
    clients
      .matchAll({ type: 'window', includeUncontrolled: true })
      .then((windowClients) => {
        // Nếu đã có tab admin mở → focus lại
        const existing = windowClients.find((c) => c.url.includes('/admin'));
        if (existing) {
          existing.focus();
          existing.navigate(targetUrl);
          return;
        }
        // Không có tab → mở tab mới
        clients.openWindow(targetUrl);
      }),
  );
});
