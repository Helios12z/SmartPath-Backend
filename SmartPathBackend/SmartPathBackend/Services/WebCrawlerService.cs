using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using SmartPathBackend.Interfaces.Services;
using SmartPathBackend.Utils;
using System.Collections.Concurrent;
using System.Text;

namespace SmartPathBackend.Services
{
    public class WebCrawlerService : IWebCrawlerService
    {
        private readonly ILogger<WebCrawlerService> _logger;
        private readonly HttpClient _http;

        public WebCrawlerService(ILogger<WebCrawlerService> logger, IHttpClientFactory httpFactory)
        {
            _logger = logger;
            _http = httpFactory.CreateClient("WebCrawler");
            _http.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; SmartPathBot/1.0)");
        }

        private class CrawlContext
        {
            public int PageCount;
            public readonly int MaxPages;
            public readonly ConcurrentDictionary<string, byte> Visited = new(StringComparer.OrdinalIgnoreCase);
            public readonly ConcurrentBag<CrawledPage> Results = new();

            public CrawlContext(int maxPages) => MaxPages = maxPages;
        }

        public async Task<List<CrawledPage>> CrawlAsync(string url, int maxDepth = 1, int maxPages = 20, CancellationToken ct = default)
        {
            var ctx = new CrawlContext(maxPages);
            await CrawlInternalAsync(url, 0, maxDepth, ctx, ct);
            return ctx.Results.ToList();
        }

        private async Task CrawlInternalAsync(
            string url,
            int depth,
            int maxDepth,
            CrawlContext ctx,
            CancellationToken ct)
        {
            if (depth > maxDepth || ctx.PageCount >= ctx.MaxPages || !ctx.Visited.TryAdd(url, 0))
            {
                return;
            }

            try
            {
                _logger.LogInformation("Crawling URL: {Url} (Depth: {Depth})", url, depth);

                using var res = await _http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
                if (!res.IsSuccessStatusCode) return;

                var contentType = res.Content.Headers.ContentType?.MediaType;
                var fileName = Path.GetFileName(new Uri(url).LocalPath);
                if (string.IsNullOrEmpty(fileName)) fileName = "download.html";

                // Read all bytes once to avoid "stream already consumed" error
                var contentBytes = await res.Content.ReadAsByteArrayAsync(ct);
                var isHtml = IsHtml(contentType, url);

                if (isHtml)
                {
                    Interlocked.Increment(ref ctx.PageCount);
                    
                    var html = Encoding.UTF8.GetString(contentBytes);
                    
                    var doc = new HtmlDocument();
                    doc.LoadHtml(html);

                    var title = doc.DocumentNode.SelectSingleNode("//title")?.InnerText?.Trim() ?? url;
                    var text = DocumentExtractor.HtmlToPlainText(html);

                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        ctx.Results.Add(new CrawledPage
                        {
                            Url = url,
                            Title = title,
                            Content = text,
                            ContentType = contentType
                        });
                    }

                    // If we still have depth, find child links
                    if (depth < maxDepth && ctx.PageCount < ctx.MaxPages)
                    {
                        var childLinks = ExtractLinks(doc, url);
                        var tasks = new List<Task>();

                        foreach (var childUrl in childLinks)
                        {
                            if (ctx.PageCount >= ctx.MaxPages) break;
                            tasks.Add(CrawlInternalAsync(childUrl, depth + 1, maxDepth, ctx, ct));
                        }

                        await Task.WhenAll(tasks);
                    }
                }
                else if (IsSupportedDocument(contentType, url))
                {
                    Interlocked.Increment(ref ctx.PageCount);
                    
                    using var ms = new MemoryStream(contentBytes);
                    var text = await DocumentExtractor.ExtractTextAsync(ms, contentType, fileName, ct);
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        ctx.Results.Add(new CrawledPage
                        {
                            Url = url,
                            Title = fileName,
                            Content = text,
                            ContentType = contentType
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to crawl URL: {Url}", url);
            }
        }

        private static List<string> ExtractLinks(HtmlDocument doc, string baseUrl)
        {
            var result = new List<string>();
            var nodes = doc.DocumentNode.SelectNodes("//a[@href]");
            if (nodes == null) return result;

            var baseUri = new Uri(baseUrl);
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var node in nodes)
            {
                var href = node.GetAttributeValue("href", "").Trim();
                if (string.IsNullOrEmpty(href) || href.StartsWith("#") || 
                    href.StartsWith("javascript:", StringComparison.OrdinalIgnoreCase) || 
                    href.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!Uri.TryCreate(href, UriKind.Absolute, out Uri? abs))
                {
                    if (!Uri.TryCreate(baseUri, href, out abs)) continue;
                }

                if (!string.Equals(abs.Host, baseUri.Host, StringComparison.OrdinalIgnoreCase))
                    continue;

                var childUrl = abs.GetLeftPart(UriPartial.Path);
                if (seen.Add(childUrl))
                {
                    var ext = Path.GetExtension(abs.LocalPath).ToLowerInvariant();
                    if (string.IsNullOrEmpty(ext) || IsSupportedExtension(ext))
                    {
                        result.Add(childUrl);
                    }
                }
            }

            return result;
        }

        private static bool IsSupportedExtension(string ext)
        {
            return ext is ".html" or ".htm" or ".php" or ".asp" or ".aspx" or ".pdf" or ".doc" or ".docx";
        }

        private static bool IsSupportedDocument(string? contentType, string url)
        {
            if (contentType != null)
            {
                if (contentType.Contains("application/pdf") || 
                    contentType.Contains("application/msword") || 
                    contentType.Contains("application/vnd.openxmlformats-officedocument.wordprocessingml.document"))
                    return true;
            }
            var ext = Path.GetExtension(url).ToLowerInvariant();
            return ext is ".pdf" or ".doc" or ".docx";
        }

        private static bool IsHtml(string? contentType, string url)
        {
            if (contentType != null && contentType.Contains("text/html")) return true;
            var ext = Path.GetExtension(url).ToLowerInvariant();
            return string.IsNullOrEmpty(ext) || ext is ".html" or ".htm" or ".php" or ".asp" or ".aspx";
        }
    }
}
