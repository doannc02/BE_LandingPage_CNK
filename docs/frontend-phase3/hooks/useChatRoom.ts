'use client';
// hooks/useChatRoom.ts
// Subscribe realtime messages trong một Firebase chat room.
// Dùng cho cả user (bubble chat) và admin (dashboard).

import { useEffect, useRef, useState, useCallback } from 'react';
import { ref, onChildAdded, push, serverTimestamp, off } from 'firebase/database';
import { getDb } from '@/lib/firebase';

export interface ChatMessage {
  id: string;
  sender: string;       // "user" | "admin" | "admin:adminId"
  text: string;
  timestamp: number;
  read: boolean;
}

interface UseChatRoomReturn {
  messages: ChatMessage[];
  sendMessage: (text: string, sender: string) => Promise<void>;
  isConnected: boolean;
}

export function useChatRoom(chatId: string | null): UseChatRoomReturn {
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [isConnected, setIsConnected] = useState(false);
  const seenIds = useRef<Set<string>>(new Set());

  useEffect(() => {
    if (!chatId) return;

    const db = getDb();
    const messagesRef = ref(db, `chats/${chatId}/messages`);
    setIsConnected(true);

    // onChildAdded fires once per existing child, then streams new ones
    const unsubscribe = onChildAdded(messagesRef, (snapshot) => {
      const key = snapshot.key!;
      if (seenIds.current.has(key)) return;   // deduplicate on hot-reload
      seenIds.current.add(key);

      const data = snapshot.val();
      setMessages((prev) => [
        ...prev,
        {
          id: key,
          sender: data.sender ?? 'unknown',
          text: data.text ?? '',
          timestamp: data.timestamp ?? Date.now(),
          read: data.read ?? false,
        },
      ]);
    });

    return () => {
      off(messagesRef, 'child_added', unsubscribe);
      setIsConnected(false);
    };
  }, [chatId]);

  const sendMessage = useCallback(
    async (text: string, sender: string) => {
      if (!chatId || !text.trim()) return;

      const db = getDb();
      const messagesRef = ref(db, `chats/${chatId}/messages`);
      await push(messagesRef, {
        sender,
        text: text.trim(),
        timestamp: serverTimestamp(),
        read: false,
      });
    },
    [chatId],
  );

  return { messages, sendMessage, isConnected };
}
