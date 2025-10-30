using Microsoft.EntityFrameworkCore;
using SmartPathBackend.Data;
using SmartPathBackend.Interfaces.Services;
using SmartPathBackend.Models.Entities;
using SmartPathBackend.Models.Enums;

namespace SmartPathBackend.Services
{
    public class ReputationService : IReputationService
    {
        private readonly SmartPathDbContext _db;

        private const int PostLikePerBand = 5;     
        private const int CommentLikePerBand = 3;  
        private const int DislikePerBand = 3;       

        private const int LikeBandReward = 10;       
        private const int DislikeBandPenalty = 5;   

        public ReputationService(SmartPathDbContext db)
        {
            _db = db;
        }

        public async Task ApplyForPostAsync(Guid postId)
        {
            var post = await _db.Posts
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == postId);

            if (post is null) return;

            var authorId = post.AuthorId;

            var likeCount = await _db.Reactions.CountAsync(r => r.PostId == postId && r.IsPositive);
            var dislikeCount = await _db.Reactions.CountAsync(r => r.PostId == postId && !r.IsPositive);

            var likeBands = likeCount / PostLikePerBand;
            var dislikeBands = dislikeCount / DislikePerBand;

            await ApplyBandsAsync(authorId, ContentType.Post, postId, likeBands, dislikeBands);
        }

        public async Task ApplyForCommentAsync(Guid commentId)
        {
            var cmt = await _db.Comments
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == commentId);

            if (cmt is null) return;

            var authorId = cmt.AuthorId;

            var likeCount = await _db.Reactions.CountAsync(r => r.CommentId == commentId && r.IsPositive);
            var dislikeCount = await _db.Reactions.CountAsync(r => r.CommentId == commentId && !r.IsPositive);

            var likeBands = likeCount / CommentLikePerBand;
            var dislikeBands = dislikeCount / DislikePerBand;

            await ApplyBandsAsync(authorId, ContentType.Comment, commentId, likeBands, dislikeBands);
        }

        private async Task ApplyBandsAsync(Guid authorId, ContentType type, Guid contentId, int currentLikeBands, int currentDislikeBands)
        {
            var checkpoint = await _db.ReputationCheckpoints
                .FirstOrDefaultAsync(x => x.ContentType == type && x.ContentId == contentId);

            if (checkpoint is null)
            {
                checkpoint = new ReputationCheckpoint
                {
                    UserId = authorId,
                    ContentType = type,
                    ContentId = contentId,
                    LikeBandsApplied = 0,
                    DislikeBandsApplied = 0
                };
                _db.ReputationCheckpoints.Add(checkpoint);
            }

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == authorId);
            if (user is null)
            {
                checkpoint.LikeBandsApplied = currentLikeBands;
                checkpoint.DislikeBandsApplied = currentDislikeBands;
                checkpoint.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
                return;
            }

            var deltaLikeBands = currentLikeBands - checkpoint.LikeBandsApplied;
            var deltaDislikeBands = currentDislikeBands - checkpoint.DislikeBandsApplied;

            if (deltaLikeBands != 0)
            {
                user.Point += deltaLikeBands * LikeBandReward;
            }

            if (deltaDislikeBands != 0)
            {
                user.Point -= deltaDislikeBands * DislikeBandPenalty;
            }

            checkpoint.LikeBandsApplied = currentLikeBands;
            checkpoint.DislikeBandsApplied = currentDislikeBands;
            checkpoint.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
        }
    }
}
