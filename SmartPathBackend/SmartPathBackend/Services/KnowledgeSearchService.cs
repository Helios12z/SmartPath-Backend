using SmartPathBackend.Interfaces;
using SmartPathBackend.Interfaces.Repositories;
using SmartPathBackend.Interfaces.Services;
using SmartPathBackend.Models.DTOs;
using Pgvector;
using SmartPathBackend.Data;
using Microsoft.EntityFrameworkCore;

namespace SmartPathBackend.Services
{
    public sealed class KnowledgeSearchService : IKnowledgeSearchService
    {
        private readonly IEmbedderService _embedder;
        private readonly IUnitOfWork _uow;
        private readonly SmartPathDbContext _db;

        public KnowledgeSearchService(IEmbedderService embedder, IUnitOfWork uow, SmartPathDbContext db)
        {
            _embedder = embedder;
            _uow = uow;
            _db = db;
        }

        public async Task<IReadOnlyList<KnowledgeSearchHit>> SearchByVectorAsync(Vector q, int k, CancellationToken ct = default)
        {
            FormattableString sql = $@"
                                        SELECT
                                            c.""Id""          AS ""ChunkId"",
                                            c.""DocumentId""  AS ""DocumentId"",
                                            c.""ChunkIndex""  AS ""ChunkIndex"",
                                            c.""Content""     AS ""Content"",
                                            d.""Title""       AS ""Title"",
                                            d.""SourceUrl""   AS ""SourceUrl"",
                                            (c.""Embedding"" <=> {q}) AS ""Score""
                                        FROM ""knowledge_chunks"" c
                                        JOIN ""knowledge_documents"" d ON d.""Id"" = c.""DocumentId""
                                        ORDER BY ""Score""
                                        LIMIT {k}";

            return await _db.Database
                .SqlQuery<KnowledgeSearchHit>(sql) 
                .AsNoTracking()
                .ToListAsync(ct);
        }
    }
}
