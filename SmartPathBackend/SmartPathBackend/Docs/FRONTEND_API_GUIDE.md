# Frontend API Implementation Guide

## Overview

This guide provides comprehensive documentation for frontend developers to implement all available features of the SmartPath backend API, including the newly implemented search engine, study materials management, and existing features.

## Base URL

```
Development: http://localhost:5000/api
Production: https://api.yourdomain.com/api
```

## Authentication

All protected endpoints require a JWT Bearer token in the Authorization header:

```http
Authorization: Bearer <your-jwt-token>
```

## 1. Authentication & User Management

### 1.1 Register User

```http
POST /auth/register
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "password123",
  "username": "username",
  "fullName": "Full Name",
  "phoneNumber": "optional-phone",
  "major": "Computer Science",
  "faculty": "Engineering",
  "yearOfStudy": 3,
  "bio": "Optional bio"
}
```

**Response:**
```json
{
  "token": "jwt-token-string",
  "expiresIn": "7d",
  "user": {
    "id": "user-id",
    "email": "user@example.com",
    "username": "username",
    "fullName": "Full Name",
    "role": "Student"
  }
}
```

### 1.2 Login

```http
POST /auth/login
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "password123"
}
```

### 1.3 Get Current User

```http
GET /auth/me
Authorization: Bearer <token>
```

### 1.4 Update Profile

```http
PUT /auth/profile
Authorization: Bearer <token>
Content-Type: application/json

{
  "fullName": "Updated Name",
  "bio": "Updated bio",
  "major": "Updated Major"
}
```

## 2. Search Engine

### 2.1 Comprehensive Search

```http
POST /search/search
Authorization: Bearer <token>
Content-Type: application/json

{
  "query": "search query",
  "searchType": "All", // "All", "Posts", "StudyMaterials"
  "categoryIds": ["guid-1", "guid-2"],
  "materialCategoryIds": ["guid-1"],
  "isQuestion": null, // true, false, or null for both
  "includeSemanticSearch": true,
  "includeKeywordSearch": true,
  "sortBy": "relevance", // "relevance", "created", "updated", "views", "likes", "rating"
  "sortOrder": "desc", // "asc", "desc"
  "page": 1,
  "pageSize": 20,
  "fromDate": "2024-01-01T00:00:00Z",
  "toDate": "2024-12-31T23:59:59Z",
  "tags": ["tag1", "tag2"]
}
```

**Response:**
```json
{
  "posts": [
    {
      "id": "post-id",
      "title": "Post Title",
      "content": "Post content preview...",
      "summary": "Summary if available",
      "isQuestion": false,
      "isSolved": false,
      "viewCount": 100,
      "likeCount": 10,
      "commentCount": 5,
      "createdAt": "2024-01-01T00:00:00Z",
      "updatedAt": "2024-01-01T00:00:00Z",
      "author": {
        "id": "author-id",
        "username": "author",
        "displayName": "Author Name",
        "avatar": "avatar-url"
      },
      "categories": [
        {
          "id": "category-id",
          "name": "Category Name",
          "slug": "category-slug"
        }
      ],
      "tags": ["tag1", "tag2"],
      "relevanceScore": 0.95,
      "matchType": "Keyword", // "Exact", "Semantic", "Keyword", "Fuzzy"
      "highlightedTitle": ["Highlighted", "Title"],
      "highlightedContent": ["Highlighted content", "snippets..."]
    }
  ],
  "studyMaterials": [
    {
      "id": "material-id",
      "title": "Material Title",
      "description": "Description...",
      "summary": "Summary...",
      "type": "PDF",
      "url": "material-url",
      "downloadUrl": "download-url",
      "viewCount": 150,
      "downloadCount": 75,
      "averageRating": 4.5,
      "reviewCount": 10,
      "createdAt": "2024-01-01T00:00:00Z",
      "updatedAt": "2024-01-01T00:00:00Z",
      "uploader": {
        "id": "uploader-id",
        "username": "uploader",
        "displayName": "Uploader Name",
        "avatar": "avatar-url"
      },
      "category": {
        "id": "category-id",
        "name": "Category Name",
        "path": "/parent/child"
      },
      "tags": ["tag1", "tag2"],
      "relevanceScore": 0.88,
      "matchType": "Semantic",
      "highlightedTitle": ["Highlighted Title"],
      "highlightedDescription": ["Highlighted description"],
      "isApproved": true,
      "aiConfidence": 0.92
    }
  ],
  "totalPosts": 100,
  "totalStudyMaterials": 50,
  "totalResults": 150,
  "facets": {
    "categories": [
      {
        "id": "category-id",
        "name": "Category Name",
        "slug": "category-slug",
        "count": 25
      }
    ],
    "materialCategories": [
      {
        "id": "material-category-id",
        "name": "Material Category",
        "path": "/path/to/category",
        "level": 1,
        "count": 15,
        "children": []
      }
    ],
    "types": [
      {
        "name": "PDF",
        "count": 30
      }
    ],
    "tags": [
      {
        "name": "tag1",
        "count": 20
      }
    ],
    "years": [
      {
        "name": "2024",
        "count": 100
      }
    ]
  },
  "suggestions": {
    "correctedQuery": "corrected spelling",
    "relatedQueries": ["query 1", "query 2"],
    "didYouMean": ["did you mean 1", "did you mean 2"]
  },
  "queryTime": "00:00:00.123"
}
```

### 2.2 Quick Search (Keyword Only)

```http
GET /search/quick?q=search-term&type=All&page=1&pageSize=10
```

### 2.3 Semantic Search (AI-Powered)

```http
GET /search/semantic?q=search-term&type=Posts&page=1&pageSize=10
```

### 2.4 Advanced Search

```http
GET /search/advanced?q=search-term&type=All&categoryIds=id1,id2&materialCategoryIds=id3&includeSemantic=true&includeKeyword=true&sortBy=relevance&sortOrder=desc&page=1&pageSize=20
```

### 2.5 Post Search Suggestions

```http
GET /search/posts/suggestions?q=search-term&limit=5
```

**Response:**
```json
[
  {
    "id": "post-id",
    "title": "Post Title",
    "isQuestion": true,
    "categories": ["Category 1", "Category 2"]
  }
]
```

### 2.6 Study Material Suggestions

```http
GET /search/materials/suggestions?q=search-term&limit=5
```

**Response:**
```json
[
  {
    "id": "material-id",
    "title": "Material Title",
    "type": "PDF",
    "category": "Category Name"
  }
]
```

### 2.7 Admin - Reindex Content

```http
POST /search/posts/{postId}/reindex
Authorization: Bearer <admin-token>
```

```http
POST /search/materials/{materialId}/reindex
Authorization: Bearer <admin-token>
```

### 2.8 Admin - Search Analytics

```http
GET /search/analytics?from=2024-01-01&to=2024-12-31
Authorization: Bearer <admin-token>
```

**Response:**
```json
{
  "totalQueries": 1000,
  "uniqueQueries": 500,
  "topQueries": ["javascript", "react", "python"],
  "averageQueryLength": 3.5,
  "queryLengthDistribution": [10, 50, 100, 200, 300, 200, 100, 40],
  "topResultTypes": [
    {
      "type": "Posts",
      "count": 600,
      "percentage": 60
    },
    {
      "type": "StudyMaterials",
      "count": 400,
      "percentage": 40
    }
  ],
  "averageResultsPerPage": 15.5
}
```

## 3. Posts

### 3.1 Get All Posts

```http
GET /post?page=1&pageSize=10&categoryIds=guid1,guid2&isQuestion=true
Authorization: Bearer <token>
```

### 3.2 Get Post by ID

```http
GET /post/{postId}
Authorization: Bearer <token>
```

### 3.3 Create Post

```http
POST /post
Authorization: Bearer <token>
Content-Type: application/json

{
  "title": "Post Title",
  "content": "Post content",
  "isQuestion": true,
  "categoryIds": ["category-id-1", "category-id-2"]
}
```

### 3.4 Update Post

```http
PUT /post/{postId}
Authorization: Bearer <token>
Content-Type: application/json

{
  "title": "Updated Title",
  "content": "Updated content",
  "isQuestion": false,
  "categoryIds": ["category-id-1"]
}
```

### 3.5 Delete Post

```http
DELETE /post/{postId}
Authorization: Bearer <token>
```

### 3.6 Get Post Categories

```http
GET /post/categories
Authorization: Bearer <token>
```

## 4. Comments

### 4.1 Get Comments for Post

```http
GET /comment/post/{postId}?page=1&pageSize=20
Authorization: Bearer <token>
```

### 4.2 Create Comment

```http
POST /comment
Authorization: Bearer <token>
Content-Type: application/json

{
  "postId": "post-id",
  "content": "Comment content",
  "parentCommentId": null // for replies
}
```

### 4.3 Update Comment

```http
PUT /comment/{commentId}
Authorization: Bearer <token>
Content-Type: application/json

{
  "content": "Updated comment content"
}
```

### 4.4 Delete Comment

```http
DELETE /comment/{commentId}
Authorization: Bearer <token>
```

## 5. Study Materials

### 5.1 Get All Study Materials

```http
GET /studymaterial?page=1&pageSize=10&categoryId=guid&status=Approved
Authorization: Bearer <token>
```

### 5.2 Get Study Material by ID

```http
GET /studymaterial/{materialId}
Authorization: Bearer <token>
```

### 5.3 Upload Study Material

```http
POST /studymaterial
Authorization: Bearer <token>
Content-Type: multipart/form-data

{
  "file": <file-data>,
  "meta": {
    "title": "Material Title",
    "description": "Material description",
    "categoryId": "category-guid",
    "sourceType": "File" // "File" or "Url"
  }
}
```

### 5.4 Add Study Material via URL

```http
POST /studymaterial/url
Authorization: Bearer <token>
Content-Type: application/json

{
  "meta": {
    "title": "Material Title",
    "description": "Material description",
    "categoryId": "category-guid",
    "sourceType": "Url",
    "sourceUrl": "https://example.com/material"
  }
}
```

### 5.5 Get User's Study Materials

```http
GET /studymaterial/mine?page=1&pageSize=10&status=Pending
Authorization: Bearer <token>
```

### 5.6 Admin Review Study Material

```http
POST /studymaterial/{materialId}/review
Authorization: Bearer <admin-token>
Content-Type: application/json

{
  "decision": "Accepted", // "Accepted" or "Rejected"
  "reason": "Review reason (if rejected)"
}
```

## 6. Material Categories

### 6.1 Get Category Tree

```http
GET /materialcategory/tree
Authorization: Bearer <token>
```

**Response:**
```json
[
  {
    "id": "category-id",
    "name": "Category Name",
    "slug": "category-slug",
    "path": "/parent/category",
    "level": 0,
    "sortOrder": 1,
    "isActive": true,
    "children": [
      {
        "id": "child-id",
        "name": "Child Category",
        "slug": "child-slug",
        "path": "/parent/category/child",
        "level": 1,
        "sortOrder": 1,
        "isActive": true,
        "children": []
      }
    ]
  }
]
```

### 6.2 Create Category (Admin)

```http
POST /materialcategory
Authorization: Bearer <admin-token>
Content-Type: application/json

{
  "name": "New Category",
  "slug": "new-category",
  "parentId": "parent-id", // optional
  "sortOrder": 1
}
```

### 6.3 Update Category (Admin)

```http
PUT /materialcategory/{categoryId}
Authorization: Bearer <admin-token>
Content-Type: application/json

{
  "name": "Updated Category",
  "slug": "updated-category",
  "parentId": "new-parent-id",
  "sortOrder": 2,
  "isActive": true
}
```

### 6.4 Delete Category (Admin)

```http
DELETE /materialcategory/{categoryId}
Authorization: Bearer <admin-token>
```

### 6.5 Move Category (Admin)

```http
POST /materialcategory/{categoryId}/move
Authorization: Bearer <admin-token>
Content-Type: application/json

{
  "newParentId": "new-parent-id", // optional for root level
  "newSortOrder": 3
}
```

## 7. Reactions

### 7.1 Toggle Like/Dislike

```http
POST /reaction
Authorization: Bearer <token>
Content-Type: application/json

{
  "postId": "post-id", // or "commentId"
  "isPositive": true // true for like, false for dislike
}
```

### 7.2 Get Reactions for Post

```http
GET /reaction/post/{postId}
Authorization: Bearer <token>
```

## 8. Chat & Messaging

### 8.1 Get Chat List

```http
GET /chat/list?page=1&pageSize=20
Authorization: Bearer <token>
```

### 8.2 Get Chat Messages

```http
GET /chat/{chatId}/messages?page=1&pageSize=50
Authorization: Bearer <token>
```

### 8.3 Send Message

```http
POST /message
Authorization: Bearer <token>
Content-Type: application/json

{
  "chatId": "chat-id",
  "content": "Message content"
}
```

### 8.4 Mark Messages as Read

```http
PUT /message/chat/{chatId}/read-all
Authorization: Bearer <token>
```

### 8.5 Start New Chat

```http
POST /chat/start
Authorization: Bearer <token>
Content-Type: application/json

{
  "participantIds": ["user-id-1", "user-id-2"],
  "groupName": "Group Name" // optional for group chat
}
```

## 9. SignalR Real-Time Events

### 9.1 Setup SignalR Connection

```typescript
import * as signalR from '@microsoft/signalr';

const connection = new signalR.HubConnectionBuilder()
  .withUrl(`${baseUrl}/hubs/message`, {
    accessTokenFactory: () => token
  })
  .withAutomaticReconnect()
  .build();
```

### 9.2 Event Handlers

```typescript
// New message received
connection.on('NewMessage', (message) => {
  console.log('New message:', message);
  // Update UI
});

// Message read receipt
connection.on('MessageRead', (event) => {
  console.log('Message read:', event);
  // Update message status
});

// New message notification
connection.on('NewMessageNotification', (notification) => {
  console.log('New notification:', notification);
  // Show notification
});

// Messages read in chat
connection.on('MessagesReadInChat', (event) => {
  console.log('All messages read:', event);
  // Update chat status
});
```

## 10. User Profile & Social Features

### 10.1 Get User Profile

```http
GET /user/{userId}
Authorization: Bearer <token>
```

### 10.2 Update User Profile

```http
PUT /user/profile
Authorization: Bearer <token>
Content-Type: application/json

{
  "fullName": "Updated Name",
  "bio": "Updated bio",
  "major": "Updated Major",
  "faculty": "Updated Faculty",
  "yearOfStudy": 3
}
```

### 10.3 Get User's Posts

```http
GET /user/{userId}/posts?page=1&pageSize=10
Authorization: Bearer <token>
```

### 10.4 Get Friends

```http
GET /user/friends?page=1&pageSize=20
Authorization: Bearer <token>
```

### 10.5 Send Friend Request

```http
POST /user/friends/request
Authorization: Bearer <token>
Content-Type: application/json

{
  "userId": "target-user-id"
}
```

### 10.6 Accept/Decline Friend Request

```http
PUT /user/friends/request/{requestId}
Authorization: Bearer <token>
Content-Type: application/json

{
  "status": "Accepted" // "Accepted" or "Declined"
}
```

### 10.7 Unfriend

```http
DELETE /user/friends/{friendId}
Authorization: Bearer <token>
```

## 11. Badges & Reputation

### 11.1 Get User Badges

```http
GET /badge/user/{userId}
Authorization: Bearer <token>
```

### 11.2 Get All Badges

```http
GET /badge
Authorization: Bearer <token>
```

### 11.3 Get Reputation History

```http
GET /reputation/checkpoints/{userId}
Authorization: Bearer <token>
```

## 12. Reports

### 12.1 Create Report

```http
POST /report
Authorization: Bearer <token>
Content-Type: application/json

{
  "postId": "post-id", // or "commentId", "userId"
  "reason": "Inappropriate content",
  "description": "Detailed description of the issue"
}
```

### 12.2 Get Reports (Admin)

```http
GET /report?page=1&pageSize=20&status=Pending
Authorization: Bearer <admin-token>
```

### 12.3 Handle Report (Admin)

```http
PUT /report/{reportId}
Authorization: Bearer <admin-token>
Content-Type: application/json

{
  "status": "Resolved", // "Pending", "Resolved", "Dismissed"
  "adminNote": "Admin notes about the resolution"
}
```

## 13. Notifications

### 13.1 Get Notifications

```http
GET /notification?page=1&pageSize=20&isRead=false
Authorization: Bearer <token>
```

### 13.2 Mark Notification as Read

```http
PUT /notification/{notificationId}/read
Authorization: Bearer <token>
```

### 13.3 Mark All Notifications as Read

```http
PUT /notification/read-all
Authorization: Bearer <token>
```

## 14. Frontend Implementation Tips

### 14.1 Error Handling

All API endpoints return standardized error responses:

```json
{
  "error": "Error type",
  "message": "Detailed error message",
  "code": 400
}
```

### 14.2 Pagination

Most list endpoints support pagination with these parameters:
- `page`: Page number (default: 1)
- `pageSize`: Items per page (default: 10, max: 100)

Response includes pagination metadata:
```json
{
  "items": [...],
  "totalCount": 100,
  "currentPage": 1,
  "pageSize": 10,
  "totalPages": 10
}
```

### 14.3 Search Implementation Best Practices

1. **Debounce Search Input**: Use 300-500ms debounce for search inputs
2. **Implement Auto-suggestions**: Use suggestion endpoints for better UX
3. **Cache Results**: Cache search results for common queries
4. **Show Loading States**: Display loading indicators during search
5. **Handle Empty States**: Show meaningful messages for no results

### 14.4 Real-Time Updates

1. **Connection Management**: Handle connection states and reconnection
2. **Event Handlers**: Register event handlers once and avoid duplicates
3. **Optimistic Updates**: Update UI immediately and rollback on error
4. **Message Queue**: Queue messages when offline and send on reconnect

### 14.5 File Uploads

1. **Progress Tracking**: Show upload progress bars
2. **File Validation**: Validate file types and sizes on client side
3. **Chunk Upload**: For large files, implement chunked uploads
4. **Cancel Upload**: Allow users to cancel ongoing uploads

### 14.6 Performance Optimization

1. **Use Caching**: Cache frequently accessed data
2. **Implement Lazy Loading**: Load data on demand
3. **Use Pagination**: Don't load all data at once
4. **Optimize Images**: Compress and resize images before upload
5. **Minimize API Calls**: Batch requests when possible

## 15. TypeScript Types

```typescript
// Common Types
interface ApiResponse<T> {
  data: T;
  message?: string;
  success: boolean;
}

interface PaginationResponse<T> {
  items: T[];
  totalCount: number;
  currentPage: number;
  pageSize: number;
  totalPages: number;
}

// User Types
interface User {
  id: string;
  email: string;
  username: string;
  fullName?: string;
  avatar?: string;
  role: 'Student' | 'Admin';
  point: number;
  createdAt: string;
}

// Post Types
interface Post {
  id: string;
  title: string;
  content: string;
  isQuestion: boolean;
  authorId: string;
  author: User;
  categories: Category[];
  tags: string[];
  createdAt: string;
  updatedAt: string;
  likeCount: number;
  commentCount: number;
}

// Study Material Types
interface StudyMaterial {
  id: string;
  title: string;
  description?: string;
  type: string;
  url: string;
  downloadUrl?: string;
  categoryId: string;
  category: MaterialCategory;
  status: 'Pending' | 'Approved' | 'Rejected';
  uploader: User;
  createdAt: string;
}

// Search Types
interface SearchResult {
  posts: PostSearchResult[];
  studyMaterials: StudyMaterialSearchResult[];
  totalPosts: number;
  totalStudyMaterials: number;
  facets: SearchFacets;
  suggestions: SearchSuggestions;
  queryTime: string;
}
```

## 16. Testing Checklist

### Authentication
- [ ] User registration works
- [ ] Login/logout functionality
- [ ] Token refresh works
- [ ] Protected routes require authentication

### Search
- [ ] Basic keyword search works
- [ ] Advanced search with filters
- [ ] Category-based search
- [ ] Auto-suggestions display
- [ ] Search results highlight matches
- [ ] Pagination works
- [ ] Sorting options work

### Posts
- [ ] Create/view/update/delete posts
- [ ] Category filtering works
- [ ] Reactions work
- [ ] Comments thread correctly
- [ ] Question/Answer format works

### Study Materials
- [ ] File uploads work
- [ ] URL-based materials work
- [ ] Category browsing works
- [ ] Admin review process works
- [ ] Download functionality works

### Real-time Features
- [ ] SignalR connection establishes
- [ ] Messages appear in real-time
- [ ] Read receipts work
- [ ] Connection handles reconnection

### Error Handling
- [ ] Network errors display properly
- [ ] Validation errors show user-friendly messages
- [ ] Loading states display during operations
- [ ] Empty states show appropriate messages

## 17. Environment Configuration

```env
# API Configuration
NEXT_PUBLIC_API_URL=http://localhost:5000/api
NEXT_PUBLIC_HUB_URL=http://localhost:5000/hubs/message

# Feature Flags
NEXT_PUBLIC_ENABLE_SEARCH=true
NEXT_PUBLIC_ENABLE_CHAT=true
NEXT_PUBLIC_ENABLE_MATERIALS=true
```

## 18. Security Considerations

1. **Token Management**: Store tokens securely (httpOnly cookies or secure storage)
2. **Input Validation**: Validate all user inputs on both client and server
3. **Content Sanitization**: Sanitize user-generated content to prevent XSS
4. **File Uploads**: Validate file types, sizes, and scan for malware
5. **Rate Limiting**: Implement client-side rate limiting for API calls
6. **HTTPS Only**: Always use HTTPS in production
7. **CORS**: Configure CORS properly for your domain

## 19. Troubleshooting

### Common Issues

#### CORS Errors
- Verify API URL in environment variables
- Check CORS configuration on backend
- Ensure proper protocol (http vs https)

#### Authentication Failures
- Check token format (Bearer <token>)
- Verify token is not expired
- Ensure user has required permissions

#### SignalR Connection Issues
- Check hub URL configuration
- Verify WebSocket support in browser
- Check network connectivity
- Review browser console for errors

#### Search Not Working
- Verify search endpoints are accessible
- Check request/response format
- Review search query structure
- Check server logs for errors

### Debug Mode

Add logging to track API calls and responses:

```typescript
const apiCall = async (url: string, options?: RequestInit) => {
  console.log('API Call:', url, options);
  const response = await fetch(url, options);
  const data = await response.json();
  console.log('API Response:', data);
  return data;
};
```

## 20. Contact & Support

For any API-related issues or questions:
- Check the browser console for error details
- Review network tab for failed requests
- Contact the backend development team
- Check API documentation for latest updates

---

**Note**: This documentation is for the current version of the API. Features and endpoints may change in future versions. Always check for the latest updates.