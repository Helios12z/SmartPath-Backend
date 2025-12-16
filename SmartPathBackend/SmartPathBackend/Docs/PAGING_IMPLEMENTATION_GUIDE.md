# Frontend Paging Implementation Guide

This guide provides instructions for implementing pagination in the frontend for all entities that support paging in the SmartPath backend API.

## Overview

The backend now supports pagination for the following entities:
- **Posts** - Offset-based pagination
- **Comments** - Offset-based pagination (top-level comments only)
- **Chat Messages** - Cursor-based pagination
- **Study Materials** - Offset-based pagination

## 1. Posts Pagination

### API Endpoint
```
GET /api/posts?page={page}&pageSize={pageSize}
```

### Parameters
- `page` (int, optional): Page number starting from 1. Default: 1
- `pageSize` (int, optional): Number of items per page. Default: 20, Max: 100

### Response Format
```json
{
  "items": [
    {
      "id": "guid",
      "title": "Post title",
      "content": "Post content",
      // ... other post fields
    }
  ],
  "total": 150
}
```

### Frontend Implementation Example (React)

```jsx
import { useState, useEffect } from 'react';

function PostsList() {
  const [posts, setPosts] = useState([]);
  const [total, setTotal] = useState(0);
  const [page, setPage] = useState(1);
  const [pageSize] = useState(20);
  const [loading, setLoading] = useState(false);

  const fetchPosts = async (pageNum) => {
    setLoading(true);
    try {
      const response = await fetch(
        `/api/posts?page=${pageNum}&pageSize=${pageSize}`
      );
      const data = await response.json();
      setPosts(data.items);
      setTotal(data.total);
    } catch (error) {
      console.error('Error fetching posts:', error);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchPosts(page);
  }, [page]);

  const totalPages = Math.ceil(total / pageSize);

  return (
    <div>
      <div className="posts-list">
        {posts.map(post => (
          <PostCard key={post.id} post={post} />
        ))}
      </div>

      {/* Pagination Controls */}
      <div className="pagination">
        <button
          onClick={() => setPage(p => Math.max(1, p - 1))}
          disabled={page === 1 || loading}
        >
          Previous
        </button>

        <span>Page {page} of {totalPages}</span>

        <button
          onClick={() => setPage(p => Math.min(totalPages, p + 1))}
          disabled={page === totalPages || loading}
        >
          Next
        </button>
      </div>
    </div>
  );
}
```

## 2. Comments Pagination

### API Endpoint
```
GET /api/comments/post/{postId}?page={page}&pageSize={pageSize}
```

### Parameters
- `page` (int, optional): Page number starting from 1. Default: 1
- `pageSize` (int, optional): Number of top-level comments per page. Default: 20, Max: 100

### Important Notes
- Only top-level comments are paginated
- All replies to paginated comments are included in the response
- Comments are ordered by creation time (oldest first)

### Response Format
```json
{
  "items": [
    {
      "id": "guid",
      "content": "Comment content",
      "replies": [
        {
          "id": "guid",
          "content": "Reply content",
          "replies": []
        }
      ]
    }
  ],
  "total": 45
}
```

### Frontend Implementation Example

```jsx
function CommentsSection({ postId }) {
  const [comments, setComments] = useState([]);
  const [total, setTotal] = useState(0);
  const [page, setPage] = useState(1);
  const [loading, setLoading] = useState(false);

  const fetchComments = async (pageNum) => {
    setLoading(true);
    try {
      const response = await fetch(
        `/api/comments/post/${postId}?page=${pageNum}&pageSize=10`
      );
      const data = await response.json();

      if (pageNum === 1) {
        setComments(data.items);
      } else {
        // Append new comments to existing ones
        setComments(prev => [...prev, ...data.items]);
      }
      setTotal(data.total);
    } catch (error) {
      console.error('Error fetching comments:', error);
    } finally {
      setLoading(false);
    }
  };

  const loadMore = () => {
    const nextPage = page + 1;
    setPage(nextPage);
    fetchComments(nextPage);
  };

  useEffect(() => {
    fetchComments(1);
  }, [postId]);

  return (
    <div>
      <div className="comments-list">
        {comments.map(comment => (
          <CommentItem key={comment.id} comment={comment} />
        ))}
      </div>

      {comments.length < total && (
        <button onClick={loadMore} disabled={loading}>
          {loading ? 'Loading...' : 'Load More Comments'}
        </button>
      )}
    </div>
  );
}
```

## 3. Chat Messages Pagination (Cursor-based)

### API Endpoint
```
GET /api/messages/chat/{chatId}?cursor={cursor}&limit={limit}
```

### Parameters
- `cursor` (string, optional): Base64 encoded timestamp cursor. Omit for first page
- `limit` (int, optional): Number of messages to return. Default: 50, Max: 100

### Important Notes
- Messages are ordered from newest to oldest
- Use cursor-based pagination for infinite scroll
- Cursor is a base64 encoded timestamp of the last message received

### Response Format
```json
{
  "items": [
    {
      "id": "guid",
      "content": "Message content",
      "senderUsername": "username",
      "createdAt": "2024-01-01T00:00:00Z",
      "isRead": false
    }
  ],
  "nextCursor": "base64-encoded-timestamp"
}
```

### Frontend Implementation Example

```jsx
import { useState, useEffect, useRef } from 'react';

function ChatMessages({ chatId }) {
  const [messages, setMessages] = useState([]);
  const [cursor, setCursor] = useState(null);
  const [loading, setLoading] = useState(false);
  const [hasMore, setHasMore] = useState(true);
  const messagesEndRef = useRef(null);
  const messagesContainerRef = useRef(null);

  const fetchMessages = async (cursorValue, isInitial = false) => {
    setLoading(true);
    try {
      const params = new URLSearchParams({
        limit: '50'
      });

      if (cursorValue) {
        params.append('cursor', cursorValue);
      }

      const response = await fetch(
        `/api/messages/chat/${chatId}?${params.toString()}`
      );
      const data = await response.json();

      if (isInitial) {
        // For initial load, reverse to show oldest first
        setMessages(data.items.reverse());
      } else {
        // For load more, prepend new messages
        setMessages(prev => [...data.items.reverse(), ...prev]);
      }

      setCursor(data.nextCursor);
      setHasMore(!!data.nextCursor);

      // Scroll to bottom on initial load
      if (isInitial) {
        scrollToBottom();
      }
    } catch (error) {
      console.error('Error fetching messages:', error);
    } finally {
      setLoading(false);
    }
  };

  const scrollToBottom = () => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  };

  const loadMoreMessages = () => {
    if (cursor && !loading && hasMore) {
      fetchMessages(cursor, false);
    }
  };

  useEffect(() => {
    fetchMessages(null, true);
  }, [chatId]);

  return (
    <div
      className="messages-container"
      ref={messagesContainerRef}
      onScroll={(e) => {
        // Load more when scrolled to top
        if (e.target.scrollTop === 0 && hasMore) {
          loadMoreMessages();
        }
      }}
    >
      {/* Load More Indicator */}
      {hasMore && (
        <div className="load-more-indicator">
          {loading ? 'Loading older messages...' : null}
        </div>
      )}

      {/* Messages */}
      {messages.map(message => (
        <MessageBubble key={message.id} message={message} />
      ))}

      <div ref={messagesEndRef} />
    </div>
  );
}
```

### Cursor Management Helper

```javascript
// Utility functions for cursor handling
const cursorUtils = {
  // Parse cursor from response
  parseCursor: (cursor) => {
    if (!cursor) return null;
    try {
      const bytes = atob(cursor);
      return new Date(parseInt(bytes, 10));
    } catch {
      return null;
    }
  },

  // Check if we should load more messages
  shouldLoadMore: (scrollTop, hasMore, loading) => {
    return scrollTop === 0 && hasMore && !loading;
  }
};
```

## 4. Study Materials Pagination

### API Endpoint
```
GET /api/study-materials/search?page={page}&pageSize={pageSize}&q={query}&categoryId={categoryId}&status={status}
```

### Parameters
- `page` (int, optional): Page number starting from 1. Default: 1
- `pageSize` (int, optional): Number of items per page. Default: 20, Max: 100
- `q` (string, optional): Search query
- `categoryId` (guid, optional): Filter by category
- `status` (string, optional): Filter by status (Pending, Accepted, Rejected)

### My Materials Endpoint
```
GET /api/study-materials/mine?page={page}&pageSize={pageSize}&status={status}
```

### Response Format
```json
{
  "items": [
    {
      "id": "guid",
      "title": "Material title",
      "description": "Description",
      "averageRating": 4.5,
      "totalRatings": 10,
      // ... other material fields
    }
  ],
  "total": 75
}
```

### Frontend Implementation Example

```jsx
function StudyMaterialsList() {
  const [materials, setMaterials] = useState([]);
  const [total, setTotal] = useState(0);
  const [page, setPage] = useState(1);
  const [pageSize] = useState(20);
  const [filters, setFilters] = useState({
    q: '',
    categoryId: null,
    status: null
  });
  const [loading, setLoading] = useState(false);

  const fetchMaterials = async (pageNum) => {
    setLoading(true);
    try {
      const params = new URLSearchParams({
        page: pageNum,
        pageSize: pageSize
      });

      if (filters.q) params.append('q', filters.q);
      if (filters.categoryId) params.append('categoryId', filters.categoryId);
      if (filters.status) params.append('status', filters.status);

      const response = await fetch(
        `/api/study-materials/search?${params.toString()}`
      );
      const data = await response.json();

      setMaterials(data.items);
      setTotal(data.total);
    } catch (error) {
      console.error('Error fetching materials:', error);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    setPage(1); // Reset to first page when filters change
    fetchMaterials(1);
  }, [filters]);

  useEffect(() => {
    fetchMaterials(page);
  }, [page]);

  return (
    <div>
      {/* Search and Filters */}
      <SearchFilters
        filters={filters}
        onFiltersChange={setFilters}
      />

      {/* Materials Grid */}
      <div className="materials-grid">
        {materials.map(material => (
          <MaterialCard key={material.id} material={material} />
        ))}
      </div>

      {/* Pagination */}
      <PaginationControls
        currentPage={page}
        total={total}
        pageSize={pageSize}
        onPageChange={setPage}
        loading={loading}
      />
    </div>
  );
}
```

## Common Pagination Component

```jsx
function PaginationControls({
  currentPage,
  total,
  pageSize,
  onPageChange,
  loading = false
}) {
  const totalPages = Math.ceil(total / pageSize);
  const hasNextPage = currentPage < totalPages;
  const hasPrevPage = currentPage > 1;

  const goToPage = (page) => {
    if (page >= 1 && page <= totalPages) {
      onPageChange(page);
    }
  };

  // Generate page numbers to show
  const getVisiblePages = () => {
    const delta = 2; // Number of pages to show on each side
    const range = [];
    const rangeWithDots = [];
    let l;

    for (let i = 1; i <= totalPages; i++) {
      if (i === 1 || i === totalPages || (i >= currentPage - delta && i <= currentPage + delta)) {
        range.push(i);
      }
    }

    range.forEach((i) => {
      if (l) {
        if (i - l === 2) {
          rangeWithDots.push(l + 1);
        } else if (i - l !== 1) {
          rangeWithDots.push('...');
        }
      }
      rangeWithDots.push(i);
      l = i;
    });

    return rangeWithDots;
  };

  return (
    <div className="pagination-controls">
      <div className="pagination-info">
        Showing {((currentPage - 1) * pageSize) + 1} to {Math.min(currentPage * pageSize, total)} of {total} items
      </div>

      <div className="pagination-buttons">
        <button
          onClick={() => goToPage(currentPage - 1)}
          disabled={!hasPrevPage || loading}
          className="pagination-btn"
        >
          Previous
        </button>

        {getVisiblePages().map((page, index) => (
          page === '...' ? (
            <span key={`dots-${index}`} className="pagination-ellipsis">...</span>
          ) : (
            <button
              key={page}
              onClick={() => goToPage(page)}
              disabled={loading}
              className={`pagination-btn ${currentPage === page ? 'active' : ''}`}
            >
              {page}
            </button>
          )
        ))}

        <button
          onClick={() => goToPage(currentPage + 1)}
          disabled={!hasNextPage || loading}
          className="pagination-btn"
        >
          Next
        </button>
      </div>
    </div>
  );
}
```

## Best Practices

### 1. Loading States
- Always show loading indicators during data fetching
- Disable pagination buttons during loading
- Show skeleton loaders for better UX

### 2. Error Handling
- Implement retry mechanisms for failed requests
- Show user-friendly error messages
- Allow manual retry

### 3. Performance Optimization
- Implement debouncing for search inputs
- Use React.memo or similar optimizations for list items
- Consider virtual scrolling for large lists

### 4. URL State Management
- Store pagination state in URL for bookmarkability
- Use browser history API for navigation
- Implement back/forward navigation properly

```javascript
// URL state management example
const usePaginationState = (initialPage = 1, initialPageSize = 20) => {
  const [searchParams, setSearchParams] = useSearchParams();

  const page = parseInt(searchParams.get('page') || initialPage);
  const pageSize = parseInt(searchParams.get('pageSize') || initialPageSize);

  const updatePagination = (newPage, newPageSize) => {
    const newParams = new URLSearchParams(searchParams);
    newParams.set('page', newPage);
    newParams.set('pageSize', newPageSize);
    setSearchParams(newParams);
  };

  return { page, pageSize, updatePagination };
};
```

### 5. Real-time Updates
- For chat messages, implement WebSocket updates
- Show real-time comment counts
- Handle optimistic updates for better UX

## Summary

This guide provides complete implementations for:
- **Offset-based pagination** for Posts, Comments, and Study Materials
- **Cursor-based pagination** for Chat Messages
- Reusable pagination components
- Best practices for performance and UX

The backend implementation ensures consistent behavior across all entities while following established patterns in the SmartPath application.