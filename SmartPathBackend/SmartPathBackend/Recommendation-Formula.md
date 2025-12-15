# Post Recommendation Engine Formula

## Overview
This document outlines the mathematical formula used to rank posts for the recommendation system. The formula balances engagement metrics with time decay to ensure fresh, high-quality content appears first.

## Metrics Available
- **Positive Reactions** (P): Count of positive reactions (likes, upvotes)
- **Negative Reactions** (N): Count of negative reactions (dislikes, downvotes)
- **Comment Count** (C): Number of comments on the post
- **Time Since Creation** (T): Hours elapsed since post creation
- **Author Points** (A): Reputation points of the post author

## Core Formula

The recommendation score **S** for a post is calculated as:

```
S = E × D × A_w
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

## Example Calculations

### Fresh High-Quality Post (1 hour old, 10 likes, 0 dislikes, 5 comments, author 5000 points)
```
T = 1, P = 10, N = 0, C = 5, A = 5000

E = (10 + 2×5) × log(1 + 10 + 5 + 1) × (1 - 0)
E = 20 × log(16) × 1
E = 20 × 2.77 = 55.4

D = e^(-0.1 × 1/24) = 0.996

A_w = 1 + 0.1 × log(1 + 5000/1000) = 1.16

S = 55.4 × 0.996 × 1.16 = 63.9
```

### Old Viral Post (168 hours old, 100 likes, 20 dislikes, 50 comments, author 10000 points)
```
T = 168, P = 100, N = 20, C = 50, A = 10000

N_penalty = min(0.7, 20/(100+20+1)) = 0.165

E = (100 + 2×50) × log(1 + 100 + 50 + 1) × (1 - 0.165)
E = 200 × log(151) × 0.835
E = 200 × 5.02 × 0.835 = 838

D = e^(-0.1 × 168/24) = 0.509

A_w = 1 + 0.1 × log(1 + 10000/1000) = 1.23

S = 838 × 0.509 × 1.23 = 524
```

## Implementation Notes

1. **Minimum Threshold**: Posts with score < 1.0 are filtered out
2. **Randomness**: Small random factor (0.95-1.05) added for variety
3. **Boost for New Posts**: Posts < 6 hours old get 1.5× boost if they have > 3 interactions
4. **Penalty for Spam**: Posts with N/P ratio > 0.7 get 0.3× penalty

## Tuning Parameters

The formula can be tuned by adjusting:
- **λ**: Time decay rate (0.05-0.2 recommended)
- **Comment weight**: Currently 2×, can be adjusted
- **Author influence**: Currently 0.1× log factor
- **Negative penalty caps**: Currently min(0.7, ...)

These parameters should be A/B tested and adjusted based on user engagement metrics.