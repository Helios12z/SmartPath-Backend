using Microsoft.EntityFrameworkCore;
using Npgsql;
using SmartPathBackend.Data;
using SmartPathBackend.Interfaces.Repositories;
using SmartPathBackend.Models.DTOs;
using SmartPathBackend.Models.Entities;
using Pgvector;

namespace SmartPathBackend.Repositories
{
    public class KnowledgeRepository: IKnowledgeRepository
    {
        private readonly SmartPathDbContext _db;
        public KnowledgeRepository(SmartPathDbContext db) => _db = db;

        public async Task<Guid> AddDocumentAsync(KnowledgeDocument doc, CancellationToken ct = default)
        {
            _db.KnowledgeDocuments.Add(doc);
            await _db.SaveChangesAsync(ct);
            return doc.Id;
        }

        public async Task AddChunksAsync(IEnumerable<KnowledgeChunk> chunks, CancellationToken ct = default)
        {
            await _db.KnowledgeChunks.AddRangeAsync(chunks, ct);
            await _db.SaveChangesAsync(ct);
        }

        public async Task<List<KnowledgeChunk>> SearchByEmbeddingAsync(float[] queryVec, int topK, CancellationToken ct = default)
        {
            var vec = new Vector(queryVec);

            return await _db.KnowledgeChunks
                .FromSqlInterpolated($@"
                                        SELECT *
                                        FROM ""knowledge_chunks""
                                        ORDER BY ""Embedding"" <=> {vec}
                                        LIMIT {topK}")
                .AsNoTracking()
                .ToListAsync(ct);
        }

        public async Task<IReadOnlyList<KnowledgeSearchHit>> SearchByVectorAsync(Vector q, int k, CancellationToken ct = default)
        {
            var sql = @"
                        SELECT
                            c.""Id""          AS ""ChunkId"",
                            c.""DocumentId""  AS ""DocumentId"",
                            c.""ChunkIndex""  AS ""ChunkIndex"",
                            c.""Content""     AS ""Content"",
                            d.""Title""       AS ""Title"",
                            d.""SourceUrl""   AS ""SourceUrl"",
                            (c.""Embedding"" <=> @q) AS ""Score""
                        FROM ""knowledge_chunks"" c
                        JOIN ""knowledge_documents"" d ON d.""Id"" = c.""DocumentId""
                        ORDER BY c.""Embedding"" <=> @q
                        LIMIT @k;";

            var pVec = new NpgsqlParameter("q", q);
            var pK = new NpgsqlParameter("k", k);

            // dùng keyless DTO
            var hits = await _db.Set<KnowledgeSearchHit>()
                .FromSqlRaw(sql, pVec, pK)
                .AsNoTracking()
                .ToListAsync(ct);

            return hits;
        }
    }
}
