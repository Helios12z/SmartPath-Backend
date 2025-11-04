using Microsoft.AspNetCore.Http;
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
        public async Task<IActionResult> Search([FromQuery] string q, [FromQuery] int k = 5, CancellationToken ct = default)
        {
            var hits = await _search.SearchAsync(q, k, ct);
            return Ok(hits);
        }
    }
}
