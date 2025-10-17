using AutoMapper;
using SmartPathBackend.Interfaces;
using SmartPathBackend.Interfaces.Services;
using SmartPathBackend.Models.DTOs;
using SmartPathBackend.Models.Entities;

namespace SmartPathBackend.Services
{
    public class PostService : IPostService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public PostService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<IEnumerable<PostResponseDto>> GetAllAsync()
        {
            var posts = await _unitOfWork.Posts.GetAllAsync();
            return _mapper.Map<IEnumerable<PostResponseDto>>(posts);
        }

        public async Task<PostResponseDto?> GetByIdAsync(Guid id)
        {
            var post = await _unitOfWork.Posts.GetByIdAsync(id);
            return post == null ? null : _mapper.Map<PostResponseDto>(post);
        }

        public async Task<IEnumerable<PostResponseDto>> GetByUserAsync(Guid userId)
        {
            var posts = await _unitOfWork.Posts.GetPostsByUserAsync(userId);
            return _mapper.Map<IEnumerable<PostResponseDto>>(posts);
        }

        public async Task<PostResponseDto> CreateAsync(Guid authorId, PostRequestDto request)
        {
            var post = new Post
            {
                Id = Guid.NewGuid(),
                AuthorId = authorId,
                Title = request.Title,
                Content = request.Content,
                CreatedAt = DateTime.UtcNow
            };
            await _unitOfWork.Posts.AddAsync(post);
            await _unitOfWork.SaveChangesAsync();
            return _mapper.Map<PostResponseDto>(post);
        }

        public async Task<PostResponseDto?> UpdateAsync(Guid postId, PostRequestDto request)
        {
            var post = await _unitOfWork.Posts.GetByIdAsync(postId);
            if (post == null) return null;

            post.Title = request.Title;
            post.Content = request.Content;
            _unitOfWork.Posts.Update(post);
            await _unitOfWork.SaveChangesAsync();

            return _mapper.Map<PostResponseDto>(post);
        }

        public async Task<bool> DeleteAsync(Guid postId)
        {
            var post = await _unitOfWork.Posts.GetByIdAsync(postId);
            if (post == null) return false;
            _unitOfWork.Posts.Remove(post);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }
    }
}
