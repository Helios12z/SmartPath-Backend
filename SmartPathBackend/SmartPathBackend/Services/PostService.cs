using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SmartPathBackend.Interfaces;
using SmartPathBackend.Interfaces.Services;
using SmartPathBackend.Models.DTOs;
using SmartPathBackend.Models.Entities;
using SmartPathBackend.Models.Enums;

namespace SmartPathBackend.Services
{
    public class PostService : IPostService
    {
        private readonly IUnitOfWork _unitOfWork;

        public PostService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        private static IQueryable<PostResponseDto> ProjectToDto(IQueryable<Post> query, Guid? currentUserId)
        {
            return query.Select(p => new PostResponseDto
            {
                Id = p.Id,
                Title = p.Title,
                Content = p.Content,
                IsQuestion = p.IsQuestion,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt,

                AuthorUsername = p.Author.Username,
                AuthorAvatarUrl = p.Author.AvatarUrl,
                AuthorId = p.Author.Id,
                AuthorPoint= p.Author.Point,

                PositiveReactionCount = p.Reactions != null ? p.Reactions.Count(r => r.IsPositive) : 0,
                NegativeReactionCount = p.Reactions != null ? p.Reactions.Count(r => !r.IsPositive) : 0,

                CommentCount = p.Comments != null ? p.Comments.Count() : 0,

                Categories = p.CategoryPosts != null
                    ? p.CategoryPosts.Select(cp => cp.Category.Name).ToList()
                    : new List<string>(),

                IsPositiveReacted = currentUserId.HasValue
                    ? p.Reactions!.Any(r => r.UserId == currentUserId && r.IsPositive)
                    : (bool?)null,

                IsNegativeReacted = currentUserId.HasValue
                    ? p.Reactions!.Any(r => r.UserId == currentUserId && !r.IsPositive)
                    : (bool?)null,
            });
        }

        public async Task<(IEnumerable<PostResponseDto> Items, int Total)> GetAllAsync(Guid? currentUserId, int page = 1, int pageSize = 20)
        {
            // Ensure valid page parameters
            page = Math.Max(1, page);
            pageSize = Math.Min(Math.Max(1, pageSize), 100); // Max 100 items per page

            var q = _unitOfWork.Posts.Query()
                        .AsNoTracking()
                        .Where(p => p.IsDeletedAt == null)
                        .Include(p => p.Author)
                        .Include(p => p.Reactions)
                        .Include(p => p.Comments)
                        .Include(p => p.CategoryPosts)!.ThenInclude(cp => cp.Category);

            var total = await q.CountAsync();
            var items = await ProjectToDto(q, currentUserId)
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, total);
        }

        public async Task<PostResponseDto?> GetByIdAsync(Guid id, Guid? currentUserId)
        {
            var q = _unitOfWork.Posts.Query()
                        .AsNoTracking()
                        .Where(p => p.Id == id && p.IsDeletedAt == null)
                        .Include(p => p.Author)
                        .Include(p => p.Reactions)
                        .Include(p => p.Comments)
                        .Include(p => p.CategoryPosts)!.ThenInclude(cp => cp.Category);

            return await ProjectToDto(q, currentUserId).FirstOrDefaultAsync();
        }

        public async Task<(IEnumerable<PostResponseDto> Items, int Total)> GetByUserAsync(Guid userId, int page = 1, int pageSize = 20)
        {
            // Ensure valid page parameters
            page = Math.Max(1, page);
            pageSize = Math.Min(Math.Max(1, pageSize), 100); // Max 100 items per page

            var q = _unitOfWork.Posts.Query()
                        .AsNoTracking()
                        .Where(p => p.AuthorId == userId && p.IsDeletedAt == null)
                        .Include(p => p.Author)
                        .Include(p => p.Reactions)
                        .Include(p => p.Comments)
                        .Include(p => p.CategoryPosts)!.ThenInclude(cp => cp.Category);

            var total = await q.CountAsync();
            var items = await ProjectToDto(q, userId)
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, total);
        }

        public async Task<PostResponseDto> CreateAsync(Guid authorId, PostRequestDto request)
        {
            var now = DateTime.UtcNow;

            var post = new Post
            {
                Id = Guid.NewGuid(),
                AuthorId = authorId,
                Title = request.Title,
                Content = request.Content,
                IsQuestion = request.IsQuestion,
                CreatedAt = now
            };

            if (request.CategoryIds is { Count: > 0 })
            {
                post.CategoryPosts = request.CategoryIds.Select(cid => new CategoryPost
                {
                    PostId = post.Id,
                    CategoryId = cid
                }).ToList();
            }

            await _unitOfWork.Posts.AddAsync(post);
            await _unitOfWork.SaveChangesAsync();

            var q = _unitOfWork.Posts.Query()
                        .AsNoTracking()
                        .Where(p => p.Id == post.Id)
                        .Include(p => p.Author)
                        .Include(p => p.Reactions)
                        .Include(p => p.Comments)
                        .Include(p => p.CategoryPosts)!.ThenInclude(cp => cp.Category);

            return await ProjectToDto(q, authorId).FirstAsync();
        }

        public async Task<PostResponseDto?> UpdateAsync(Guid postId, PostRequestDto request, Guid? currentUserId)
        {
            var post = await _unitOfWork.Posts.GetByIdAsync(postId);
            if (post == null || post.IsDeletedAt != null) return null;

            if (currentUserId!=post.AuthorId) throw new UnauthorizedAccessException("Not authorized to update this post");

            post.Title = request.Title;
            post.Content = request.Content;
            post.IsQuestion = request.IsQuestion;
            post.UpdatedAt = DateTime.UtcNow;

            if (request.CategoryIds != null)
            {
                post.CategoryPosts ??= new List<CategoryPost>();
                post.CategoryPosts.Clear();
                foreach (var cid in request.CategoryIds)
                {
                    post.CategoryPosts.Add(new CategoryPost { PostId = post.Id, CategoryId = cid });
                }
            }

            _unitOfWork.Posts.Update(post);
            await _unitOfWork.SaveChangesAsync();

            var q = _unitOfWork.Posts.Query()
                        .AsNoTracking()
                        .Where(p => p.Id == postId)
                        .Include(p => p.Author)
                        .Include(p => p.Reactions)
                        .Include(p => p.Comments)
                        .Include(p => p.CategoryPosts)!.ThenInclude(cp => cp.Category);

            return await ProjectToDto(q, currentUserId).FirstOrDefaultAsync();
        }

        public async Task<bool> DeleteAsync(Guid postId)
        {
            var post = await _unitOfWork.Posts.GetByIdAsync(postId);
            if (post == null) return false;

            post.IsDeletedAt = DateTime.UtcNow;
            _unitOfWork.Posts.Update(post);

            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<PostResponseDto>> GetRecommendationsAsync(Guid? currentUserId, int? limit = null)
        {
            var limitToUse = Math.Min(limit ?? 20, 50); // Default to 20, max 50

            var q = _unitOfWork.Posts.Query()
                        .AsNoTracking()
                        .Where(p => p.IsDeletedAt == null)
                        .Include(p => p.Author)
                        .Include(p => p.Reactions)
                        .Include(p => p.Comments)
                        .Include(p => p.CategoryPosts)!.ThenInclude(cp => cp.Category);

            var posts = await q.ToListAsync();

            // Get friendships if user is logged in
            HashSet<Guid> followedUsers = new HashSet<Guid>();
            HashSet<Guid> mutualFriends = new HashSet<Guid>();

            if (currentUserId.HasValue)
            {
                var friendships = await _unitOfWork.Friendships.Query()
                    .AsNoTracking()
                    .Where(f => f.Status == Status.Accepted &&
                               (f.FollowerId == currentUserId.Value || f.FollowedUserId == currentUserId.Value))
                    .ToListAsync();

                followedUsers = friendships
                    .Where(f => f.FollowerId == currentUserId.Value)
                    .Select(f => f.FollowedUserId)
                    .ToHashSet();

                var usersFollowingMe = friendships
                    .Where(f => f.FollowedUserId == currentUserId.Value)
                    .Select(f => f.FollowerId)
                    .ToHashSet();

                // Mutual friends = intersection of people I follow AND people who follow me
                mutualFriends = followedUsers
                    .Intersect(usersFollowingMe)
                    .ToHashSet();
            }

            var recommendations = CalculateRecommendationScores(posts, currentUserId, followedUsers, mutualFriends)
                .Where(p => p.Score > 0.1) // Lowered minimum threshold for users with no friends
                .OrderByDescending(p => p.Score)
                .Take(limitToUse)
                .Select(p => p.Dto);

            return recommendations;
        }

        private static List<(PostResponseDto Dto, double Score)> CalculateRecommendationScores(
            List<Post> posts,
            Guid? currentUserId,
            HashSet<Guid> followedUsers,
            HashSet<Guid> mutualFriends)
        {
            var result = new List<(PostResponseDto, double)>();
            var random = new Random();
            var now = DateTime.UtcNow;

            foreach (var post in posts)
            {
                // Extract metrics
                var positiveReactions = post.Reactions?.Count(r => r.IsPositive) ?? 0;
                var negativeReactions = post.Reactions?.Count(r => !r.IsPositive) ?? 0;
                var commentCount = post.Comments?.Count() ?? 0;
                var timeSinceCreationHours = (now - post.CreatedAt).TotalHours;
                var authorPoints = post.Author?.Point ?? 0;

                // Calculate Engagement Score (E)
                var totalInteractions = positiveReactions + commentCount;
                var negativePenalty = Math.Min(0.7, negativeReactions / (positiveReactions + negativeReactions + 1));
                var engagementScore = (positiveReactions + 2 * commentCount)
                    * Math.Log(1 + totalInteractions + 1)
                    * (1 - negativePenalty);

                // Ensure minimum engagement score for very new posts
                if (totalInteractions == 0 && timeSinceCreationHours < 24)
                {
                    engagementScore = Math.Max(engagementScore, 0.5); // Minimum score for new posts
                }

                // Calculate Time Decay Factor (D)
                const double lambda = 0.1; // Decay rate
                var timeDecayFactor = Math.Exp(-lambda * timeSinceCreationHours / 24);

                // Calculate Author Weight (A_w)
                var authorWeight = 1 + 0.1 * Math.Log(1 + authorPoints / 1000.0);

                // Calculate Friend Boost Factor (F_b)
                double friendBoost = 1.0;
                if (currentUserId.HasValue && post.AuthorId != currentUserId.Value)
                {
                    if (mutualFriends.Contains(post.AuthorId))
                    {
                        friendBoost = 2.5; // Mutual friend - maximum boost
                    }
                    else if (followedUsers.Contains(post.AuthorId))
                    {
                        friendBoost = 1.8; // Following - moderate boost
                    }
                }

                // Base score
                var score = engagementScore * timeDecayFactor * authorWeight * friendBoost;

                // Ensure minimum score to prevent empty results
                score = Math.Max(score, 0.2);

                // Apply boost for new posts with engagement
                if (timeSinceCreationHours < 6 && (positiveReactions + commentCount) > 3)
                {
                    score *= 1.5;
                }

                // Apply penalty for potential spam
                var negativeRatio = positiveReactions > 0 ? (double)negativeReactions / positiveReactions : 0;
                if (negativeRatio > 0.7)
                {
                    score *= 0.3;
                }

                // Add small randomness for variety
                var randomFactor = 0.95 + (random.NextDouble() * 0.10); // 0.95 to 1.05
                score *= randomFactor;

                // Create DTO
                var dto = new PostResponseDto
                {
                    Id = post.Id,
                    Title = post.Title,
                    Content = post.Content,
                    IsQuestion = post.IsQuestion,
                    CreatedAt = post.CreatedAt,
                    UpdatedAt = post.UpdatedAt,
                    AuthorUsername = post.Author?.Username ?? "Unknown",
                    AuthorAvatarUrl = post.Author?.AvatarUrl,
                    AuthorId = post.AuthorId,
                    AuthorPoint = authorPoints,
                    PositiveReactionCount = positiveReactions,
                    NegativeReactionCount = negativeReactions,
                    CommentCount = commentCount,
                    Categories = post.CategoryPosts?.Select(cp => cp.Category?.Name).Where(n => n != null).Cast<string>().ToList() ?? new List<string>(),
                    IsPositiveReacted = null, // Not calculated for recommendations
                    IsNegativeReacted = null  // Not calculated for recommendations
                };

                result.Add((dto, score));
            }

            return result;
        }
    }
}
