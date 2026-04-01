'use client';
// components/admin/AdminChatDashboard.tsx
// Admin dashboard để quản lý chat support.
//
// Tab 1 - "Pending": Tin nhắn để lại khi không có admin online (từ PostgreSQL API)
// Tab 2 - "Live": Chat room đang active trong Firebase (từ Firebase subscription)
//
// Cách dùng:
//   Đặt trong trang admin, truyền adminId và displayName từ auth context.
//   Yêu cầu admin đã đăng nhập và có JWT token.

import { useState, useEffect, useRef } from 'react';
import { ref, query, orderByChild, equalTo, onValue, off } from 'firebase/database';
import { getDb } from '@/lib/firebase';
import { useChatRoom } from '@/hooks/useChatRoom';
import { useAdminPresence } from '@/hooks/useAdminPresence';
import { useAdminFcm } from '@/hooks/useAdminFcm';

// ── Types ─────────────────────────────────────────────────────────────────────

interface PendingMessage {
  id: string;
  sessionId: string;
  userMessage: string;
  status: string;
  adminReply: string | null;
  createdAt: string;
  repliedAt: string | null;
}

interface LiveChatRoom {
  chatId: string;
  userId: string;
  adminId: string;
  status: string;
  userQuestion: string;
  createdAt: number;
}

// ── Constants ─────────────────────────────────────────────────────────────────

const API_BASE = process.env.NEXT_PUBLIC_API_URL ?? 'https://localhost:52040';

// ── Main Component ────────────────────────────────────────────────────────────

interface AdminChatDashboardProps {
  adminId: string;
  displayName: string;
  jwtToken: string;
}

export default function AdminChatDashboard({
  adminId,
  displayName,
  jwtToken,
}: AdminChatDashboardProps) {
  const [activeTab, setActiveTab] = useState<'pending' | 'live'>('pending');
  const [selectedPending, setSelectedPending] = useState<PendingMessage | null>(null);
  const [selectedChatId, setSelectedChatId] = useState<string | null>(null);
  const [pendingMessages, setPendingMessages] = useState<PendingMessage[]>([]);
  const [liveChats, setLiveChats] = useState<LiveChatRoom[]>([]);
  const [replyText, setReplyText] = useState('');
  const [isSending, setIsSending] = useState(false);

  // Set admin online in Firebase
  const { fcmToken, requestPermission } = useAdminFcm();
  useAdminPresence({ adminId, displayName, fcmToken: fcmToken ?? undefined });

  // Realtime messages for selected live chat
  const { messages: liveMessages, sendMessage: sendLiveMessage } =
    useChatRoom(selectedChatId);

  const messagesEndRef = useRef<HTMLDivElement>(null);

  // ── Load pending messages ──────────────────────────────────────────────────

  const fetchPendingMessages = async () => {
    try {
      const res = await fetch(`${API_BASE}/api/admin/pending-messages`, {
        headers: { Authorization: `Bearer ${jwtToken}` },
      });
      const json = await res.json();
      if (json.isSuccess) setPendingMessages(json.data ?? []);
    } catch (err) {
      console.error('Failed to load pending messages:', err);
    }
  };

  useEffect(() => {
    fetchPendingMessages();
    requestPermission();   // Request FCM permission on mount
  }, []);

  // ── Subscribe to live Firebase chats ───────────────────────────────────────

  useEffect(() => {
    const db = getDb();
    const chatsRef = ref(db, 'chats');

    const unsubscribe = onValue(chatsRef, (snapshot) => {
      if (!snapshot.exists()) return;

      const rooms: LiveChatRoom[] = [];
      snapshot.forEach((child) => {
        const meta = child.child('metadata').val();
        if (meta && meta.adminId === adminId && meta.status === 'open') {
          rooms.push({
            chatId: child.key!,
            userId: meta.userId,
            adminId: meta.adminId,
            status: meta.status,
            userQuestion: meta.userQuestion ?? '',
            createdAt: meta.createdAt ?? 0,
          });
        }
      });

      setLiveChats(rooms.sort((a, b) => b.createdAt - a.createdAt));
    });

    return () => off(chatsRef, 'value', unsubscribe);
  }, [adminId]);

  // Auto-scroll live messages
  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [liveMessages]);

  // ── Actions ────────────────────────────────────────────────────────────────

  const handleReplyPending = async () => {
    if (!selectedPending || !replyText.trim()) return;
    setIsSending(true);

    try {
      const res = await fetch(
        `${API_BASE}/api/admin/pending-messages/${selectedPending.id}/reply`,
        {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
            Authorization: `Bearer ${jwtToken}`,
          },
          body: JSON.stringify({ text: replyText.trim() }),
        },
      );

      if (res.ok) {
        setReplyText('');
        setSelectedPending(null);
        await fetchPendingMessages();
      }
    } catch (err) {
      console.error('Reply failed:', err);
    } finally {
      setIsSending(false);
    }
  };

  const handleSendLiveMessage = async () => {
    if (!selectedChatId || !replyText.trim()) return;
    await sendLiveMessage(replyText.trim(), `admin:${adminId}`);
    setReplyText('');
  };

  const handleCloseChat = async (chatId: string) => {
    await fetch(`${API_BASE}/api/admin/chats/${chatId}/close`, {
      method: 'POST',
      headers: { Authorization: `Bearer ${jwtToken}` },
    });
    if (selectedChatId === chatId) setSelectedChatId(null);
  };

  // ── Render ─────────────────────────────────────────────────────────────────

  const pendingCount = pendingMessages.filter((m) => m.status === 'Pending').length;
  const liveCount = liveChats.length;

  return (
    <div className="flex h-screen bg-gray-50">
      {/* Sidebar */}
      <aside className="w-72 flex-shrink-0 bg-white border-r border-gray-200 flex flex-col">
        {/* Header */}
        <div className="p-4 border-b border-gray-100">
          <h2 className="text-lg font-bold text-gray-800">Chat Support</h2>
          <p className="text-sm text-green-600">🟢 {displayName} đang online</p>
        </div>

        {/* Tabs */}
        <div className="flex border-b border-gray-100">
          <button
            onClick={() => setActiveTab('pending')}
            className={`flex-1 py-3 text-sm font-medium ${
              activeTab === 'pending'
                ? 'border-b-2 border-red-600 text-red-600'
                : 'text-gray-500 hover:text-gray-700'
            }`}
          >
            Pending
            {pendingCount > 0 && (
              <span className="ml-1 rounded-full bg-red-500 px-1.5 py-0.5 text-xs text-white">
                {pendingCount}
              </span>
            )}
          </button>
          <button
            onClick={() => setActiveTab('live')}
            className={`flex-1 py-3 text-sm font-medium ${
              activeTab === 'live'
                ? 'border-b-2 border-red-600 text-red-600'
                : 'text-gray-500 hover:text-gray-700'
            }`}
          >
            Live
            {liveCount > 0 && (
              <span className="ml-1 rounded-full bg-green-500 px-1.5 py-0.5 text-xs text-white">
                {liveCount}
              </span>
            )}
          </button>
        </div>

        {/* List */}
        <div className="flex-1 overflow-y-auto">
          {activeTab === 'pending' && (
            <>
              {pendingMessages.length === 0 ? (
                <p className="p-4 text-sm text-gray-400 text-center">Không có pending message</p>
              ) : (
                pendingMessages.map((msg) => (
                  <button
                    key={msg.id}
                    onClick={() => {
                      setSelectedPending(msg);
                      setSelectedChatId(null);
                    }}
                    className={`w-full text-left p-4 border-b border-gray-50 hover:bg-gray-50 transition-colors ${
                      selectedPending?.id === msg.id ? 'bg-red-50 border-l-4 border-l-red-500' : ''
                    }`}
                  >
                    <div className="flex items-start justify-between gap-2">
                      <p className="text-sm font-medium text-gray-800 line-clamp-2">
                        {msg.userMessage}
                      </p>
                      <StatusBadge status={msg.status} />
                    </div>
                    <p className="mt-1 text-xs text-gray-400">
                      {new Date(msg.createdAt).toLocaleString('vi-VN')}
                    </p>
                  </button>
                ))
              )}
            </>
          )}

          {activeTab === 'live' && (
            <>
              {liveChats.length === 0 ? (
                <p className="p-4 text-sm text-gray-400 text-center">Không có chat đang active</p>
              ) : (
                liveChats.map((chat) => (
                  <button
                    key={chat.chatId}
                    onClick={() => {
                      setSelectedChatId(chat.chatId);
                      setSelectedPending(null);
                    }}
                    className={`w-full text-left p-4 border-b border-gray-50 hover:bg-gray-50 ${
                      selectedChatId === chat.chatId
                        ? 'bg-green-50 border-l-4 border-l-green-500'
                        : ''
                    }`}
                  >
                    <div className="flex items-center justify-between">
                      <p className="text-sm font-medium text-gray-800 line-clamp-1">
                        {chat.userQuestion}
                      </p>
                      <span className="text-xs text-green-600 font-medium">Live</span>
                    </div>
                    <p className="mt-1 text-xs text-gray-400">
                      Session: {chat.userId.slice(0, 16)}...
                    </p>
                  </button>
                ))
              )}
            </>
          )}
        </div>
      </aside>

      {/* Main panel */}
      <main className="flex-1 flex flex-col">
        {/* No selection */}
        {!selectedPending && !selectedChatId && (
          <div className="flex-1 flex items-center justify-center">
            <div className="text-center text-gray-400">
              <p className="text-5xl mb-4">💬</p>
              <p className="text-lg font-medium">Chọn một cuộc hội thoại</p>
              <p className="text-sm mt-1">Pending hoặc Live chat từ sidebar</p>
            </div>
          </div>
        )}

        {/* Pending message reply panel */}
        {selectedPending && (
          <div className="flex-1 flex flex-col p-6">
            <div className="mb-4">
              <h3 className="text-lg font-bold text-gray-800">Reply Pending Message</h3>
              <p className="text-xs text-gray-400 mt-1">
                Session: {selectedPending.sessionId} •{' '}
                {new Date(selectedPending.createdAt).toLocaleString('vi-VN')}
              </p>
            </div>

            {/* Original message */}
            <div className="mb-6 rounded-xl bg-gray-100 p-4">
              <p className="text-xs text-gray-500 mb-1">Tin nhắn của user:</p>
              <p className="text-sm text-gray-800 leading-relaxed">
                {selectedPending.userMessage}
              </p>
            </div>

            {/* If already replied */}
            {selectedPending.adminReply && (
              <div className="mb-6 rounded-xl bg-green-50 border border-green-200 p-4">
                <p className="text-xs text-green-600 mb-1">✅ Đã reply:</p>
                <p className="text-sm text-gray-800">{selectedPending.adminReply}</p>
              </div>
            )}

            {/* Reply input */}
            {selectedPending.status === 'Pending' && (
              <div className="mt-auto">
                <textarea
                  value={replyText}
                  onChange={(e) => setReplyText(e.target.value)}
                  placeholder="Nhập nội dung reply..."
                  rows={4}
                  className="w-full rounded-xl border border-gray-200 p-3 text-sm outline-none focus:border-red-400 resize-none"
                />
                <button
                  onClick={handleReplyPending}
                  disabled={!replyText.trim() || isSending}
                  className="mt-3 w-full rounded-xl bg-red-600 py-3 text-sm font-semibold text-white hover:bg-red-700 disabled:opacity-50 transition-colors"
                >
                  {isSending ? 'Đang gửi...' : 'Gửi Reply'}
                </button>
                <p className="mt-2 text-xs text-gray-400 text-center">
                  Reply sẽ được gửi tới user qua Firebase notification (nếu còn online)
                </p>
              </div>
            )}
          </div>
        )}

        {/* Live chat panel */}
        {selectedChatId && (
          <div className="flex-1 flex flex-col">
            {/* Chat header */}
            <div className="flex items-center justify-between border-b border-gray-200 px-6 py-4">
              <div>
                <h3 className="font-bold text-gray-800">Live Chat</h3>
                <p className="text-xs text-gray-400">{selectedChatId}</p>
              </div>
              <button
                onClick={() => handleCloseChat(selectedChatId)}
                className="rounded-lg border border-red-200 px-3 py-1 text-sm text-red-600 hover:bg-red-50"
              >
                Đóng Chat
              </button>
            </div>

            {/* Messages */}
            <div className="flex-1 overflow-y-auto p-6 space-y-3">
              {liveMessages.map((msg) => {
                const isAdmin = msg.sender.startsWith('admin');
                return (
                  <div key={msg.id} className={`flex ${isAdmin ? 'justify-end' : 'justify-start'}`}>
                    <div
                      className={`max-w-[70%] rounded-2xl px-4 py-2 text-sm ${
                        isAdmin
                          ? 'bg-red-600 text-white rounded-br-sm'
                          : 'bg-gray-100 text-gray-800 rounded-bl-sm'
                      }`}
                    >
                      <p className="leading-relaxed">{msg.text}</p>
                      <p
                        className={`mt-1 text-xs ${
                          isAdmin ? 'text-white/70' : 'text-gray-400'
                        }`}
                      >
                        {isAdmin ? 'Bạn' : 'User'} •{' '}
                        {new Date(msg.timestamp).toLocaleTimeString('vi-VN', {
                          hour: '2-digit',
                          minute: '2-digit',
                        })}
                      </p>
                    </div>
                  </div>
                );
              })}
              <div ref={messagesEndRef} />
            </div>

            {/* Input */}
            <div className="flex items-center gap-3 border-t border-gray-200 p-4">
              <input
                value={replyText}
                onChange={(e) => setReplyText(e.target.value)}
                onKeyDown={(e) => {
                  if (e.key === 'Enter' && !e.shiftKey) {
                    e.preventDefault();
                    handleSendLiveMessage();
                  }
                }}
                placeholder="Nhập tin nhắn..."
                className="flex-1 rounded-xl border border-gray-200 px-4 py-2 text-sm outline-none focus:border-red-400"
              />
              <button
                onClick={handleSendLiveMessage}
                disabled={!replyText.trim()}
                className="rounded-xl bg-red-600 px-4 py-2 text-sm font-semibold text-white disabled:opacity-40 hover:bg-red-700"
              >
                Gửi
              </button>
            </div>
          </div>
        )}
      </main>
    </div>
  );
}

// ── Helper component ──────────────────────────────────────────────────────────

function StatusBadge({ status }: { status: string }) {
  const config: Record<string, { label: string; className: string }> = {
    Pending:  { label: 'Chờ',     className: 'bg-yellow-100 text-yellow-700' },
    Assigned: { label: 'Đang xử', className: 'bg-blue-100 text-blue-700' },
    Replied:  { label: 'Đã reply', className: 'bg-green-100 text-green-700' },
    Closed:   { label: 'Đóng',    className: 'bg-gray-100 text-gray-500' },
  };
  const { label, className } = config[status] ?? { label: status, className: 'bg-gray-100 text-gray-500' };
  return (
    <span className={`flex-shrink-0 rounded-full px-2 py-0.5 text-xs font-medium ${className}`}>
      {label}
    </span>
  );
}
