'use client';
// components/chat/BubbleChat.tsx
// Chat bubble widget hiển thị ở góc dưới-phải trang web cho user.
//
// Flow:
//   User nhấn nút → gõ message → POST /api/v1/chat/message
//     type: "AI"          → hiển thị answer ngay lập tức
//     type: "HumanOnline" → kết nối realtime Firebase chat room
//     type: "LeftMessage" → thông báo "Chúng tôi sẽ phản hồi sớm"
//
// Cài đặt:
//   npm install firebase
//   Thêm env vars từ firebase.ts

import { useState, useRef, useEffect, useCallback } from 'react';
import { useChatRoom } from '@/hooks/useChatRoom';
import { useUserNotification } from '@/hooks/useUserNotification';

// ── Types ─────────────────────────────────────────────────────────────────────

type ChatState = 'idle' | 'loading' | 'ai' | 'human-online' | 'left-message';

interface DisplayMessage {
  id: string;
  role: 'user' | 'assistant' | 'system';
  text: string;
  timestamp: number;
}

interface HistoryItem {
  role: 'user' | 'assistant';
  content: string;
}

// ── Constants ─────────────────────────────────────────────────────────────────

const API_BASE = process.env.NEXT_PUBLIC_API_URL ?? 'https://localhost:52040';

// Stable session ID per browser (localStorage)
function getSessionId(): string {
  if (typeof window === 'undefined') return 'ssr';
  let id = localStorage.getItem('cnk_session_id');
  if (!id) {
    id = `sess_${Date.now()}_${Math.random().toString(36).slice(2, 9)}`;
    localStorage.setItem('cnk_session_id', id);
  }
  return id;
}

// ── Main Component ────────────────────────────────────────────────────────────

export default function BubbleChat() {
  const [isOpen, setIsOpen] = useState(false);
  const [input, setInput] = useState('');
  const [chatState, setChatState] = useState<ChatState>('idle');
  const [chatRoomId, setChatRoomId] = useState<string | null>(null);
  const [displayMessages, setDisplayMessages] = useState<DisplayMessage[]>([]);
  const [history, setHistory] = useState<HistoryItem[]>([]);
  const [hasUnread, setHasUnread] = useState(false);

  const sessionId = useRef(getSessionId());
  const messagesEndRef = useRef<HTMLDivElement>(null);

  // Realtime Firebase chat (only active when chatRoomId is set)
  const { messages: firebaseMessages, sendMessage: sendFirebaseMessage } =
    useChatRoom(chatRoomId);

  // Listen for admin reply when user left a message
  const adminReply = useUserNotification(
    chatState === 'left-message' ? sessionId.current : null,
  );

  // Sync Firebase messages into displayMessages
  useEffect(() => {
    if (!chatRoomId || firebaseMessages.length === 0) return;

    setDisplayMessages((prev) => {
      const existingIds = new Set(prev.map((m) => m.id));
      const newMsgs = firebaseMessages
        .filter((m) => !existingIds.has(m.id))
        .map((m) => ({
          id: m.id,
          role: m.sender === 'user' ? ('user' as const) : ('assistant' as const),
          text: m.text,
          timestamp: m.timestamp,
        }));

      if (newMsgs.length === 0) return prev;

      // Unread badge if chat is closed
      if (!isOpen) setHasUnread(true);
      return [...prev, ...newMsgs];
    });
  }, [firebaseMessages, chatRoomId, isOpen]);

  // Show admin reply notification (LeftMessage → reply arrived)
  useEffect(() => {
    if (!adminReply) return;
    addMessage('assistant', adminReply.reply);
    setChatState('ai');
    if (!isOpen) setHasUnread(true);
  }, [adminReply]);

  // Auto-scroll to latest message
  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [displayMessages]);

  // Clear unread when user opens chat
  useEffect(() => {
    if (isOpen) setHasUnread(false);
  }, [isOpen]);

  // ── Helpers ─────────────────────────────────────────────────────────────────

  const addMessage = useCallback((role: DisplayMessage['role'], text: string) => {
    setDisplayMessages((prev) => [
      ...prev,
      { id: `local_${Date.now()}`, role, text, timestamp: Date.now() },
    ]);
  }, []);

  // ── Send message ─────────────────────────────────────────────────────────────

  const handleSend = async () => {
    const text = input.trim();
    if (!text || chatState === 'loading') return;

    setInput('');
    addMessage('user', text);
    const currentHistory = [...history];

    // If already in human-online (Firebase) mode → send directly to Firebase
    if (chatState === 'human-online' && chatRoomId) {
      await sendFirebaseMessage(text, 'user');
      return;
    }

    // Otherwise → call backend classify endpoint
    setChatState('loading');

    try {
      const res = await fetch(`${API_BASE}/api/v1/chat/message`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          sessionId: sessionId.current,
          message: text,
          history: currentHistory,
        }),
      });

      if (!res.ok) throw new Error(`HTTP ${res.status}`);

      const json = await res.json();
      const data = json.data as {
        type: string;
        answer?: string;
        chatRoomId?: string;
        messageId?: string;
      };

      if (data.type === 'AI') {
        addMessage('assistant', data.answer ?? '');
        setHistory((h) => [
          ...h,
          { role: 'user', content: text },
          { role: 'assistant', content: data.answer ?? '' },
        ]);
        setChatState('ai');
      } else if (data.type === 'HumanOnline') {
        setChatRoomId(data.chatRoomId!);
        setChatState('human-online');
        addMessage('system', '✅ Đã kết nối với hỗ trợ viên. Bạn có thể tiếp tục chat!');
      } else if (data.type === 'LeftMessage') {
        setChatState('left-message');
        addMessage(
          'system',
          '📩 Tin nhắn của bạn đã được ghi nhận. Hỗ trợ viên sẽ phản hồi sớm nhất có thể. Bạn sẽ thấy phản hồi ngay tại đây!',
        );
      }
    } catch {
      setChatState('ai');
      addMessage(
        'system',
        '⚠️ Đã xảy ra lỗi kết nối. Vui lòng thử lại hoặc gọi hotline 0868.699.860',
      );
    }
  };

  const handleKeyDown = (e: React.KeyboardEvent<HTMLInputElement>) => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault();
      handleSend();
    }
  };

  // ── Render ───────────────────────────────────────────────────────────────────

  return (
    <>
      {/* Floating button */}
      <button
        onClick={() => setIsOpen((o) => !o)}
        className="fixed bottom-6 right-6 z-50 flex h-14 w-14 items-center justify-center rounded-full bg-red-600 shadow-lg transition-transform hover:scale-110"
        aria-label="Chat hỗ trợ"
      >
        {isOpen ? (
          <span className="text-white text-xl">✕</span>
        ) : (
          <>
            <span className="text-white text-2xl">💬</span>
            {hasUnread && (
              <span className="absolute -top-1 -right-1 h-4 w-4 rounded-full bg-yellow-400 border-2 border-white" />
            )}
          </>
        )}
      </button>

      {/* Chat window */}
      {isOpen && (
        <div className="fixed bottom-24 right-6 z-50 flex w-80 flex-col rounded-2xl shadow-2xl bg-white overflow-hidden border border-gray-200 sm:w-96">
          {/* Header */}
          <div className="flex items-center gap-3 bg-red-600 px-4 py-3 text-white">
            <div className="flex h-9 w-9 items-center justify-center rounded-full bg-white/20 text-lg">
              🥋
            </div>
            <div className="flex-1">
              <p className="text-sm font-semibold">Hỗ trợ CLB Côn Nhị Khúc</p>
              <p className="text-xs text-white/80">
                {chatState === 'human-online' ? '🟢 Đang kết nối với hỗ trợ viên' : '🤖 Trợ lý AI'}
              </p>
            </div>
          </div>

          {/* Messages */}
          <div className="flex-1 overflow-y-auto p-4 space-y-3 max-h-80">
            {displayMessages.length === 0 && (
              <p className="text-center text-sm text-gray-400 mt-8">
                Xin chào! Tôi có thể giúp gì cho bạn về Võ đường Côn Nhị Khúc Hà Đông? 🥋
              </p>
            )}

            {displayMessages.map((msg) => (
              <ChatBubble key={msg.id} message={msg} />
            ))}

            {chatState === 'loading' && (
              <div className="flex justify-start">
                <div className="rounded-2xl bg-gray-100 px-4 py-2">
                  <span className="flex gap-1">
                    <span className="animate-bounce text-gray-400" style={{ animationDelay: '0ms' }}>●</span>
                    <span className="animate-bounce text-gray-400" style={{ animationDelay: '150ms' }}>●</span>
                    <span className="animate-bounce text-gray-400" style={{ animationDelay: '300ms' }}>●</span>
                  </span>
                </div>
              </div>
            )}
            <div ref={messagesEndRef} />
          </div>

          {/* Input */}
          <div className="flex items-center gap-2 border-t border-gray-100 p-3">
            <input
              value={input}
              onChange={(e) => setInput(e.target.value)}
              onKeyDown={handleKeyDown}
              placeholder={
                chatState === 'left-message'
                  ? 'Đang chờ phản hồi...'
                  : 'Nhập câu hỏi của bạn...'
              }
              disabled={chatState === 'loading' || chatState === 'left-message'}
              className="flex-1 rounded-xl border border-gray-200 px-3 py-2 text-sm outline-none focus:border-red-400 disabled:bg-gray-50 disabled:text-gray-400"
            />
            <button
              onClick={handleSend}
              disabled={!input.trim() || chatState === 'loading' || chatState === 'left-message'}
              className="flex h-9 w-9 items-center justify-center rounded-xl bg-red-600 text-white disabled:opacity-40 hover:bg-red-700 transition-colors"
            >
              ➤
            </button>
          </div>
        </div>
      )}
    </>
  );
}

// ── Sub-component ─────────────────────────────────────────────────────────────

function ChatBubble({ message }: { message: DisplayMessage }) {
  if (message.role === 'system') {
    return (
      <div className="flex justify-center">
        <p className="rounded-xl bg-blue-50 px-3 py-2 text-xs text-blue-600 text-center max-w-[90%]">
          {message.text}
        </p>
      </div>
    );
  }

  const isUser = message.role === 'user';
  return (
    <div className={`flex ${isUser ? 'justify-end' : 'justify-start'}`}>
      <div
        className={`max-w-[80%] rounded-2xl px-4 py-2 text-sm leading-relaxed whitespace-pre-wrap ${
          isUser
            ? 'bg-red-600 text-white rounded-br-sm'
            : 'bg-gray-100 text-gray-800 rounded-bl-sm'
        }`}
      >
        {message.text}
      </div>
    </div>
  );
}
