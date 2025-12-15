# Study Material Rating System Implementation

## Overview
Implemented a comprehensive rating system for study materials that allows users to rate and provide feedback on educational content.

## Components Created

### 1. StudyMaterialRating Entity
- **Location**: `Models/Entities/StudyMaterialRating.cs`
- **Fields**:
  - `MaterialId`: Reference to the study material
  - `UserId`: User who provided the rating
  - `Rating`: 1-5 star rating
  - `Comment`: Optional text feedback
  - `CreatedAt`: Rating timestamp
  - `UpdatedAt`: For rating updates

### 2. DTOs
- **Location**: `Models/DTOs/StudyMaterialRatingDTOs.cs`
- **Types**:
  - `StudyMaterialRatingRequest`: For submitting/updating ratings
  - `StudyMaterialRatingResponse`: For returning rating data
  - `StudyMaterialRatingStats`: For rating statistics

### 3. Database Configuration
- **Unique constraint**: One rating per user per material
- **Check constraint**: Rating must be between 1 and 5
- **Indexes**: Optimized for common queries

### 4. API Endpoints

#### Rating Statistics
```
GET /api/studymaterial/{id}/ratings/stats
- Returns average rating, total count, and distribution
- Public access (no authentication required)
```

#### Rate Material
```
POST /api/studymaterial/{id}/ratings
- Submit or update a rating
- Requires authentication
- Only allowed for accepted materials
```

#### Get Material Ratings
```
GET /api/studymaterial/{id}/ratings
- Get paginated list of all ratings for a material
- Public access
- Includes user feedback
```

#### Get User Rating
```
GET /api/studymaterial/{id}/ratings/my
- Get current user's rating for a material
- Requires authentication
```

#### Delete Rating
```
DELETE /api/studymaterial/{id}/ratings
- Delete user's rating
- Requires authentication
```

## Updated Components

### StudyMaterialResponse
Added rating fields to the study material response:
- `AverageRating`: Calculated average (0.0 if no ratings)
- `TotalRatings`: Number of ratings

### StudyMaterialLibraryService
Added rating-related methods:
- `GetRatingStatsAsync()`: Calculate rating statistics
- `RateMaterialAsync()`: Add/update ratings
- `GetMaterialRatingsAsync()`: Paginated rating list
- `GetUserRatingAsync()`: Get user's rating
- `DeleteRatingAsync()`: Remove rating

## Features

### 1. Rating Validation
- Ratings must be between 1 and 5 stars
- Only accepted materials can be rated
- One rating per user per material

### 2. Rating Updates
- Users can update their ratings
- Timestamp tracks when rating was last updated

### 3. Statistics
- Real-time average rating calculation
- Rating distribution (1-5 stars)
- Total rating count

### 4. Integration
- Ratings are automatically included in study material responses
- Efficient database queries with proper indexing

## Database Migration
Created migration: `AddStudyMaterialRatingSystem`
- Creates `StudyMaterialRatings` table
- Adds foreign key relationships
- Implements constraints for data integrity

## Usage Example

### Rate a study material:
```javascript
POST /api/studymaterial/{materialId}/ratings
{
  "rating": 5,
  "comment": "Excellent material! Very comprehensive."
}
```

### Get rating statistics:
```javascript
GET /api/studymaterial/{materialId}/ratings/stats

Response:
{
  "averageRating": 4.5,
  "totalRatings": 12,
  "ratingDistribution1": 0,
  "ratingDistribution2": 1,
  "ratingDistribution3": 2,
  "ratingDistribution4": 3,
  "ratingDistribution5": 6
}
```

## Benefits

1. **Quality Feedback**: Users can provide quantitative and qualitative feedback
2. **Content Discovery**: Ratings help others identify high-quality materials
3. **Community Engagement**: Encourages user participation and contribution
4. **Content Improvement**: Feedback helps material creators improve their content
5. **Trust Building**: Transparent rating system builds community trust