using Pgvector;
using Microsoft.AspNetCore.Mvc;
using SmartPathBackend.Interfaces.Services;

namespace SmartPathBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class KnowledgeController : ControllerBase
    {
        private readonly IKnowledgeService _ingest;
        private readonly IKnowledgeService _search;

        public KnowledgeController(IKnowledgeService ingest, IKnowledgeService search)
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
    }
}
