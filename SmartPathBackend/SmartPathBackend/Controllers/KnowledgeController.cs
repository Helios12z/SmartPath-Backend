using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using SmartPathBackend.Interfaces.Services;
using SmartPathBackend.Models.DTOs;

namespace SmartPathBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class KnowledgeController : ControllerBase
    {
        private readonly IKnowledgeService _knowledge;
        private readonly FileExtensionContentTypeProvider _contentTypes = new();

        public KnowledgeController(IKnowledgeService knowledge)
        {
            _knowledge = knowledge;
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("ingest/text")]
        public async Task<IActionResult> IngestText([FromBody] TextIngestRequest req, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(req.Title))
                return BadRequest("Title is required.");
            if (string.IsNullOrWhiteSpace(req.Text))
                return BadRequest("Text is required.");

            var id = await _knowledge.IngestRawAsync(req.Title, req.SourceUrl, req.Text, ct);
            return Ok(new { documentId = id });
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("ingest/url")]
        public async Task<IActionResult> IngestUrl([FromBody] UrlIngestRequest req, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(req.Url))
                return BadRequest("Url is required.");

            var id = await _knowledge.IngestFromUrlAsync(req.Url, req.Title, ct);
            return Ok(new { documentId = id });
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("ingest/files")]
        [RequestSizeLimit(50_000_000)] 
        public async Task<IActionResult> IngestFiles([FromForm] FileIngestForm form, CancellationToken ct)
        {
            if (form.Files is null || form.Files.Count == 0)
                return BadRequest("No files uploaded.");

            var results = new List<object>();

            foreach (var file in form.Files)
            {
                if (file.Length == 0) continue;

                string fileName = file.FileName ?? "uploaded";
                string title = form.Title ?? System.IO.Path.GetFileNameWithoutExtension(fileName);
                string? contentType = file.ContentType;

                if (string.IsNullOrWhiteSpace(contentType) &&
                    _contentTypes.TryGetContentType(fileName, out var guessed))
                {
                    contentType = guessed;
                }

                await using var stream = file.OpenReadStream();
                var id = await _knowledge.IngestFileAsync(
                    title: title,
                    sourceUrl: form.SourceUrl,
                    fileStream: stream,
                    contentType: contentType,
                    fileName: fileName,
                    ct: ct
                );
                results.Add(new { file = fileName, documentId = id });
            }

            return Ok(results);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("documents")]
        public async Task<IActionResult> ListDocuments(
            [FromQuery] string? q = null,
            CancellationToken ct = default)
        {
            var result = await _knowledge.GetDocumentsAsync(q, ct);
            return Ok(result);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("documents/{id:guid}")]
        public async Task<IActionResult> GetDocument(Guid id, CancellationToken ct)
        {
            var doc = await _knowledge.GetDocumentAsync(id, ct);
            if (doc == null) return NotFound();
            return Ok(doc);
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("documents/{id:guid}")]
        public async Task<IActionResult> UpdateDocument(Guid id, [FromBody] KnowledgeDocumentUpdateRequest req, CancellationToken ct)
        {
            var ok = await _knowledge.UpdateDocumentAsync(id, req, ct);
            if (!ok) return NotFound();
            return NoContent();
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("documents/{id:guid}")]
        public async Task<IActionResult> DeleteDocument(Guid id, CancellationToken ct)
        {
            var ok = await _knowledge.DeleteDocumentAsync(id, ct);
            if (!ok) return NotFound();
            return NoContent();
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("preview/text")]
        public async Task<IActionResult> PreviewText([FromBody] TextIngestRequest req, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(req.Title))
                return BadRequest("Title is required.");

            var preview = await _knowledge.PreviewByMetadataAsync(req.Title, req.SourceUrl, ct);
            return Ok(preview);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("preview/url")]
        public async Task<IActionResult> PreviewUrl([FromBody] UrlIngestRequest req, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(req.Url))
                return BadRequest("Url is required.");

            // Dùng title nếu có, nếu không thì guess theo URL
            string? title = req.Title;
            if (string.IsNullOrWhiteSpace(title))
            {
                try
                {
                    var uri = new Uri(req.Url);
                    title = Path.GetFileNameWithoutExtension(uri.LocalPath);
                }
                catch
                {
                    title = req.Url;
                }
            }

            var preview = await _knowledge.PreviewByMetadataAsync(title, req.Url, ct);
            return Ok(preview);
        }


        [Authorize(Roles = "Admin")]
        [HttpPost("preview/files")]
        [RequestSizeLimit(50_000_000)]
        public async Task<IActionResult> PreviewFiles([FromForm] FileIngestForm form, CancellationToken ct)
        {
            if (form.Files is null || form.Files.Count == 0)
                return BadRequest("No files uploaded.");

            var results = new List<KnowledgePreviewResultDTO>();

            foreach (var file in form.Files)
            {
                if (file.Length == 0) continue;

                var fileName = file.FileName ?? "uploaded";
                var title = form.Title ?? Path.GetFileNameWithoutExtension(fileName);
                var preview = await _knowledge.PreviewByMetadataAsync(title, form.SourceUrl, ct);
                results.Add(preview);
            }

            return Ok(results);
        }

    }

    public record TextIngestRequest(string Title, string? SourceUrl, string Text);
    public record UrlIngestRequest(string Url, string? Title);

    public class FileIngestForm
    {
        public string? Title { get; set; }
        public string? SourceUrl { get; set; }
        public List<IFormFile>? Files { get; set; }
    }
}
