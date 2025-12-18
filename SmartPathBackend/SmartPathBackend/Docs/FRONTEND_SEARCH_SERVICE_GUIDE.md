# Frontend Search Service Implementation Guide

## Overview

The search service provides unified search across posts and study materials with keyword search, semantic search (AI-powered), and filtering capabilities.

## API Endpoint

```
POST /api/search
```

## Request Format

```typescript
interface SearchRequestDTO {
  query: string;                    // Required: Search query text
  searchType: 'All' | 'Posts' | 'StudyMaterials';  // Optional: Default 'All'
  categoryIds?: Guid[];              // Optional: Filter by post categories (not implemented yet)
  materialCategoryIds?: Guid[];       // Optional: Filter by material categories
  isQuestion?: boolean;               // Optional: Filter posts by question status
  tags?: string[];                     // Optional: Filter by tags (not implemented yet)

  // Search methods (both enabled by default)
  includeKeywordSearch: boolean;       // Optional: Default true
  includeSemanticSearch: boolean;      // Optional: Default true

  // Sorting options
  sortBy: 'relevance' | 'created' | 'updated' | 'views'; // Optional: Default 'relevance'
  sortOrder: 'asc' | 'desc';         // Optional: Default 'desc'

  // Date filtering
  fromDate?: Date;                  // Optional: Start date filter
  toDate?: Date;                    // Optional: End date filter

  // No pagination parameters (removed)
}
```

## Response Format

```typescript
interface SearchResultDTO {
  // Post results
  posts: PostSearchResultDTO[];
  totalPosts: number;

  // Study material results
  studyMaterials: StudyMaterialSearchResultDTO[];
  totalStudyMaterials: number;

  // Combined stats
  totalResults: number;

  // Facets for filtering (categories temporarily disabled)
  facets: SearchFacetsDTO;

  // Suggestions when no results found
  suggestions: SearchSuggestionDTO;

  // Performance metric
  queryTime: TimeSpan;
}
```

### Post Search Result

```typescript
interface PostSearchResultDTO {
  id: Guid;
  title: string;
  content: string;
  summary?: string;                   // Truncated preview (500 chars)
  isQuestion: boolean;
  isSolved: boolean;
  viewCount: number;
  likeCount: number;
  commentCount: number;
  createdAt: Date;
  updatedAt: Date;

  // Author information
  author: AuthorDTO {
    id: Guid;
    username: string;
    displayName: string;
    avatar?: string;
  };

  // Categories and tags temporarily removed
  categories: CategoryDTO[];
  tags: string[];

  // Scoring
  relevanceScore: number;
  matchType: 'Keyword' | 'Semantic';

  // Search highlighting
  highlightedTitle: string;          // Title with search terms highlighted
  highlightedContent: string;         // Content preview with search terms highlighted
}
```

### Study Material Search Result

```typescript
interface StudyMaterialSearchResultDTO {
  id: Guid;
  title: string;
  description?: string;
  summary?: string;
  type: string;                          // 'Document', 'Video', 'Link', etc.
  url?: string;
  downloadUrl?: string;
  viewCount: number;
  downloadCount: number;
  averageRating: number;
  reviewCount: number;
  createdAt: Date;
  updatedAt: Date;

  // Uploader information
  uploader: AuthorDTO {
    id: Guid;
    username: string;
    displayName: string;
    avatar?: string;
  };

  // Category information
  category: MaterialCategoryDTO {
    id: Guid;
    name: string;
    path: string;
  };

  tags: string[];

  // Scoring
  relevanceScore: number;
  matchType: 'Keyword' | 'Semantic';

  // Search highlighting
  highlightedTitle: string;
  highlightedDescription: string;

  // Approval status
  isApproved: boolean;
  aiConfidence?: number;
}
```

## Basic Implementation

### 1. Search Component

```typescript
import React, { useState } from 'react';

const SearchComponent: React.FC = () => {
  const [query, setQuery] = useState('');
  const [results, setResults] = useState<SearchResultDTO | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleSearch = async () => {
    if (!query.trim()) return;

    setLoading(true);
    setError(null);

    try {
      const request: SearchRequestDTO = {
        query: query.trim(),
        searchType: 'All',
        includeKeywordSearch: true,
        includeSemanticSearch: true,
        sortBy: 'relevance',
        sortOrder: 'desc'
      };

      const response = await fetch('/api/search', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        'Authorization': `Bearer ${getToken()}`
        },
        body: JSON.stringify(request)
      });

      if (!response.ok) {
        throw new Error('Search failed');
      }

      const data: SearchResultDTO = await response.json();
      setResults(data);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Unknown error');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="search-container">
      <div className="search-input-group">
        <input
          type="text"
          value={query}
          onChange={(e) => setQuery(e.target.value)}
          placeholder="Search posts and materials..."
          onKeyPress={(e) => e.key === 'Enter' && handleSearch()}
        />
        <button onClick={handleSearch} disabled={loading || !query.trim()}>
          {loading ? 'Searching...' : 'Search'}
        </button>
      </div>

      {error && <div className="error-message">{error}</div>}

      {results && <SearchResults results={results} />}
    </div>
  );
};
```

### 2. Search Results Display

```typescript
interface SearchResultsProps {
  results: SearchResultDTO;
}

const SearchResults: React.FC<SearchResultsProps> = ({ results }) => {
  return (
    <div className="search-results">
      {/* Posts Section */}
      {results.posts.length > 0 && (
        <div className="results-section">
          <h3>Posts ({results.totalPosts})</h3>
          <div className="posts-grid">
            {results.posts.map(post => (
              <PostCard key={post.id} post={post} />
            ))}
          </div>
        </div>
      )}

      {/* Study Materials Section */}
      {results.studyMaterials.length > 0 && (
        <div className="results-section">
          <h3>Study Materials ({results.totalStudyMaterials})</h3>
          <div className="materials-grid">
            {results.studyMaterials.map(material => (
              <MaterialCard key={material.id} material={material} />
            ))}
          </div>
        </div>
      )}

      {/* No Results */}
      {results.totalResults === 0 && (
        <div className="no-results">
          <p>No results found for "{/* show original query if stored */}"</p>
          {results.suggestions && (
            <div className="suggestions">
              <h4>Try searching for:</h4>
              <ul>
                {results.suggestions.posts.map(post => (
                  <li key={post.id}>
                    <a href={`/posts/${post.id}`}>{post.title}</a>
                  </li>
                ))}
                {results.suggestions.studyMaterials.map(material => (
                  <li key={material.id}>
                    <a href={`/materials/${material.id}`}>{material.title}</a>
                  </li>
                ))}
              </ul>
            </div>
          )}
        </div>
      )}
    </div>
  );
};
```

## Advanced Implementation

### 1. Search with Filters

```typescript
const AdvancedSearch: React.FC = () => {
  const [filters, setFilters] = useState({
    searchType: 'All' as 'All' | 'Posts' | 'StudyMaterials',
    sortBy: 'relevance' as 'relevance' | 'created' | 'updated' | 'views',
    sortOrder: 'desc' as 'asc' | 'desc',
    isQuestion: undefined as boolean | undefined,
    materialCategoryIds: [] as Guid[],
    dateRange: {
      from: undefined as Date | undefined,
      to: undefined as Date | undefined
    }
  });

  const buildSearchRequest = (query: string): SearchRequestDTO => ({
    query: query.trim(),
    ...filters,
    includeKeywordSearch: true,
    includeSemanticSearch: true
  });

  return (
    <div className="advanced-search">
      {/* Search input */}

      {/* Search type filter */}
      <div className="filter-group">
        <label>Search Type:</label>
        <select
          value={filters.searchType}
          onChange={(e) => setFilters(prev => ({
            ...prev,
            searchType: e.target.value as any
          }))}
        >
          <option value="All">All</option>
          <option value="Posts">Posts Only</option>
          <option value="StudyMaterials">Study Materials Only</option>
        </select>
      </div>

      {/* Post filters */}
      {filters.searchType === 'All' || filters.searchType === 'Posts' ? (
        <div className="filter-group">
          <label>
            <input
              type="checkbox"
              checked={filters.isQuestion || false}
              onChange={(e) => setFilters(prev => ({
                ...prev,
                isQuestion: e.target.checked ? true : undefined
              }))}
            />
            Questions Only
          </label>
        </div>
      ) : null}

      {/* Material category filters */}
      {filters.searchType === 'All' || filters.searchType === 'StudyMaterials' ? (
        <div className="filter-group">
          <label>Categories:</label>
          <CategoryMultiSelect
            value={filters.materialCategoryIds}
            onChange={(ids) => setFilters(prev => ({
              ...prev,
              materialCategoryIds: ids
            }))}
          />
        </div>
      ) : null}

      {/* Date range filter */}
      <div className="filter-group">
        <label>Date Range:</label>
        <DateRangePicker
          value={filters.dateRange}
          onChange={(range) => setFilters(prev => ({
            ...prev,
            dateRange: range
          }))}
        />
      </div>

      {/* Sort options */}
      <div className="filter-group">
        <label>Sort By:</label>
        <select
          value={filters.sortBy}
          onChange={(e) => setFilters(prev => ({
            ...prev,
            sortBy: e.target.value as any
          }))}
        >
          <option value="relevance">Most Relevant</option>
          <option value="created">Newest</option>
          <option value="updated">Recently Updated</option>
          <option value="views">Most Viewed</option>
        </select>

        <select
          value={filters.sortOrder}
          onChange={(e) => setFilters(prev => ({
            ...prev,
            sortOrder: e.target.value as any
          }))}
        >
          <option value="desc">Descending</option>
          <option value="asc">Ascending</option>
        </select>
      </div>
    </div>
  );
};
```

### 2. Search Highlighting

```typescript
interface HighlightedTextProps {
  text: string;
  highlightTerms: string[];
  className?: string;
}

const HighlightedText: React.FC<HighlightedTextProps> = ({
  text,
  highlightTerms,
  className = ''
}) => {
  if (!highlightTerms.length) {
    return <span className={className}>{text}</span>;
  }

  const regex = new RegExp(
    `(${highlightTerms.map(term => escapeRegex(term)).join('|')})`,
    'gi'
  );

  const parts = text.split(regex);

  return (
    <span className={className}>
      {parts.map((part, index) =>
        highlightTerms.some(term =>
          part.toLowerCase() === term.toLowerCase()
        ) ? (
          <mark key={index} className="search-highlight">
            {part}
          </mark>
        ) : (
          <span key={index}>{part}</span>
        )
      )}
    </span>
  );
};

// Usage in PostCard
const PostCard: React.FC<{ post: PostSearchResultDTO }> = ({ post }) => {
  return (
    <div className="post-card">
      <h3 className="post-title">
        <HighlightedText
          text={post.highlightedTitle || post.title}
          highlightTerms={/* extract from highlighted content */}
        />
      </h3>

      {post.highlightedContent && (
        <p className="post-excerpt">
          <HighlightedText
            text={post.highlightedContent}
            highlightTerms={/* extract from highlighted content */}
          />
        </p>
      )}
    </div>
  );
};
```

### 3. Real-time Search (Debounced)

```typescript
import { useCallback, useEffect } from 'react';

const useDebouncedSearch = (delay = 300) => {
  const [debouncedQuery, setDebouncedQuery] = useState('');
  const [isSearching, setIsSearching] = useState(false);

  const debouncedSearch = useCallback(
    debounce((query: string) => {
      setDebouncedQuery(query);
      setIsSearching(query.length > 0);
    }, delay),
    []
  );

  const search = useCallback((query: string) => {
    debouncedSearch(query);
  }, [debouncedSearch]);

  return { debouncedQuery, isSearching, search };
};

const RealtimeSearch: React.FC = () => {
  const [query, setQuery] = useState('');
  const { debouncedQuery, isSearching, search } = useDebouncedSearch(300);
  const [results, setResults] = useState<SearchResultDTO | null>(null);

  useEffect(() => {
    if (debouncedQuery.length >= 2) {
      performSearch(debouncedQuery);
    } else {
      setResults(null);
    }
  }, [debouncedQuery]);

  const performSearch = async (searchQuery: string) => {
    // ... search implementation
  };

  return (
    <div className="realtime-search">
      <input
        type="text"
        value={query}
        onChange={(e) => {
          setQuery(e.target.value);
          search(e.target.value);
        }}
        placeholder="Start typing to search..."
      />

      {isSearching && <div className="searching-indicator">Searching...</div>}

      {results && <SearchResults results={results} />}
    </div>
  );
};
```

## Search Suggestions/Autocomplete

### 1. Search Input with Suggestions

```typescript
const SearchWithSuggestions: React.FC = () => {
  const [query, setQuery] = useState('');
  const [suggestions, setSuggestions] = useState<{
    posts: PostSuggestionDTO[];
    materials: StudyMaterialSuggestionDTO[];
  } | null>(null);
  const [showSuggestions, setShowSuggestions] = useState(false);

  const fetchSuggestions = async (searchQuery: string) => {
    if (searchQuery.length < 2) {
      setSuggestions(null);
      return;
    }

    try {
      const [postsRes, materialsRes] = await Promise.all([
        fetch(`/api/search/posts/suggestions?q=${encodeURIComponent(searchQuery)}`),
        fetch(`/api/search/materials/suggestions?q=${encodeURIComponent(searchQuery)}`)
      ]);

      const [posts, materials] = await Promise.all([
        postsRes.json(),
        materialsRes.json()
      ]);

      setSuggestions({ posts, materials });
      setShowSuggestions(true);
    } catch (error) {
      console.error('Failed to fetch suggestions:', error);
    }
  };

  const debouncedFetch = useCallback(debounce(fetchSuggestions, 300), []);

  const handleInputChange = (value: string) => {
    setQuery(value);
    debouncedFetch(value);
  };

  return (
    <div className="search-with-suggestions">
      <div className="search-input-wrapper">
        <input
          type="text"
          value={query}
          onChange={(e) => handleInputChange(e.target.value)}
          onFocus={() => query.length >= 2 && setShowSuggestions(true)}
          onBlur={() => setTimeout(() => setShowSuggestions(false), 200)}
          placeholder="Search..."
        />

        {showSuggestions && suggestions && (
          <div className="suggestions-dropdown">
            {/* Post suggestions */}
            {suggestions.posts.length > 0 && (
              <div className="suggestion-section">
                <h4>Posts</h4>
                {suggestions.posts.map(post => (
                  <div
                    key={post.id}
                    className="suggestion-item"
                    onClick={() => {
                      window.location.href = `/posts/${post.id}`;
                    }}
                  >
                    <span className="suggestion-title">{post.title}</span>
                    {post.isQuestion && (
                      <span className="question-badge">Q</span>
                    )}
                  </div>
                ))}
              </div>
            )}

            {/* Material suggestions */}
            {suggestions.materials.length > 0 && (
              <div className="suggestion-section">
                <h4>Study Materials</h4>
                {suggestions.materials.map(material => (
                  <div
                    key={material.id}
                    className="suggestion-item"
                    onClick={() => {
                      window.location.href = `/materials/${material.id}`;
                    }}
                  >
                    <span className="suggestion-title">{material.title}</span>
                    <span className="material-type">{material.type}</span>
                  </div>
                ))}
              </div>
            )}

            {suggestions.posts.length === 0 && suggestions.materials.length === 0 && (
              <div className="no-suggestions">
                No suggestions found
              </div>
            )}
          </div>
        )}
      </div>
    </div>
  );
};
```

## Performance Considerations

### 1. Caching Search Results

```typescript
const searchCache = new Map<string, SearchResultDTO>();

const getCachedSearch = (request: SearchRequestDTO): SearchResultDTO | null => {
  const cacheKey = JSON.stringify(request);
  return searchCache.get(cacheKey) || null;
};

const setCachedSearch = (request: SearchRequestDTO, results: SearchResultDTO): void => {
  const cacheKey = JSON.stringify(request);
  searchCache.set(cacheKey, results);

  // Cache for 5 minutes
  setTimeout(() => {
    searchCache.delete(cacheKey);
  }, 5 * 60 * 1000);
};
```

### 2. Loading States

```typescript
interface SearchState {
  data: SearchResultDTO | null;
  loading: boolean;
  error: string | null;
  queryTime?: number;
}

const useSearchState = (): SearchState => {
  const [state, setState] = useState<SearchState>({
    data: null,
    loading: false,
    error: null
  });

  const search = useCallback(async (request: SearchRequestDTO) => {
    // Check cache first
    const cached = getCachedSearch(request);
    if (cached) {
      setState({ data: cached, loading: false, error: null });
      return cached;
    }

    setState(prev => ({ ...prev, loading: true, error: null }));

    const startTime = performance.now();

    try {
      const response = await fetch('/api/search', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(request)
      });

      if (!response.ok) {
        throw new Error(`HTTP ${response.status}`);
      }

      const data = await response.json();
      const queryTime = performance.now() - startTime;

      // Cache results
      setCachedSearch(request, data);

      setState({
        data,
        loading: false,
        error: null,
        queryTime
      });

      return data;
    } catch (error) {
      const errorMessage = error instanceof Error ? error.message : 'Unknown error';
      setState(prev => ({
        ...prev,
        loading: false,
        error: errorMessage
      }));
      throw error;
    }
  }, []);

  return { ...state, search };
};
```

## Error Handling

```typescript
interface SearchErrorProps {
  error: string | null;
  onRetry?: () => void;
}

const SearchError: React.FC<SearchErrorProps> = ({ error, onRetry }) => {
  if (!error) return null;

  return (
    <div className="search-error">
      <div className="error-icon">⚠️</div>
      <div className="error-message">
        <strong>Search Error</strong>
        <p>{error}</p>
      </div>
      {onRetry && (
        <button onClick={onRetry} className="retry-button">
          Try Again
        </button>
      )}
    </div>
  );
};

// Usage
const SearchWithErrorHandling: React.FC = () => {
  const { error, search, loading } = useSearchState();

  const handleRetry = () => {
    if (searchQuery) {
      search(buildSearchRequest(searchQuery));
    }
  };

  return (
    <div>
      <SearchError error={error} onRetry={handleRetry} />
      {/* Rest of search UI */}
    </div>
  );
};
```

## TypeScript Types

```typescript
// Complete type definitions for frontend use

type SearchType = 'All' | 'Posts' | 'StudyMaterials';
type MatchType = 'Keyword' | 'Semantic';
type SortOption = 'relevance' | 'created' | 'updated' | 'views';

interface SearchRequestDTO {
  query: string;
  searchType?: SearchType;
  categoryIds?: Guid[];
  materialCategoryIds?: Guid[];
  isQuestion?: boolean;
  tags?: string[];
  includeKeywordSearch?: boolean;
  includeSemanticSearch?: boolean;
  sortBy?: SortOption;
  sortOrder?: 'asc' | 'desc';
  fromDate?: Date;
  toDate?: Date;
}

interface SearchResultDTO {
  posts: PostSearchResultDTO[];
  studyMaterials: StudyMaterialSearchResultDTO[];
  totalPosts: number;
  totalStudyMaterials: number;
  totalResults: number;
  facets: SearchFacetsDTO;
  suggestions: SearchSuggestionDTO;
  queryTime: number; // in milliseconds
}

interface AuthorDTO {
  id: Guid;
  username: string;
  displayName: string;
  avatar?: string;
}

interface PostSearchResultDTO {
  id: Guid;
  title: string;
  content: string;
  summary?: string;
  isQuestion: boolean;
  isSolved: boolean;
  viewCount: number;
  likeCount: number;
  commentCount: number;
  createdAt: Date;
  updatedAt: Date;
  author: AuthorDTO;
  categories: CategoryDTO[];
  tags: string[];
  relevanceScore: number;
  matchType: MatchType;
  highlightedTitle: string;
  highlightedContent: string;
}

interface StudyMaterialSearchResultDTO {
  id: Guid;
  title: string;
  description?: string;
  summary?: string;
  type: string;
  url?: string;
  downloadUrl?: string;
  viewCount: number;
  downloadCount: number;
  averageRating: number;
  reviewCount: number;
  createdAt: Date;
  updatedAt: Date;
  uploader: AuthorDTO;
  category: MaterialCategoryDTO;
  tags: string[];
  relevanceScore: number;
  matchType: MatchType;
  highlightedTitle: string;
  highlightedDescription: string;
  isApproved: boolean;
  aiConfidence?: number;
}

interface PostSuggestionDTO {
  id: Guid;
  title: string;
  isQuestion: boolean;
  categories: string[]; // Currently empty due to backend changes
}

interface StudyMaterialSuggestionDTO {
  id: Guid;
  title: string;
  type: string;
  isApproved: boolean;
}
```

## Testing

### 1. Unit Test Components

```typescript
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { SearchComponent } from './SearchComponent';

describe('SearchComponent', () => {
  it('should handle search input', async () => {
    render(<SearchComponent />);

    const input = screen.getByPlaceholderText(/search/i);
    const button = screen.getByRole('button', { name: /search/i });

    // Type search query
    fireEvent.change(input, { target: { value: 'react tutorial' } });

    // Click search button
    fireEvent.click(button);

    // Wait for loading state
    await waitFor(() => {
      expect(screen.getByText(/searching/i)).toBeInTheDocument();
    });
  });

  it('should display search results', async () => {
    // Mock successful search response
    const mockResults: SearchResultDTO = {
      posts: [
        {
          id: 'test-id',
          title: 'React Tutorial',
          content: 'Learn React basics',
          relevanceScore: 0.8,
          matchType: 'Keyword' as const,
          // ... other required fields
        }
      ],
      studyMaterials: [],
      totalPosts: 1,
      totalStudyMaterials: 0,
      totalResults: 1,
      facets: { categories: [], materialCategories: [] },
      suggestions: { posts: [], studyMaterials: [] },
      queryTime: 150
    };

    jest.spyOn(global, 'fetch').mockResolvedValue({
      ok: true,
      json: async () => mockResults
    });

    render(<SearchComponent />);

    // Perform search
    const input = screen.getByPlaceholderText(/search/i);
    const button = screen.getByRole('button', { name: /search/i });

    fireEvent.change(input, { target: { value: 'react' } });
    fireEvent.click(button);

    // Wait for results
    await waitFor(() => {
      expect(screen.getByText('React Tutorial')).toBeInTheDocument();
    });
  });
});
```

### 2. Integration Test Search Flow

```typescript
describe('Search Integration', () => {
  it('should perform end-to-end search', async () => {
    // Navigate to search page
    cy.visit('/search');

    // Enter search query
    cy.get('[data-testid="search-input"]').type('javascript programming{enter}');

    // Wait for results
    cy.get('[data-testid="search-results"]').should('be.visible');

    // Verify results contain expected items
    cy.get('[data-testid="search-results"]').within(() => {
      cy.get('.post-card').should('have.length.gt', 0);
      cy.get('.material-card').should('have.length.gt', 0);
    });

    // Test filters
    cy.get('[data-testid="search-type-filter"]').select('Posts');
    cy.get('.post-card').should('be.visible');
    cy.get('.material-card').should('not.exist');

    // Test sorting
    cy.get('[data-testid="sort-filter"]').select('Newest');
    cy.get('.post-card')
      .first()
      .find('.post-date')
      .should('contain', new Date().getFullYear());
  });
});
```

## Best Practices

1. **Always debounce search input** to avoid excessive API calls
2. **Provide loading states** for better UX
3. **Cache search results** when appropriate
4. **Handle empty states** with helpful suggestions
5. **Use semantic HTML** for accessibility
6. **Implement proper error boundaries**
7. **Optimize for performance** with virtualization for large result sets
8. **Test various search scenarios** including edge cases

## Migration from Pagination

Since pagination has been removed from the backend:

1. **Remove all pagination UI elements** (page numbers, load more buttons)
2. **Display all results returned** from the backend
3. **Implement client-side filtering/sorting** if needed for large datasets
4. **Consider infinite scroll** or virtualization for performance with very large result sets
5. **Update any existing pagination logic** to handle complete result sets