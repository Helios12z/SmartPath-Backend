using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SmartPathBackend.Interfaces;
using SmartPathBackend.Interfaces.Services;
using SmartPathBackend.Models.DTOs;
using SmartPathBackend.Models.Entities;

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

                ReactionCount = p.Reactions != null ? p.Reactions.Count() : 0,

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

        public async Task<IEnumerable<PostResponseDto>> GetAllAsync(Guid? currentUserId)
        {
            var q = _unitOfWork.Posts.Query()
                        .AsNoTracking()
                        .Where(p => p.IsDeletedAt == null)
                        .Include(p => p.Author)
                        .Include(p => p.Reactions)
                        .Include(p => p.Comments)
                        .Include(p => p.CategoryPosts)!.ThenInclude(cp => cp.Category);

            return await ProjectToDto(q, currentUserId).ToListAsync();
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

        public async Task<IEnumerable<PostResponseDto>> GetByUserAsync(Guid userId)
        {
            var q = _unitOfWork.Posts.Query()
                        .AsNoTracking()
                        .Where(p => p.AuthorId == userId && p.IsDeletedAt == null)
                        .Include(p => p.Author)
                        .Include(p => p.Reactions)
                        .Include(p => p.Comments)
                        .Include(p => p.CategoryPosts)!.ThenInclude(cp => cp.Category);

            return await ProjectToDto(q, userId).ToListAsync();
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
    }
}
