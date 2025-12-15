# Frontend Real-Time Chat Implementation Guide

## Overview

This guide explains what frontend developers need to do to implement real-time messaging with the updated SignalR chat backend. The backend now supports real-time messaging, read receipts, and notifications.

## Prerequisites

1. Install SignalR client library:
```bash
npm install @microsoft/signalr
# or
yarn add @microsoft/signalr
```

2. Ensure you have access to the chat and message APIs

## 1. Create/Update SignalR Client Utilities

### File: `lib/signalrClient.ts`

```typescript
import * as signalR from '@microsoft/signalr';

export function getHubConnection(url: string, tokenGetter: () => string | null) {
  return new signalR.HubConnectionBuilder()
    .withUrl(url, {
      accessTokenFactory: () => tokenGetter() || '',
    })
    .withAutomaticReconnect()
    .build();
}

// Interface for all chat hub events
interface ChatHubHandlers {
  onNewMessage?: (m: any) => void;
  onMessageRead?: (e: any) => void;
  onMessageStatusUpdated?: (e: any) => void;
  onNewMessageNotification?: (n: any) => void;
  onMessagesReadInChat?: (e: any) => void;
}

export function setHandlersOnce(
  conn: signalR.HubConnection,
  handlers: ChatHubHandlers
) {
  // Remove existing handlers to prevent duplicates
  conn.off('NewMessage');
  conn.off('MessageRead');
  conn.off('MessageStatusUpdated');
  conn.off('NewMessageNotification');
  conn.off('MessagesReadInChat');

  // Register new handlers
  if (handlers.onNewMessage) {
    conn.on('NewMessage', handlers.onNewMessage);
  }
  if (handlers.onMessageRead) {
    conn.on('MessageRead', handlers.onMessageRead);
  }
  if (handlers.onMessageStatusUpdated) {
    conn.on('MessageStatusUpdated', handlers.onMessageStatusUpdated);
  }
  if (handlers.onNewMessageNotification) {
    conn.on('NewMessageNotification', handlers.onNewMessageNotification);
  }
  if (handlers.onMessagesReadInChat) {
    conn.on('MessagesReadInChat', handlers.onMessagesReadInChat);
  }
}

export async function ensureStarted(conn: signalR.HubConnection) {
  if (conn.state === signalR.HubConnectionState.Disconnected) {
    await conn.start();
  }
}

export async function invokeSafe<T = any>(conn: signalR.HubConnection, method: string, ...args: any[]): Promise<T> {
  try {
    return await conn.invoke(method, ...args);
  } catch (e) {
    console.error(`[SignalR] ${method} failed`, e);
    throw e;
  }
}

export function stopOnUnload(conn: signalR.HubConnection) {
  // Don't stop on page navigation for better UX
  // conn.stop();
}
```

## 2. Create Chat Hook

### File: `hooks/use-chat.ts`

```typescript
'use client';
import { useEffect, useRef, useState, useCallback } from 'react';
import { getHubConnection, ensureStarted, setHandlersOnce, invokeSafe } from '@/lib/signalrClient';
import { useAccessToken } from './use-access-token';

export function useChatHub(opts?: {
  hubUrl?: string;
  onNewMessage?: (m: any) => void;
  onMessageRead?: (e: any) => void;
  onMessageStatusUpdated?: (e: any) => void;
  onNewMessageNotification?: (n: any) => void;
  onMessagesReadInChat?: (e: any) => void;
  selectedChatId?: string | undefined;
}) {
  const {
    hubUrl = process.env.NEXT_PUBLIC_HUB_URL ?? '',
    onNewMessage,
    onMessageRead,
    onMessageStatusUpdated,
    onNewMessageNotification,
    onMessagesReadInChat,
    selectedChatId
  } = opts || {};

  const { tokenRef } = useAccessToken();
  const [connected, setConnected] = useState(false);
  const connRef = useRef<signalR.HubConnection | null>(null);
  const currentChatRef = useRef<string | undefined>(undefined);
  const lifecycleSetRef = useRef(false);

  const tokenGetter = useCallback(() => tokenRef.current, [tokenRef]);

  useEffect(() => {
    if (!hubUrl) return;

    const conn = getHubConnection(hubUrl, tokenGetter);
    connRef.current = conn;

    setHandlersOnce(conn, {
      onNewMessage,
      onMessageRead,
      onMessageStatusUpdated,
      onNewMessageNotification,
      onMessagesReadInChat
    });

    ensureStarted(conn)
      .then(async () => {
        setConnected(true);
        stopOnUnload(conn);

        if (currentChatRef.current) {
          try {
            await invokeSafe(conn, 'JoinChat', currentChatRef.current);
          } catch (e) {
            console.error('[SignalR] JoinChat after start failed', e);
          }
        }

        if (!lifecycleSetRef.current) {
          (conn as any).onreconnected?.(async () => {
            try {
              if (currentChatRef.current) {
                await invokeSafe(conn, 'JoinChat', currentChatRef.current);
              }
              setConnected(true);
            } catch (e) {
              console.error('[SignalR] rejoin after reconnect failed', e);
            }
          });

          (conn as any).onreconnecting?.(() => setConnected(false));
          (conn as any).onclose?.(() => setConnected(false));
          lifecycleSetRef.current = true;
        }
      })
      .catch(err => console.error('[SignalR] start failed', err));

    return () => {
      // Don't stop for better UX across navigation
    };
  }, [hubUrl, tokenGetter, onNewMessage, onMessageRead, onMessageStatusUpdated, onNewMessageNotification, onMessagesReadInChat]);

  useEffect(() => {
    if (!connRef.current) return;
    const conn = connRef.current;
    const prev = currentChatRef.current;
    const next = selectedChatId;

    (async () => {
      try {
        if (prev && prev !== next) {
          await invokeSafe(conn, 'LeaveChat', prev);
        }
        if (next) {
          await invokeSafe(conn, 'JoinChat', next);
        }
        currentChatRef.current = next;
      } catch (e) {
        console.error('Join/Leave failed', e);
      }
    })();
  }, [selectedChatId]);

  const markMessagesRead = useCallback(async (chatId: string) => {
    if (!connRef.current) return;
    try {
      await invokeSafe(connRef.current, 'MarkMessagesRead', chatId);
    } catch (e) {
      console.error('MarkMessagesRead failed', e);
    }
  }, []);

  return {
    connected,
    markMessagesRead,
    connection: connRef.current
  };
}
```

## 3. Update Message API

### File: `lib/api/messageAPI.ts`

```typescript
import { api } from './api';

export const messageAPI = {
  // Existing methods...
  send: async (data: { chat_id: string; content: string }) => {
    return await api.post('/message', data);
  },

  markRead: async (messageId: string) => {
    return await api.put(`/message/${messageId}/read`);
  },

  // NEW: Bulk read operation
  markAllRead: async (chatId: string) => {
    return await api.put(`/message/chat/${chatId}/read-all`);
  },

  getByChat: async (chatId: string) => {
    return await api.get(`/message/by-chat/${chatId}`);
  }
};
```

## 4. Update Chat Component/Page

### Key Implementation Steps:

#### 1. Use the Chat Hook

```typescript
const { connected, markMessagesRead } = useChatHub({
  selectedChatId,
  onNewMessage: (message) => {
    // Handle new message
    // Update messages array
    // Auto-mark as read if needed
  },
  onMessageRead: (event) => {
    // Handle message read receipt
    // Update message status
  },
  onMessageStatusUpdated: (event) => {
    // Handle bulk status updates
  },
  onNewMessageNotification: (notification) => {
    // Update chat list with new message indicator
  },
  onMessagesReadInChat: (event) => {
    // Handle bulk read operation
  }
});
```

#### 2. Auto-Mark Messages as Read

```typescript
useEffect(() => {
  if (!selectedChat || !currentUser?.id) return;
  const unread = (selectedChat.messages ?? [])
    .filter(m => !m.isRead && m.senderId !== currentUser.id);

  if (unread.length > 0) {
    // Use bulk read for efficiency
    messageAPI.markAllRead(selectedChatId).catch(() => {});
  }
}, [selectedChat, currentUser?.id]);
```

#### 3. Handle Message Sending

```typescript
const handleSendMessage = async () => {
  if (!messageInput.trim() || !selectedChatId) return;

  try {
    await messageAPI.send({
      chat_id: selectedChatId,
      content: messageInput.trim()
    });
    setMessageInput('');

    // Message will be received via SignalR
    // Optionally add optimistic update
  } catch (e) {
    console.error(e);
  }
};
```

## 5. Real-Time Events to Handle

### Event Types and Data:

#### NewMessage
```typescript
{
  id: string,
  chatId: string,
  content: string,
  senderId: string,
  senderUsername: string,
  isRead: boolean,
  createdAt: string
}
```

#### MessageRead
```typescript
{
  messageId: string,
  chatId: string,
  readerId: string,
  readAt: string
}
```

#### MessageStatusUpdated
```typescript
{
  messageId: string,
  isRead: boolean,
  readerId: string
}
```

#### NewMessageNotification
```typescript
{
  chatId: string,
  messageId: string,
  senderUsername: string,
  content: string, // Truncated preview
  createdAt: string
}
```

#### MessagesReadInChat
```typescript
{
  chatId: string,
  readerId: string,
  readAt: string
}
```

## 6. UI Updates Required

### Chat List Updates
- Show unread message count badges
- Update last message preview
- Show "New" indicator for unread chats
- Move active chat to top when new message arrives

### Message List Updates
- Show "Read" status for own messages
- Real-time message appearance
- Smooth animations for new messages
- Typing indicators (if implemented)

### Connection Status
- Show "Connected" / "Connecting..." / "Disconnected" status
- Retry failed messages automatically
- Show offline indicator

## 7. Best Practices

### Performance
- Use pagination for message history
- Implement message caching
- Debounce message typing indicators
- Use React.memo for message components

### User Experience
- Add optimistic updates for sent messages
- Show loading states
- Handle network failures gracefully
- Store unsent messages locally

### Security
- Always validate messages on backend (frontend is just for display)
- Sanitize message content to prevent XSS
- Check user permissions before joining chat groups

### Error Handling
- Reconnect automatically on disconnection
- Queue messages when offline
- Show clear error messages to users
- Implement retry logic with exponential backoff

## 8. Testing

### Manual Testing Checklist
- [ ] Messages appear in real-time
- [ ] Read receipts work correctly
- [ ] Chat list updates with new messages
- [ ] Bulk read operations work
- [ ] Connection handles page refreshes
- [ ] Works across multiple tabs
- [ ] Handles network disconnections
- [ ] Messages persist after refresh

### Debugging Tips
- Check browser console for SignalR logs
- Monitor Network tab for WebSocket connection
- Verify JWT token is sent correctly
- Check hub URL configuration

## 9. Environment Configuration

### Environment Variables
```env
NEXT_PUBLIC_HUB_URL=http://localhost:5000/hubs/message  # Development
NEXT_PUBLIC_HUB_URL=https://api.yourdomain.com/hubs/message  # Production
```

### CORS Configuration
Ensure your SignalR hub URL is included in CORS configuration on the backend.

## 10. Additional Features (Optional)

### Typing Indicators
Backend support needed for:
- `UserTyping` event
- `UserStoppedTyping` event
- Debounced typing detection

### Online Status
- Show "Online" / "Offline" status
- Last seen timestamps
- Presence indicators

### Push Notifications
- Browser notifications for new messages
- Service worker integration
- Permission handling

## 11. Troubleshooting

### Common Issues

#### Connection Fails
- Check JWT token is valid
- Verify hub URL is correct
- Check CORS settings
- Ensure SignalR port is open

#### Messages Not Real-Time
- Verify SignalR connection is established
- Check event handlers are registered
- Verify chat groups are joined
- Check browser console for errors

#### Read Receipts Not Working
- Ensure `markAllRead` is called when opening chat
- Check `MessageRead` event handler
- Verify user permissions
- Check message IDs match

### Debug Mode
Add console logging to track events:

```typescript
connection.on('NewMessage', (message) => {
  console.log('New message received:', message);
  // Handle message
});
```

## 12. Migration from Existing Implementation

If you have an existing chat implementation:

1. **Backup your current code**
2. **Update SignalR client** to handle new events
3. **Add new API endpoints** to your API client
4. **Update UI components** to show read status
5. **Test thoroughly** before deploying

### Breaking Changes
- New event names (`MessageStatusUpdated`, `MessagesReadInChat`)
- New API endpoint (`PUT /message/chat/{id}/read-all`)
- Different event data structure for some events