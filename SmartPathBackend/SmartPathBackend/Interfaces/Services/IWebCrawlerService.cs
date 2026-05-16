using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SmartPathBackend.Interfaces.Services
{
    public class CrawledPage
    {
        public string Url { get; set; } = default!;
        public string Title { get; set; } = default!;
        public string Content { get; set; } = default!;
        public string? ContentType { get; set; }
    }

    public interface IWebCrawlerService
    {
        /// <summary>
        /// Crawls a URL and its nested links up to a specified depth.
        /// </summary>
        /// <param name="url">The starting URL.</param>
        /// <param name="maxDepth">Maximum depth of recursion (0 = only the provided URL).</param>
        /// <param name="maxPages">Maximum total pages to crawl across all levels.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>A list of successfully crawled pages.</returns>
        Task<List<CrawledPage>> CrawlAsync(string url, int maxDepth = 1, int maxPages = 20, CancellationToken ct = default);
    }
}
