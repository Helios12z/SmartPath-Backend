# SmartPath Search API Guide

## Overview

The SmartPath search API provides a powerful unified search system that can search through posts, study materials, and more with support for semantic search, keyword search, filtering, and sorting.

## Base URL
```
https://your-domain.com/api/search
```

## Authentication
- Most endpoints are public (`AllowAnonymous`)
- Admin-only endpoints require authentication with `Admin` role
- Use JWT Bearer token for authentication

## Main Search Endpoint

### POST /api/search
Unified search endpoint that handles all search types and filters. This is the only search endpoint you need.

**Request Body:**
```json
{
  "query": "react hooks tutorial",
  "searchType": "All", // "All", "Posts", "StudyMaterials"
  "categoryIds": ["guid1", "guid2"], // Post categories
  "materialCategoryIds": ["guid3", "guid4"], // Material categories
  "isQuestion": null, // true, false, or null for both
  "includeSemanticSearch": true,
  "includeKeywordSearch": true,
  "sortBy": "relevance", // "relevance", "created", "updated", "views", "likes", "rating"
  "sortOrder": "desc", // "asc", "desc"
  "page": 1,
  "pageSize": 20,
  "fromDate": "2024-01-01T00:00:00Z", // Optional
  "toDate": "2024-12-31T23:59:59Z", // Optional
  "tags": ["javascript", "react"] // Optional
}
```

**Example Request:**
```javascript
const response = await fetch('/api/search', {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json',
  },
  body: JSON.stringify({
    query: 'react hooks',
    searchType: 'All',
    includeSemanticSearch: true,
    includeKeywordSearch: true,
    sortBy: 'relevance',
    sortOrder: 'desc',
    page: 1,
    pageSize: 20,
    categoryIds: ['category-guid-1'],
    tags: ['react', 'hooks']
  })
});
```

## Response Format

```json
{
  "posts": [
    {
      "id": "guid",
      "title": "Understanding React Hooks",
      "content": "React Hooks revolutionized...",
      "summary": null,
      "isQuestion": false,
      "isSolved": true,
      "viewCount": 1250,
      "likeCount": 89,
      "commentCount": 23,
      "createdAt": "2024-01-15T10:30:00Z",
      "updatedAt": "2024-01-15T10:30:00Z",
      "author": {
        "id": "guid",
        "username": "johndoe",
        "displayName": "John Doe",
        "avatar": "url-to-avatar"
      },
      "categories": [
        {
          "id": "guid",
          "name": "React",
          "slug": "react"
        }
      ],
      "tags": ["react", "hooks", "javascript"],
      "relevanceScore": 0.95,
      "matchType": "Semantic",
      "highlightedTitle": [],
      "highlightedContent": []
    }
  ],
  "studyMaterials": [
    {
      "id": "guid",
      "title": "React Hooks Complete Guide",
      "description": "A comprehensive guide...",
      "summary": "Learn everything about React Hooks",
      "type": "PDF",
      "url": "https://example.com/file.pdf",
      "downloadUrl": "https://example.com/download/file.pdf",
      "viewCount": 3420,
      "downloadCount": 892,
      "averageRating": 4.7,
      "reviewCount": 45,
      "createdAt": "2024-01-10T08:00:00Z",
      "updatedAt": "2024-01-10T08:00:00Z",
      "uploader": {
        "id": "guid",
        "username": "author",
        "displayName": "Author Name",
        "avatar": "url-to-avatar"
      },
      "category": {
        "id": "guid",
        "name": "Frontend Development",
        "path": "Programming/Frontend/React"
      },
      "tags": ["react", "hooks", "tutorial"],
      "relevanceScore": 0.88,
      "matchType": "Keyword",
      "highlightedTitle": [],
      "highlightedDescription": [],
      "isApproved": true,
      "aiConfidence": 0.92
    }
  ],
  "totalPosts": 15,
  "totalStudyMaterials": 8,
  "totalResults": 23,
  "facets": {
    "categories": [],
    "materialCategories": [],
    "types": [],
    "tags": [],
    "years": []
  },
  "suggestions": {
    "correctedQuery": "",
    "relatedQueries": ["react hooks useEffect", "react hooks state"],
    "didYouMean": []
  },
  "queryTime": "00:00:00.1250000"
}
```

## Additional Endpoints

### Get Post Suggestions
Auto-complete suggestions for posts as user types.

**GET /api/search/posts/suggestions**
```
GET /api/search/posts/suggestions?q=react&limit=5
```

**Response:**
```json
[
  {
    "id": "guid",
    "title": "React Hooks Tutorial",
    "isQuestion": false,
    "categories": ["React", "JavaScript"]
  }
]
```

### Get Material Suggestions
Auto-complete suggestions for study materials.

**GET /api/search/materials/suggestions**
```
GET /api/search/materials/suggestions?q=react&limit=5
```

**Response:**
```json
[
  {
    "id": "guid",
    "title": "React Hooks Guide",
    "type": "PDF",
    "category": "React"
  }
]
```

### Admin Endpoints

#### Reindex Post
Force reindex a specific post in the search index.

**POST /api/search/posts/{postId}/reindex**
```
POST /api/search/posts/550e8400-e29b-41d4-a716-446655440000/reindex
```

#### Reindex Material
Force reindex a specific study material.

**POST /api/search/materials/{materialId}/reindex**
```
POST /api/search/materials/550e8400-e29b-41d4-a716-446655440000/reindex
```

#### Get Search Analytics
Get search analytics for admin dashboard.

**GET /api/search/analytics**
```
GET /api/search/analytics?from=2024-01-01&to=2024-01-31
```

## Search Types and Options

### SearchType Values
- `All`: Search both posts and study materials
- `Posts`: Search only posts
- `StudyMaterials`: Search only study materials

### SortBy Options
- `relevance`: Sort by relevance score (default)
- `created`: Sort by creation date
- `updated`: Sort by last update date
- `views`: Sort by view count
- `likes`: Sort by like count (posts only)
- `rating`: Sort by average rating (materials only)

### Match Types
- `Semantic`: AI-powered semantic search using embeddings
- `Keyword`: Traditional keyword matching
- `Exact`: Exact phrase matches
- `Fuzzy`: Fuzzy matching with typos

## Best Practices

### 1. Use Semantic Search for Natural Language
```javascript
// Good for natural language queries
{
  "query": "how to implement user authentication in react",
  "includeSemanticSearch": true,
  "includeKeywordSearch": true
}
```

### 2. Use Keyword Search for Specific Terms
```javascript
// Good for specific technical terms
{
  "query": "useEffect cleanup function",
  "includeSemanticSearch": false,
  "includeKeywordSearch": true
}
```

### 3. Combine Filters for Precise Results
```javascript
// Find React tutorials from 2024
{
  "query": "react tutorial",
  "searchType": "StudyMaterials",
  "materialCategoryIds": ["react-category-guid"],
  "fromDate": "2024-01-01T00:00:00Z",
  "tags": ["react", "tutorial"],
  "sortBy": "rating",
  "sortOrder": "desc"
}
```

### 4. Pagination
Always use pagination for better performance:
```javascript
{
  "query": "javascript",
  "page": 1,
  "pageSize": 20
}
```

### 5. Handle Empty Results
Always check if results are empty:
```javascript
const data = await response.json();
if (data.totalResults === 0) {
  // Show "no results found" message
  console.log('No results found for:', request.query);
}
```

## Error Handling

### Common Error Responses
```json
{
  "error": "Search failed",
  "message": "Query is required"
}
```

```json
{
  "error": "Search failed",
  "message": "Connection timeout"
}
```

### Frontend Error Handling Example
```javascript
try {
  const response = await fetch('/api/search', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify(searchRequest)
  });

  if (!response.ok) {
    const error = await response.json();
    throw new Error(error.message || 'Search failed');
  }

  const data = await response.json();
  return data;
} catch (error) {
  console.error('Search error:', error);
  // Show user-friendly error message
  alert('Search failed. Please try again.');
}
```

## Performance Tips

1. **Debounce Search Input**: Wait for user to stop typing (300-500ms)
2. **Limit Page Size**: Use reasonable page sizes (10-50 items)
3. **Use GET for Simple Searches**: Faster for basic queries
4. **Cache Categories**: Cache category lists for filter UI
5. **Lazy Load Results**: Implement infinite scroll with pagination

## Example Frontend Integration

### React Hook for Search
```javascript
import { useState, useCallback } from 'react';

export function useSearch() {
  const [loading, setLoading] = useState(false);
  const [results, setResults] = useState(null);
  const [error, setError] = useState(null);

  const search = useCallback(async (searchRequest) => {
    setLoading(true);
    setError(null);

    try {
      const response = await fetch('/api/search', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(searchRequest)
      });

      if (!response.ok) {
        const errorData = await response.json();
        throw new Error(errorData.message || 'Search failed');
      }

      const data = await response.json();
      setResults(data);
      return data;
    } catch (err) {
      setError(err.message);
      throw err;
    } finally {
      setLoading(false);
    }
  }, []);

  return {
    search,
    loading,
    results,
    error
  };
}
```

### Debounced Search Input
```javascript
import { useEffect, useState } from 'react';

function useDebounce(value, delay) {
  const [debouncedValue, setDebouncedValue] = useState(value);

  useEffect(() => {
    const handler = setTimeout(() => {
      setDebouncedValue(value);
    }, delay);

    return () => {
      clearTimeout(handler);
    };
  }, [value, delay]);

  return debouncedValue;
}

// Usage in component
function SearchComponent() {
  const [query, setQuery] = useState('');
  const debouncedQuery = useDebounce(query, 300);
  const { search, loading, results } = useSearch();

  useEffect(() => {
    if (debouncedQuery) {
      search({
        query: debouncedQuery,
        searchType: 'All',
        page: 1,
        pageSize: 20
      });
    }
  }, [debouncedQuery, search]);

  return (
    <input
      type="text"
      value={query}
      onChange={(e) => setQuery(e.target.value)}
      placeholder="Search..."
    />
  );
}
```

## Testing the API

### Using curl
```bash
# Search with POST
curl -X POST "https://your-domain.com/api/search" \
  -H "Content-Type: application/json" \
  -d '{
    "query": "react hooks",
    "searchType": "All",
    "includeSemanticSearch": true,
    "includeKeywordSearch": true,
    "page": 1,
    "pageSize": 20
  }'

# Get suggestions
curl -X GET "https://your-domain.com/api/search/posts/suggestions?q=react&limit=5"
```

### Using JavaScript fetch
```javascript
// Search function
const search = async (filters) => {
  const response = await fetch('/api/search', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify(filters)
  });

  if (!response.ok) {
    throw new Error('Search failed');
  }

  return response.json();
};

// Example usage
const results = await search({
  query: 'react hooks',
  searchType: 'All',
  includeSemanticSearch: true,
  includeKeywordSearch: true,
  page: 1,
  pageSize: 20
});
```

## Summary

The SmartPath search API provides:
- ✅ Unified search endpoint for all content types
- ✅ Support for both semantic and keyword search
- ✅ Advanced filtering by categories, tags, dates
- ✅ Flexible sorting options
- ✅ Auto-complete suggestions
- ✅ Pagination support
- ✅ Admin endpoints for management
- ✅ Analytics and monitoring

Use the POST endpoint for complex searches with many filters, and the GET endpoint for simple queries. Always handle errors gracefully and implement proper loading states in your frontend application.