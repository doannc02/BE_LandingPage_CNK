'use client';
// hooks/useUserNotification.ts
// Lắng nghe admin reply khi user đã để lại message (LeftMessage flow).
// Firebase path: /notifications/{sessionId}

import { useEffect, useState } from 'react';
import { ref, onValue, off } from 'firebase/database';
import { getDb } from '@/lib/firebase';

export interface AdminReplyNotification {
  reply: string;
  adminId: string;
  repliedAt: number;
  read: boolean;
}

export function useUserNotification(sessionId: string | null) {
  const [notification, setNotification] = useState<AdminReplyNotification | null>(null);

  useEffect(() => {
    if (!sessionId) return;

    const db = getDb();
    const notifRef = ref(db, `notifications/${sessionId}`);

    const unsubscribe = onValue(notifRef, (snap) => {
      if (!snap.exists()) return;
      const data = snap.val();
      if (data?.reply) setNotification(data as AdminReplyNotification);
    });

    return () => off(notifRef, 'value', unsubscribe);
  }, [sessionId]);

  return notification;
}
