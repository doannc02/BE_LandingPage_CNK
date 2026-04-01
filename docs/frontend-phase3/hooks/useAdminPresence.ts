'use client';
// hooks/useAdminPresence.ts
// Set admin online/offline trong Firebase Realtime Database.
// Dùng onDisconnect để tự động set offline khi tab đóng / mất mạng.

import { useEffect } from 'react';
import { ref, set, onValue, onDisconnect, serverTimestamp } from 'firebase/database';
import { getDb } from '@/lib/firebase';

interface UseAdminPresenceOptions {
  adminId: string;
  displayName: string;
  fcmToken?: string;   // Lấy từ useAdminFcm hook, truyền vào đây để lưu vào Firebase
  enabled?: boolean;   // Set false để disable (e.g. khi admin logout)
}

export function useAdminPresence({
  adminId,
  displayName,
  fcmToken,
  enabled = true,
}: UseAdminPresenceOptions): void {
  useEffect(() => {
    if (!adminId || !enabled) return;

    const db = getDb();
    const presenceRef = ref(db, `presence/admins/${adminId}`);
    const connectedRef = ref(db, '.info/connected');

    // Lắng nghe trạng thái kết nối Firebase
    const unsubscribe = onValue(connectedRef, (snap) => {
      if (!snap.val()) return;   // Chưa kết nối

      // Khi mất kết nối → Firebase tự động set offline
      onDisconnect(presenceRef).set({
        online: false,
        lastSeen: serverTimestamp(),
        displayName,
        fcmToken: fcmToken ?? null,
      });

      // Set online ngay lập tức
      set(presenceRef, {
        online: true,
        lastSeen: serverTimestamp(),
        displayName,
        fcmToken: fcmToken ?? null,
      });
    });

    return () => {
      // Set offline manually khi component unmount (admin logout)
      set(presenceRef, {
        online: false,
        lastSeen: serverTimestamp(),
        displayName,
        fcmToken: fcmToken ?? null,
      });
      unsubscribe();
    };
  }, [adminId, displayName, fcmToken, enabled]);
}
