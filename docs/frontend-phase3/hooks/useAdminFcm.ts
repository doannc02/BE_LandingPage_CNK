'use client';
// hooks/useAdminFcm.ts
// Quản lý FCM token cho admin push notification.
// Gọi hook này trong admin layout để luôn có token mới nhất.

import { useEffect, useState } from 'react';
import { getToken, onMessage } from 'firebase/messaging';
import { getFirebaseMessaging } from '@/lib/firebase';

interface UseAdminFcmReturn {
  fcmToken: string | null;
  notificationPermission: NotificationPermission;
  requestPermission: () => Promise<string | null>;
}

export function useAdminFcm(): UseAdminFcmReturn {
  const [fcmToken, setFcmToken] = useState<string | null>(null);
  const [notificationPermission, setNotificationPermission] =
    useState<NotificationPermission>('default');

  useEffect(() => {
    setNotificationPermission(Notification.permission);

    // Listen for foreground messages (when admin tab is open)
    const messaging = getFirebaseMessaging();
    if (!messaging) return;

    const unsubscribe = onMessage(messaging, (payload) => {
      console.log('[FCM] Foreground message:', payload);
      // Có thể dùng toast notification ở đây thay vì system notification
      if (payload.notification) {
        new Notification(payload.notification.title ?? 'Tin nhắn mới', {
          body: payload.notification.body,
          icon: '/logo.png',
        });
      }
    });

    return () => unsubscribe();
  }, []);

  const requestPermission = async (): Promise<string | null> => {
    try {
      const permission = await Notification.requestPermission();
      setNotificationPermission(permission);

      if (permission !== 'granted') return null;

      const messaging = getFirebaseMessaging();
      if (!messaging) return null;

      const token = await getToken(messaging, {
        vapidKey: process.env.NEXT_PUBLIC_FIREBASE_VAPID_KEY!,
      });

      setFcmToken(token);
      return token;
    } catch (err) {
      console.error('[FCM] Failed to get token:', err);
      return null;
    }
  };

  return { fcmToken, notificationPermission, requestPermission };
}
