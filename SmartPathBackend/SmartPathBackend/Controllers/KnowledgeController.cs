using Pgvector;
using Microsoft.AspNetCore.Mvc;
using SmartPathBackend.Interfaces.Services;

namespace SmartPathBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class KnowledgeController : ControllerBase
    {
        private readonly IKnowledgeIngestService _ingest;
        private readonly IKnowledgeSearchService _search;

        public KnowledgeController(IKnowledgeIngestService ingest, IKnowledgeSearchService search)
        {
            _ingest = ingest;
            _search = search;
        }

        [HttpPost("ingest/pdf")]
        public async Task<IActionResult> IngestPdf([FromBody] string url, CancellationToken ct)
        {
            var id = await _ingest.IngestPdfUrlAsync(url, null, ct);
            return Ok(new { documentId = id });
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchByVector(
        [FromQuery] float[] v,
        [FromQuery] int k = 5,
        CancellationToken ct = default)
        {
            if (v is null || v.Length == 0) return BadRequest("Query 'v' (comma-separated floats) is required.");
            if (k <= 0) return BadRequest("k must be > 0");

            var vec = new Vector(v);
            var hits = await _search.SearchByVectorAsync(vec, k, ct);
            return Ok(hits);
        }
    }
}
