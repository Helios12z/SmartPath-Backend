# Real-Time Chat System Implementation

## Summary of Changes

The chat system has been updated to support real-time messaging and read receipts. The following improvements have been implemented:

### 1. Enhanced SignalR Hub (MessageHub.cs)

#### New Features:
- **Auto-join chat groups**: Users are automatically joined to all their existing chats upon connection
- **User-specific notifications**: Each user joins their own notification group (`user-{userId}`)
- **Secure chat validation**: Users can only join chats they are members of
- **Bulk read functionality**: Hub method to mark all messages in a chat as read

#### Key Methods:
- `OnConnectedAsync()`: Automatically joins user to all their chats
- `JoinChat()`: Validates user permissions before joining chat groups
- `MarkMessagesRead()`: Marks all messages in a chat as read with real-time notifications

### 2. Improved MessageService (MessageService.cs)

#### Enhanced Real-time Notifications:
- **NewMessage**: Broadcasts to chat group AND sends notification to recipient
- **MessageRead**: Sends read receipt directly to the message sender
- **MessageStatusUpdated**: Updates chat group when message status changes
- **MessagesReadInChat**: Notifies senders when their messages are read in bulk

#### New Methods:
- `MarkAllAsReadAsync()`: Marks all unread messages in a chat as read

### 3. New API Endpoints (MessageController.cs)

#### Added:
- `PUT /api/message/chat/{chatId}/read-all`: Mark all messages in a chat as read

### 4. Repository Updates

#### MessageRepository.cs:
- Added `GetUnreadMessagesAsync(userId, chatId)` for chat-specific unread message retrieval

#### IMessageRepository.cs:
- Updated interface to include new repository method

## Real-time Events

Clients can now listen to these SignalR events:

### For Chat Groups (`chat-{chatId}`):
- `NewMessage`: New message in chat
- `MessageStatusUpdated`: Message read status changed

### For User Groups (`user-{userId}`):
- `NewMessageNotification`: New message notification (preview)
- `MessageRead`: Specific message read receipt
- `MessagesReadInChat`: All messages read in chat

## Event Data Formats

### NewMessage
```json
{
  "id": "guid",
  "chatId": "guid",
  "content": "string",
  "senderId": "guid",
  "senderUsername": "string",
  "isRead": false,
  "createdAt": "datetime"
}
```

### NewMessageNotification
```json
{
  "chatId": "guid",
  "messageId": "guid",
  "senderUsername": "string",
  "content": "string (truncated to 50 chars)",
  "createdAt": "datetime"
}
```

### MessageRead
```json
{
  "messageId": "guid",
  "chatId": "guid",
  "readerId": "guid",
  "readAt": "datetime"
}
```

### MessageStatusUpdated
```json
{
  "messageId": "guid",
  "isRead": true,
  "readerId": "guid"
}
```

### MessagesReadInChat
```json
{
  "chatId": "guid",
  "readerId": "guid",
  "readAt": "datetime"
}
```

## Security Improvements

1. **Authorization checks**: Users can only join chats they are members of
2. **Read validation**: Users cannot mark their own messages as read
3. **Service validation**: MessageService validates chat membership before sending messages

## Frontend Integration Guide

### Connection
```javascript
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/hubs/message", { accessTokenFactory: () => token })
    .build();
```

### Listen to Events
```javascript
// New messages in current chat
connection.on("NewMessage", (message) => {
  // Update chat UI
});

// New message notifications
connection.on("NewMessageNotification", (notification) => {
  // Update chat list with unread indicator
});

// Message read receipts
connection.on("MessageRead", (receipt) => {
  // Show read indicator
});

// Bulk read notifications
connection.on("MessagesReadInChat", (data) => {
  // Update all messages in chat as read
});
```

### Mark Messages as Read
```javascript
// Mark single message
await fetch(`/api/message/${messageId}/read`, { method: 'PUT' });

// Mark all messages in chat as read
await fetch(`/api/message/chat/${chatId}/read-all`, { method: 'PUT' });

// Or use SignalR for real-time bulk marking
connection.invoke("MarkMessagesRead", chatId);
```

## Benefits

1. **Real-time messaging**: Messages appear instantly for all chat participants
2. **Read receipts**: Senders know when their messages are read
3. **Bulk operations**: Efficiently mark multiple messages as read
4. **Security**: Proper authorization prevents unauthorized access
5. **Scalability**: Uses SignalR groups for efficient message routing
6. **Notifications**: Users receive notifications even when not actively viewing a chat