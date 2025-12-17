# Post AI Review System

## Overview

The Post AI Review System automatically checks posts before they are published, similar to the study material approval system. This ensures quality and appropriateness of content.

## How It Works

### 1. Post Creation Flow

When a user creates a post:
1. Post is created with `Status = Pending`
2. AI reviewer analyzes the post for:
   - **Content appropriateness** (no spam, hate speech, etc.)
   - **Category relevance** (matches selected categories?)
   - **Overall quality** (helpful, well-formatted?)

### 2. AI Decision Logic

Based on AI analysis:
- **Confidence ≥ 0.7** AND **Appropriate** → Status = Accepted ✅
- **Confidence 0.4-0.7** AND **Appropriate** → Status = Pending (needs manual review) ⏳
- **Not Appropriate** OR **Confidence < 0.4** → Status = Rejected ❌

### 3. AI Review Results Stored

For each post, the system stores:
- `AiConfidence`: AI's confidence level (0.0 - 1.0)
- `AiCategoryMatch`: Whether content matches categories
- `AiReason`: Explanation of AI's decision
- `ReviewedAt`: When AI review was completed
- `Status`: Final status (Accepted/Pending/Rejected)
- `RejectReason`: Reason for rejection (if any)

## API Changes

### Post Creation

```javascript
POST /api/posts
{
  "title": "How to implement React hooks?",
  "content": "I'm trying to understand...",
  "isQuestion": true,
  "categoryIds": ["guid-category-1", "guid-category-2"]
}
```

**Response** (includes AI review data):
```json
{
  "id": "post-guid",
  "title": "How to implement React hooks?",
  "content": "...",
  "isQuestion": true,
  "status": "Accepted",
  "rejectReason": null,
  "aiConfidence": 0.92,
  "aiCategoryMatch": true,
  "aiReason": "Post is well-structured and relevant to React programming",
  "reviewedAt": "2024-01-15T10:30:00Z",
  "createdAt": "2024-01-15T10:30:00Z",
  // ... other post fields
}
```

### Post Visibility

- **Accepted posts**: Visible to all users
- **Pending posts**: Only visible to author and admins
- **Rejected posts**: Only visible to author and admins (with reject reason)

## Admin Features

### Review Pending Posts

Admins can review posts marked as Pending:
```javascript
GET /api/posts/admin/pending
```

### Manually Update Post Status

```javascript
PUT /api/posts/{postId}/status
{
  "status": "Accepted",
  "adminNote": "Approved after manual review"
}
```

## AI Review Prompts

The AI uses these prompts for evaluation:

### System Prompt
```
You are an AI content moderator for a Q&A and educational platform. Your task is to review posts for:
1. Content appropriateness (no spam, hate speech, inappropriate content)
2. Category relevance (does the post match the selected categories?)
3. Overall quality (is the post helpful and well-formatted?)

You must respond with a JSON object in this format:
{
  "isAppropriate": true|false,
  "categoryMatch": true|false,
  "confidence": 0.0-1.0,
  "reason": "Brief explanation of your decision"
}
```

### Example User Input
```
Post Title: "How to implement React hooks?"
Is Question: true

Content Preview:
I'm trying to understand how React hooks work...

Available Categories: [{"id": "...", "name": "React"}, ...]

Selected Categories: React, JavaScript

Please review this post for appropriateness and category matching.
```

## Error Handling

If AI review fails:
- Post remains in `Pending` status
- `AiReason` = "AI review failed - requires manual review"
- Admins can review manually

## Configuration

The AI reviewer uses the same LLM service as other AI features. Configure in `appsettings.json`:

```json
{
  "LLM": {
    "Provider": "Local",
    "BaseUrl": "http://localhost:11434",
    "Model": "llama2",
    "ApiKey": null
  }
}
```

## Best Practices

1. **Monitor AI Confidence**: Keep track of confidence scores
2. **Manual Review Queue**: Regularly review Pending posts
3. **Feedback Loop**: Consider user feedback for improving AI decisions
4. **False Positives**: Have a quick appeal process for wrong rejections
5. **Performance**: AI review happens asynchronously, doesn't block post creation

## Migration Notes

Existing posts (created before this feature):
- Have `Status = Accepted` by default
- Have `AiConfidence = null`
- Will continue to work normally

New posts will go through AI review automatically.