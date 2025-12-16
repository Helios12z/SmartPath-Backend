# Post Recommendation Engine Formula

## Overview
This document outlines the mathematical formula used to rank posts for the recommendation system. The formula balances engagement metrics with time decay, social connections, and quality to ensure relevant content appears first for each user.

## Metrics Available
- **Positive Reactions** (P): Count of positive reactions (likes, upvotes)
- **Negative Reactions** (N): Count of negative reactions (dislikes, downvotes)
- **Comment Count** (C): Number of comments on the post
- **Time Since Creation** (T): Hours elapsed since post creation
- **Author Points** (A): Reputation points of the post author

## Core Formula

The recommendation score **S** for a post is calculated as:

```
S = E × D × A_w × F_b
```

Where:

### 1. Engagement Score (E)
```
E = (P + 2C) × log(1 + P + C + 1) × (1 - N_penalty)
```

- **P**: Positive reaction count
- **C**: Comment count (weighted 2x to encourage discussion)
- **log()**: Logarithmic scaling prevents runaway growth
- **N_penalty**: Negative reaction penalty factor

Negative reaction penalty:
```
N_penalty = min(0.7, N / (P + N + 1))
```

### 2. Time Decay Factor (D)
```
D = e^(-λ × T/24)
```

- **T**: Time since creation in hours
- **λ**: Decay rate constant (λ = 0.1 for moderate decay)
- **e^(-λ × T/24)**: Ensures posts lose about 63% of their initial weight after 7 days

### 3. Author Weight (A_w)
```
A_w = 1 + 0.1 × log(1 + A/1000)
```

- **A**: Author's reputation points
- **log()**: Scaling ensures reputation doesn't dominate the score

### 4. Friend Boost Factor (F_b)
```
F_b = {
  2.5,    if author is a mutual friend (both follow each other)
  1.8,    if author is followed by current user
  1.0     otherwise
}
```

- **Mutual Friends**: Both users follow each other (maximum boost)
- **Following**: Current user follows the author (moderate boost)
- **None**: No friendship connection (no boost)

## Example Calculations

### Fresh High-Quality Post from Non-Friend (1 hour old, 10 likes, 0 dislikes, 5 comments, author 5000 points)
```
T = 1, P = 10, N = 0, C = 5, A = 5000, F_b = 1.0

E = (10 + 2×5) × log(1 + 10 + 5 + 1) × (1 - 0)
E = 20 × log(16) × 1
E = 20 × 2.77 = 55.4

D = e^(-0.1 × 1/24) = 0.996

A_w = 1 + 0.1 × log(1 + 5000/1000) = 1.16

S = 55.4 × 0.996 × 1.16 × 1.0 = 63.9
```

### Same Post from Mutual Friend
```
All metrics same, F_b = 2.5

S = 55.4 × 0.996 × 1.16 × 2.5 = 159.7 (2.5× boost)
```

### Same Post from User You Follow
```
All metrics same, F_b = 1.8

S = 55.4 × 0.996 × 1.16 × 1.8 = 115.0 (1.8× boost)
```

### Old Viral Post from Mutual Friend (168 hours old, 100 likes, 20 dislikes, 50 comments, author 10000 points)
```
T = 168, P = 100, N = 20, C = 50, A = 10000, F_b = 2.5

N_penalty = min(0.7, 20/(100+20+1)) = 0.165

E = (100 + 2×50) × log(1 + 100 + 50 + 1) × (1 - 0.165)
E = 200 × log(151) × 0.835
E = 200 × 5.02 × 0.835 = 838

D = e^(-0.1 × 168/24) = 0.509

A_w = 1 + 0.1 × log(1 + 10000/1000) = 1.23

S = 838 × 0.509 × 1.23 × 2.5 = 1310
```

## Implementation Notes

1. **Minimum Threshold**: Posts with score < 0.1 are filtered out (lowered for users with no friends)
2. **Randomness**: Small random factor (0.95-1.05) added for variety
3. **Boost for New Posts**: Posts < 6 hours old get 1.5× boost if they have > 3 interactions
4. **Penalty for Spam**: Posts with N/P ratio > 0.7 get 0.3× penalty
5. **Friend Boost**: Only applied to authenticated users' feeds:
   - Mutual friends: 2.5× score boost (maximum priority)
   - Following: 1.8× score boost (moderate priority)
   - Anonymous users: No friend boost applied
6. **Friend Detection**: Uses Status.Accepted friendships only, checking both follower/followed relationships
7. **Exclusions**: User's own posts don't receive friend boosts
8. **Minimum Score Protection**:
   - Global minimum score of 0.2 to ensure no empty results
   - New posts (< 24 hours) with zero interactions get minimum engagement score of 0.5
   - Filter threshold lowered to 0.1 to accommodate users with no friends

## Tuning Parameters

The formula can be tuned by adjusting:
- **λ**: Time decay rate (0.05-0.2 recommended)
- **Comment weight**: Currently 2×, can be adjusted
- **Author influence**: Currently 0.1× log factor
- **Negative penalty caps**: Currently min(0.7, ...)
- **Friend boost multipliers**:
  - Mutual friend: 2.5× (can be adjusted 2.0-3.0)
  - Following: 1.8× (can be adjusted 1.5-2.5)

These parameters should be A/B tested and adjusted based on user engagement metrics and social interaction patterns.

## API Usage

```
GET /api/Post/recommendations?limit=20
```

- **Anonymous users**: Get recommendations based on global metrics only
- **Authenticated users**: Get personalized recommendations with friend boosts
- **limit**: Optional parameter (default: 20, maximum: 50)

The endpoint automatically detects user authentication status and applies appropriate scoring.