using Microsoft.EntityFrameworkCore;
using SmartPathBackend.Data;
using SmartPathBackend.Interfaces.Repositories;
using SmartPathBackend.Models.Entities;
using System.Collections.Generic;

namespace SmartPathBackend.Repositories
{
    public class MaterialRepository : BaseRepository<Material>, IMaterialRepository
    {
        public MaterialRepository(SmartPathDbContext ctx) : base(ctx) { }

        public async Task<IReadOnlyList<Material>> GetByPostAsync(Guid postId) =>
            await _dbSet.Where(x => x.PostId == postId).OrderByDescending(x => x.UploadedAt).ToListAsync();

        public async Task<IReadOnlyList<Material>> GetByCommentAsync(Guid commentId) =>
            await _dbSet.Where(x => x.CommentId == commentId).OrderByDescending(x => x.UploadedAt).ToListAsync();

        public async Task<IReadOnlyList<Material>> GetByMessageAsync(Guid messageId) =>
            await _dbSet.Where(x => x.MessageId == messageId).OrderByDescending(x => x.UploadedAt).ToListAsync();
    }
}
