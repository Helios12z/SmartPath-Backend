using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using SmartPathBackend.Interfaces.Services;

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

        [HttpPost("ingest/url")]
        public async Task<IActionResult> IngestUrl([FromBody] UrlIngestRequest req, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(req.Url))
                return BadRequest("Url is required.");

            var id = await _knowledge.IngestFromUrlAsync(req.Url, req.Title, ct);
            return Ok(new { documentId = id });
        }

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
