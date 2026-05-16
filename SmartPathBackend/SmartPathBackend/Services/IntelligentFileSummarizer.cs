using DocumentFormat.OpenXml.Packaging;
using Microsoft.Extensions.Logging;
using SmartPathBackend.Interfaces.Services;
using SmartPathBackend.Models.DTOs;
using System.Text;
using System.Text.RegularExpressions;
using System.Text.Json;
using System.Xml.Linq;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Writer;

namespace SmartPathBackend.Services
{
    public class IntelligentFileSummarizer : IIntelligentFileSummarizer
    {
        private readonly ILogger<IntelligentFileSummarizer> _logger;

        public IntelligentFileSummarizer(ILogger<IntelligentFileSummarizer> logger)
        {
            _logger = logger;
        }

        public async Task<FileSummaryResult> SummarizeFileAsync(
            string title,
            string? description,
            string fileName,
            long fileSize,
            string contentType,
            Stream fileStream,
            CancellationToken ct = default)
        {
            var result = new FileSummaryResult
            {
                ContentType = DetermineContentType(contentType, fileName)
            };

            try
            {
                // Reset stream position
                if (fileStream.CanSeek)
                    fileStream.Position = 0;

                result.IsTextBased = IsTextBasedFile(contentType, fileName);

                if (!result.IsTextBased)
                {
                    // Handle non-text files (images, videos, etc.)
                    result.ProcessingNotes = "Non-text file. Content analysis based on metadata only.";
                    return result;
                }

                // Extract and analyze content
                var extractedText = await ExtractTextSmartlyAsync(fileStream, contentType, fileName, ct);
                result.ExtractedSample = extractedText.Item1;
                result.WordCount = extractedText.Item2;
                result.Language = DetectLanguage(extractedText.Item1);
                result.EstimatedReadingTime = CalculateReadingTime(result.WordCount);

                // Intelligent analysis
                result.IsEducational = AnalyzeEducationalContent(extractedText.Item1);
                result.QualityScore = AnalyzeContentQuality(extractedText.Item1);
                result.AcademicLevel = DetermineAcademicLevel(extractedText.Item1);
                result.KeyTopics = ExtractKeyTopics(extractedText.Item1);
                result.HasCode = ContainsCode(extractedText.Item1);
                result.HasFormulas = ContainsFormulas(extractedText.Item1);
                result.HasDiagrams = HasDiagramReferences(extractedText.Item1);
                result.PageCount = EstimatePageCount(contentType, fileName, extractedText.Item2);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error summarizing file {FileName}", fileName);
                result.ProcessingNotes = $"Error processing file: {ex.Message}";
            }

            return result;
        }

        public async Task<FileSummaryResult> SummarizeTextAsync(
            string title,
            string? description,
            string rawText,
            CancellationToken ct = default)
        {
            var result = new FileSummaryResult
            {
                ContentType = "Text/Web Content",
                IsTextBased = true
            };

            try
            {
                // Smart sampling
                var words = rawText.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                result.WordCount = words.Length;

                if (words.Length <= 1000)
                {
                    result.ExtractedSample = rawText;
                }
                else
                {
                    var firstPart = string.Join(" ", words.Take(300));
                    var lastPart = string.Join(" ", words.TakeLast(300));
                    result.ExtractedSample = $"{firstPart}\n...[truncated]...\n{lastPart}";
                }

                result.Language = DetectLanguage(result.ExtractedSample);
                result.EstimatedReadingTime = CalculateReadingTime(result.WordCount);
                result.IsEducational = AnalyzeEducationalContent(rawText);
                result.QualityScore = AnalyzeContentQuality(rawText);
                result.AcademicLevel = DetermineAcademicLevel(rawText);
                result.KeyTopics = ExtractKeyTopics(rawText);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error summarizing text for {Title}", title);
                result.ProcessingNotes = $"Error processing text: {ex.Message}";
            }

            return result;
        }

        public async Task<string> GenerateSummarizationPromptAsync(
            string categoryPath,
            FileSummaryResult summary,
            CancellationToken ct = default)
        {
            // Create a concise, token-efficient prompt
            var jsonFormat = "{\"categoryMatch\":true|false,\"isAppropriate\":true|false,\"confidence\":0.0-1.0,\"reason\":\"Brief explanation\"}";

            var prompt = $"""
                STUDY MATERIAL REVIEW - Token Efficient Analysis

                Category Path: {categoryPath}
                Title: {summary.ContentType} Material
                Content Summary: {summary.ExtractedSample.Substring(0, Math.Min(500, summary.ExtractedSample.Length))}...

                Key Metrics:
                - Educational: {summary.IsEducational}
                - Quality Score: {summary.QualityScore:F2}
                - Academic Level: {summary.AcademicLevel}
                - Word Count: {summary.WordCount}
                - Reading Time: {summary.EstimatedReadingTime} min
                - Topics: {string.Join(", ", summary.KeyTopics.Take(5))}
                - Has Code: {summary.HasCode}
                - Has Formulas: {summary.HasFormulas}
                - Language: {summary.Language}
                {(!string.IsNullOrEmpty(summary.ProcessingNotes) ? $"- Notes: {summary.ProcessingNotes}" : "")}

                Analysis Request:
                1. Does this belong to the specified category? (true/false)
                2. Is this appropriate for educational platform? (true/false)
                3. Quality assessment (0.0-1.0)
                4. Brief reason for decision

                Respond with JSON only:
                {jsonFormat}
                """;

            return prompt;
        }

        #region Private Helper Methods

        private string DetermineContentType(string contentType, string fileName)
        {
            if (string.IsNullOrEmpty(contentType))
            {
                var ext = Path.GetExtension(fileName).ToLowerInvariant();
                return ext switch
                {
                    ".pdf" => "PDF Document",
                    ".doc" or ".docx" => "Word Document",
                    ".ppt" or ".pptx" => "PowerPoint",
                    ".xls" or ".xlsx" => "Excel Spreadsheet",
                    ".txt" => "Text File",
                    ".md" => "Markdown",
                    ".jpg" or ".jpeg" or ".png" or ".gif" => "Image",
                    _ => "Unknown"
                };
            }

            return contentType.Split('/')[0] switch
            {
                "application" => contentType.Split('/')[1].Split('.').FirstOrDefault() ?? "Application",
                "image" => "Image",
                "text" => "Text",
                _ => contentType.Split('/')[0]
            };
        }

        private bool IsTextBasedFile(string contentType, string fileName)
        {
            var textTypes = new[]
            {
                "application/pdf",
                "application/msword",
                "application/vnd.openxmlformats-officedocument",
                "application/vnd.ms-powerpoint",
                "application/vnd.ms-excel",
                "text/plain",
                "text/html",
                "text/markdown"
            };

            return textTypes.Contains(contentType.ToLowerInvariant()) ||
                   Path.GetExtension(fileName).ToLowerInvariant() switch
                   {
                       ".pdf" or ".doc" or ".docx" or ".txt" or ".md" or ".html" => true,
                       _ => false
                   };
        }

        private async Task<(string Sample, int WordCount)> ExtractTextSmartlyAsync(
            Stream stream,
            string contentType,
            string fileName,
            CancellationToken ct)
        {
            var fullText = await ExtractFullTextAsync(stream, contentType, fileName, ct);

            // Smart sampling: get first part + last part + middle sample
            var words = fullText.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

            string sample;
            if (words.Length <= 1000)
            {
                sample = fullText;
            }
            else
            {
                var firstPart = string.Join(" ", words.Take(300));
                var middleIndex = words.Length / 2;
                var middlePart = string.Join(" ", words.Skip(middleIndex - 150).Take(300));
                var lastPart = string.Join(" ", words.TakeLast(300));

                sample = $"{firstPart}\n...[middle section]...\n{lastPart}";
            }

            return (sample, words.Length);
        }

        private async Task<string> ExtractFullTextAsync(
            Stream stream,
            string contentType,
            string fileName,
            CancellationToken ct)
        {
            try
            {
                if (stream.CanSeek)
                    stream.Position = 0;

                var ext = Path.GetExtension(fileName).ToLowerInvariant();

                // PDF extraction using PdfPig
                if (ext == ".pdf" || contentType.Contains("pdf"))
                {
                    return await ExtractPdfTextAsync(stream, ct);
                }

                // DOCX extraction
                if (ext == ".docx" || contentType.Contains("wordprocessingml.document"))
                {
                    return await ExtractDocxTextAsync(stream, ct);
                }

                // DOC extraction (older format)
                if (ext == ".doc" || contentType.Contains("msword"))
                {
                    return await ExtractDocTextAsync(stream, ct);
                }

                // XLSX extraction
                if (ext == ".xlsx" || contentType.Contains("sheet"))
                {
                    return await ExtractXlsxTextAsync(stream, ct);
                }

                // PPTX extraction
                if (ext == ".pptx" || contentType.Contains("presentation"))
                {
                    return await ExtractPptxTextAsync(stream, ct);
                }

                // Plain text files
                if (ext == ".txt" || contentType.StartsWith("text/"))
                {
                    return await ExtractPlainTextAsync(stream, ct);
                }

                // Markdown
                if (ext == ".md" || contentType == "text/markdown")
                {
                    var content = await ExtractPlainTextAsync(stream, ct);
                    return StripMarkdown(content);
                }

                // HTML
                if (ext == ".html" || ext == ".htm" || contentType.Contains("html"))
                {
                    var html = await ExtractPlainTextAsync(stream, ct);
                    return HtmlToPlainText(html);
                }

                // RTF files
                if (ext == ".rtf")
                {
                    return await ExtractRtfTextAsync(stream, ct);
                }

                // For unsupported formats, return basic info
                return $"Unsupported format for text extraction: {contentType} ({ext})";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to extract text from file {FileName}", fileName);
                return $"Text extraction failed: {ex.Message}";
            }
        }

        #region Format-Specific Text Extraction Methods

        private async Task<string> ExtractPdfTextAsync(Stream stream, CancellationToken ct)
        {
            try
            {
                // Ensure stream is at the beginning
                if (stream.CanSeek)
                    stream.Position = 0;

                // For PDF, we need to read the bytes first to avoid stream issues
                byte[] pdfBytes;
                if (stream is MemoryStream ms)
                {
                    pdfBytes = ms.ToArray();
                }
                else
                {
                    using var memoryStream = new MemoryStream();
                    await stream.CopyToAsync(memoryStream, ct);
                    pdfBytes = memoryStream.ToArray();
                }

                // Open PDF from bytes
                using var document = PdfDocument.Open(pdfBytes);
                var textBuilder = new StringBuilder();
                var pageCount = 0;

                foreach (var page in document.GetPages())
                {
                    ct.ThrowIfCancellationRequested();
                    pageCount++;

                    // Extract text with words
                    var words = page.GetWords();
                    var pageText = string.Join(" ", words.Select(w => w.Text));

                    // PdfPig doesn't have built-in table extraction, but we can check for table-like structures
                    // Tables are typically identified by regular spacing patterns

                    textBuilder.AppendLine(pageText);
                    textBuilder.AppendLine(); // Page separator

                    // Limit to first 50 pages for performance
                    if (pageCount >= 50) break;
                }

                var extractedText = textBuilder.ToString();
                _logger.LogInformation("Extracted text from PDF with {PageCount} pages, {WordCount} words",
                    pageCount, extractedText.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length);

                return extractedText;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PDF text extraction failed: {Message}", ex.Message);
                return "PDF text extraction failed";
            }
        }

    
        private async Task<string> ExtractDocxTextAsync(Stream stream, CancellationToken ct)
        {
            try
            {
                using var wordDoc = WordprocessingDocument.Open(stream, false);
                var body = wordDoc.MainDocumentPart?.Document?.Body;

                if (body == null) return "Empty DOCX document";

                var textBuilder = new StringBuilder();

                // Extract paragraphs
                var paragraphs = body.Elements<DocumentFormat.OpenXml.Wordprocessing.Paragraph>();
                foreach (var paragraph in paragraphs)
                {
                    ct.ThrowIfCancellationRequested();
                    var paragraphText = paragraph.InnerText?.Trim();
                    if (!string.IsNullOrEmpty(paragraphText))
                    {
                        textBuilder.AppendLine(paragraphText);
                    }
                }

                // Extract tables
                var tables = body.Elements<DocumentFormat.OpenXml.Wordprocessing.Table>();
                foreach (var table in tables)
                {
                    ct.ThrowIfCancellationRequested();
                    textBuilder.AppendLine("[Table]");
                    var rows = table.Elements<DocumentFormat.OpenXml.Wordprocessing.TableRow>();
                    foreach (var row in rows)
                    {
                        var cells = row.Elements<DocumentFormat.OpenXml.Wordprocessing.TableCell>()
                            .Select(cell => cell.InnerText?.Trim())
                            .Where(text => !string.IsNullOrEmpty(text));
                        if (cells.Any())
                        {
                            textBuilder.AppendLine(string.Join(" | ", cells));
                        }
                    }
                    textBuilder.AppendLine();
                }

                // Extract headers and footers
                if (wordDoc.MainDocumentPart.HeaderParts != null)
                {
                    foreach (var header in wordDoc.MainDocumentPart.HeaderParts)
                    {
                        var headerText = header.Header?.InnerText;
                        if (!string.IsNullOrEmpty(headerText))
                        {
                            textBuilder.AppendLine($"[Header: {headerText.Trim()}]");
                        }
                    }
                }

                if (wordDoc.MainDocumentPart.FooterParts != null)
                {
                    foreach (var footer in wordDoc.MainDocumentPart.FooterParts)
                    {
                        var footerText = footer.Footer?.InnerText;
                        if (!string.IsNullOrEmpty(footerText))
                        {
                            textBuilder.AppendLine($"[Footer: {footerText.Trim()}]");
                        }
                    }
                }

                return textBuilder.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DOCX text extraction failed");
                return "DOCX text extraction failed";
            }
        }

        private async Task<string> ExtractDocTextAsync(Stream stream, CancellationToken ct)
        {
            // For older .doc format, we'd need a library like NPOI or LibreOffice
            // For now, return placeholder
            return "[Legacy DOC file - text extraction requires additional libraries]";
        }

        private async Task<string> ExtractXlsxTextAsync(Stream stream, CancellationToken ct)
        {
            try
            {
                using var spreadsheet = DocumentFormat.OpenXml.Packaging.SpreadsheetDocument.Open(stream, false);
                var textBuilder = new StringBuilder();

                foreach (var worksheet in spreadsheet.WorkbookPart.WorksheetParts)
                {
                    ct.ThrowIfCancellationRequested();

                    var sheetData = worksheet.Worksheet.Elements<DocumentFormat.OpenXml.Spreadsheet.SheetData>().FirstOrDefault();
                    if (sheetData != null)
                    {
                        textBuilder.AppendLine($"[Worksheet]");
                        foreach (var row in sheetData.Elements<DocumentFormat.OpenXml.Spreadsheet.Row>())
                        {
                            var cells = row.Elements<DocumentFormat.OpenXml.Spreadsheet.Cell>()
                                .Select(cell => GetCellValue(cell, spreadsheet.WorkbookPart.SharedStringTablePart))
                                .Where(text => !string.IsNullOrEmpty(text));

                            if (cells.Any())
                            {
                                textBuilder.AppendLine(string.Join(" | ", cells));
                            }
                        }
                        textBuilder.AppendLine();
                    }
                }

                return textBuilder.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "XLSX text extraction failed");
                return "XLSX text extraction failed";
            }
        }

        private string GetCellValue(DocumentFormat.OpenXml.Spreadsheet.Cell cell, DocumentFormat.OpenXml.Packaging.SharedStringTablePart sharedStringTablePart)
        {
            if (cell.DataType != null && cell.DataType.Value == DocumentFormat.OpenXml.Spreadsheet.CellValues.SharedString)
            {
                return sharedStringTablePart.SharedStringTable.Elements<DocumentFormat.OpenXml.Spreadsheet.SharedStringItem>()
                    .ElementAt(int.Parse(cell.InnerText)).InnerText;
            }
            return cell.InnerText;
        }

        private async Task<string> ExtractPptxTextAsync(Stream stream, CancellationToken ct)
        {
            try
            {
                using var presentation = DocumentFormat.OpenXml.Packaging.PresentationDocument.Open(stream, false);
                var textBuilder = new StringBuilder();

                foreach (var slide in presentation.PresentationPart.SlideParts)
                {
                    ct.ThrowIfCancellationRequested();

                    textBuilder.AppendLine($"[Slide {slide.Slide.Count()}]");

                    // Extract text from slide
                    var slideText = slide.Slide.Descendants<DocumentFormat.OpenXml.Drawing.Text>()
                        .Select(t => t.Text)
                        .Where(text => !string.IsNullOrEmpty(text));

                    foreach (var text in slideText)
                    {
                        textBuilder.AppendLine(text);
                    }

                    // Extract notes
                    if (slide.NotesSlidePart != null)
                    {
                        var notesText = slide.NotesSlidePart.NotesSlide.Descendants<DocumentFormat.OpenXml.Drawing.Text>()
                            .Select(t => t.Text)
                            .Where(text => !string.IsNullOrEmpty(text));

                        if (notesText.Any())
                        {
                            textBuilder.AppendLine("[Notes:]");
                            foreach (var note in notesText)
                            {
                                textBuilder.AppendLine(note);
                            }
                        }
                    }

                    textBuilder.AppendLine();
                }

                return textBuilder.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PPTX text extraction failed");
                return "PPTX text extraction failed";
            }
        }

        private async Task<string> ExtractPlainTextAsync(Stream stream, CancellationToken ct)
        {
            try
            {
                using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
                return await reader.ReadToEndAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Plain text extraction failed");
                return "Plain text extraction failed";
            }
        }

        private async Task<string> ExtractRtfTextAsync(Stream stream, CancellationToken ct)
        {
            try
            {
                // Basic RTF stripping - for full support, consider using a library
                using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
                var rtf = await reader.ReadToEndAsync(ct);

                // Remove RTF control words and keep text
                var text = Regex.Replace(rtf, @"\\[a-zA-Z]+\d*", "");
                text = Regex.Replace(text, @"\\[^a-zA-Z]", "");
                text = Regex.Replace(text, @"[{}]", "");

                return text.Trim();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RTF text extraction failed");
                return "RTF text extraction failed";
            }
        }

        #endregion

        #region Enhanced Content Analysis Methods

        private bool AnalyzeEducationalContent(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;

            var educationalIndicators = new[]
            {
                // English indicators
                "learn", "study", "tutorial", "lesson", "course", "education",
                "example", "exercise", "practice", "theory", "concept",
                "introduction", "overview", "summary", "definition", "chapter",
                "objective", "outcome", "assessment", "quiz", "exam", "homework",
                "notebook", "workbook", "textbook", "manual", "guide", "handbook",
                "syllabus", "curriculum", "lecture", "seminar", "workshop",

                // Vietnamese indicators
                "chương", "bài", "bài học", "học tập", "giảng bài", "khái niệm",
                "định nghĩa", "tổng quan", "mục tiêu", "kết quả", "tập huấn",
                "bài tập", "luyện tập", "thực hành", "lý thuyết", "ví dụ",
                "giáo trình", "sách giáo khoa", "sách tham khảo", "tài liệu",

                // Academic terms
                "methodology", "method", "approach", "principle", "fundamental",
                "framework", "model", "algorithm", "procedure", "process",
                "analyze", "analysis", "evaluate", "evaluation", "research",
                "investigation", "experiment", "data", "results", "conclusion"
            };

            var lowerText = text.ToLowerInvariant();
            var indicatorCount = educationalIndicators.Count(indicator => lowerText.Contains(indicator));

            // Also check for structured content like numbered lists, sections, etc.
            var hasStructure = Regex.IsMatch(text, @"\d+\.\s|Chapter\s+\d+|Section\s+\d+");
            var hasQuestions = Regex.IsMatch(lowerText, @"\b(what|when|where|why|how|who|which|ai|bao giờ|ở đâu|tại sao|như thế nào|ai)\b");

            // Determine if educational based on indicators and structure
            return indicatorCount >= 3 || (indicatorCount >= 2 && (hasStructure || hasQuestions));
        }

        private double AnalyzeContentQuality(string text)
        {
            if (string.IsNullOrEmpty(text)) return 0.0;

            var qualityScore = 0.3; // Base score

            // Length factor
            var wordCount = text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
            if (wordCount > 1000) qualityScore += 0.2;
            else if (wordCount > 500) qualityScore += 0.15;
            else if (wordCount < 100) qualityScore -= 0.1;
            else if (wordCount < 50) qualityScore -= 0.2;

            // Structure and organization
            var sentences = text.Split(new[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);
            var avgSentenceLength = sentences.Any() ? sentences.Average(s => s.Split(' ').Length) : 0;

            // Good sentence length (10-25 words)
            if (avgSentenceLength >= 10 && avgSentenceLength <= 25) qualityScore += 0.1;
            else if (avgSentenceLength > 50 || avgSentenceLength < 5) qualityScore -= 0.1;

            // Paragraph structure
            var paragraphs = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            if (paragraphs.Length > 3) qualityScore += 0.05;

            // Vocabulary diversity
            var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(w => w.ToLowerInvariant().Trim('.').Trim(',').Trim('!').Trim('?'))
                .Where(w => w.Length > 2);

            var uniqueWords = words.Distinct().Count();
            var totalWords = words.Count();

            if (totalWords > 0)
            {
                var diversity = (double)uniqueWords / totalWords;
                if (diversity > 0.6) qualityScore += 0.1;
                else if (diversity < 0.3) qualityScore -= 0.1;
            }

            // Punctuation and grammar indicators
            var hasProperPunctuation = text.Contains('.') || text.Contains('!') || text.Contains('?');
            if (hasProperPunctuation) qualityScore += 0.05;

            // Check for common indicators of poor quality
            var poorIndicators = new[] { "click here", "buy now", "free money", "urgent", "act now" };
            var hasPoorIndicators = poorIndicators.Any(indicator => text.ToLowerInvariant().Contains(indicator));
            if (hasPoorIndicators) qualityScore -= 0.15;

            // Check for academic/educational language
            var academicWords = new[] { "therefore", "however", "consequently", "furthermore", "moreover",
                                       "accordingly", "subsequently", "nevertheless", "nonetheless", "do đó", "tuy nhiên" };
            var academicCount = academicWords.Count(word => text.ToLowerInvariant().Contains(word));
            if (academicCount >= 2) qualityScore += 0.1;

            return Math.Max(0, Math.Min(1, qualityScore));
        }

        private string DetermineAcademicLevel(string text)
        {
            if (string.IsNullOrEmpty(text)) return "Unknown";

            var lowerText = text.ToLowerInvariant();

            // Elementary/Beginner indicators
            var elementaryIndicators = new[]
            {
                "basic", "beginner", "introduction", "getting started", "for dummies",
                "elementary", "primary", "fundamental", "step by step", "easy",
                "cơ bản", "người mới bắt đầu", "dễ hiểu", "hướng dẫn cơ bản"
            };

            // Intermediate indicators
            var intermediateIndicators = new[]
            {
                "intermediate", "practical", "hands on", "real world", "application",
                "practice", "implementation", "use case", "example", "case study",
                "thực hành", "ví dụ thực tế", "trung cấp", "ứng dụng"
            };

            // Advanced indicators
            var advancedIndicators = new[]
            {
                "advanced", "expert", "master", "research", "thesis", "dissertation",
                "phd", "doctorate", "graduate", "postgraduate", "scholarly",
                "algorithm", "complexity", "optimization", "theorem", "proof",
                "nâng cao", "chuyên gia", " nghiên cứu", "tiến sĩ", "đại học"
            };

            // Professional indicators
            var professionalIndicators = new[]
            {
                "professional", "certification", "industry", "enterprise", "production",
                "best practices", "standards", "compliance", "expert", "master",
                "chuyên nghiệp", "chứng chỉ", "ngành", "doanh nghiệp", "tiêu chuẩn"
            };

            // Count matches for each level
            var elementaryCount = elementaryIndicators.Count(i => lowerText.Contains(i));
            var intermediateCount = intermediateIndicators.Count(i => lowerText.Contains(i));
            var advancedCount = advancedIndicators.Count(i => lowerText.Contains(i));
            var professionalCount = professionalIndicators.Count(i => lowerText.Contains(i));

            // Also consider complexity indicators
            var hasComplexTerms = lowerText.Contains("algorithm") || lowerText.Contains("complexity") ||
                                 lowerText.Contains("optimization") || lowerText.Contains("cryptography") ||
                                 lowerText.Contains("quantum") || lowerText.Contains("machine learning");

            // Determine level based on counts
            if (advancedCount >= 2 || (advancedCount >= 1 && hasComplexTerms))
                return "Advanced";
            else if (professionalCount >= 2)
                return "Professional";
            else if (intermediateCount >= 2)
                return "Intermediate";
            else if (elementaryCount >= 2)
                return "Beginner";
            else if (hasComplexTerms)
                return "Advanced";
            else
                return "Intermediate";
        }

        private List<string> ExtractKeyTopics(string text)
        {
            if (string.IsNullOrEmpty(text)) return new List<string>();

            // Expanded topic categories
            var topicCategories = new Dictionary<string, string[]>
            {
                ["Computer Science"] = new[] { "programming", "algorithm", "data structure", "software", "coding", "developer", "database", "api", "frontend", "backend" },
                ["Data Science"] = new[] { "data science", "machine learning", "artificial intelligence", "deep learning", "neural network", "analytics", "big data", "statistics", "visualization" },
                ["Web Development"] = new[] { "html", "css", "javascript", "react", "angular", "vue", "nodejs", "php", "asp.net", "web development" },
                ["Mobile Development"] = new[] { "android", "ios", "swift", "kotlin", "react native", "flutter", "mobile app", "smartphone", "tablet" },
                ["Business"] = new[] { "business", "marketing", "finance", "accounting", "management", "entrepreneurship", "startup", "strategy", "leadership" },
                ["Mathematics"] = new[] { "mathematics", "algebra", "calculus", "geometry", "statistics", "probability", "linear algebra", "discrete math" },
                ["Science"] = new[] { "physics", "chemistry", "biology", "science", "scientific", "research", "experiment", "laboratory", "theory" },
                ["Engineering"] = new[] { "engineering", "mechanical", "electrical", "civil", "chemical", "computer engineering", "design", "manufacturing" },
                ["Design"] = new[] { "design", "ui", "ux", "graphic design", "web design", "user interface", "user experience", "photoshop", "illustrator" },
                ["Education"] = new[] { "education", "learning", "teaching", "pedagogy", "curriculum", "assessment", "educational", "academic" },
                ["Language"] = new[] { "language", "english", "vocabulary", "grammar", "writing", "reading", "speaking", "listening", "communication" },
                ["Health"] = new[] { "health", "medicine", "healthcare", "fitness", "nutrition", "wellness", "medical", "clinical", "therapy" },
                ["Social Sciences"] = new[] { "psychology", "sociology", "anthropology", "economics", "political science", "history", "philosophy" },
                ["Technology"] = new[] { "technology", "tech", "innovation", "digital", "automation", "robotics", "iot", "cloud", "cybersecurity" }
            };

            var lowerText = text.ToLowerInvariant();
            var detectedTopics = new List<string>();
            var topicCounts = new Dictionary<string, int>();

            // Count occurrences for each topic
            foreach (var category in topicCategories)
            {
                var count = category.Value.Sum(topic =>
                    Regex.Matches(lowerText, $@"\b{Regex.Escape(topic)}\b").Count);

                if (count > 0)
                {
                    topicCounts[category.Key] = count;
                }
            }

            // Sort by frequency and take top 10
            var sortedTopics = topicCounts
                .OrderByDescending(kvp => kvp.Value)
                .Select(kvp => kvp.Key)
                .Take(10)
                .ToList();

            return sortedTopics;
        }

        private bool ContainsCode(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;

            var codeIndicators = new[]
            {
                // Programming keywords
                "function", "def ", "class ", "var ", "let ", "const ", "if ", "else", "for ", "while ",
                "foreach", "switch", "case", "break", "continue", "return", "import ", "export ",
                "public", "private", "protected", "static", "void", "int", "string", "bool",

                // Programming symbols
                "{", "}", "()", "[]", "=>", "==", "!=", "<=", ">=", "++", "--", "&&", "||",

                // Data structures
                "array", "list", "dictionary", "map", "set", "queue", "stack", "tree", "graph",

                // Common patterns
                "console.log", "System.out.println", "printf", "print", "alert", "document.getElementById"
            };

            // Check for code blocks
            var hasCodeBlocks = Regex.IsMatch(text, @"```[\s\S]*?```") ||
                               Regex.IsMatch(text, @"`\s*[^`]+\s*`") ||
                               Regex.IsMatch(text, @"^\s*[\w\s]+\s*\([^)]*\)\s*\{[\s\S]*?\}\s*$", RegexOptions.Multiline);

            return codeIndicators.Any(indicator => text.Contains(indicator)) || hasCodeBlocks;
        }

        private bool ContainsFormulas(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;

            // Mathematical operators and symbols
            var formulaIndicators = new[]
            {
                "=", "+", "-", "*", "/", "^", "√", "π", "∑", "∏", "∫", "∂", "∇",
                "≈", "≠", "≤", "≥", "∞", "α", "β", "γ", "δ", "θ", "λ", "μ", "σ", "φ", "ω"
            };

            // Mathematical terms
            var mathTerms = new[]
            {
                "equation", "formula", "calculate", "compute", "solve", "derivative", "integral",
                "function", "variable", "constant", "coefficient", "exponent", "logarithm",
                "sin", "cos", "tan", "matrix", "vector", "probability", "statistics"
            };

            // Check for mathematical expressions
            var hasMathExpression = Regex.IsMatch(text, @"[a-zA-Z]\s*=\s*[a-zA-Z0-9+\-*/^()]+") ||
                                   Regex.IsMatch(text, @"\b\d+\s*[+\-*/]\s*\d+\b") ||
                                   Regex.IsMatch(text, @"\b[a-zA-Z]\^\d+\b");

            return formulaIndicators.Any(symbol => text.Contains(symbol)) ||
                   mathTerms.Any(term => text.ToLowerInvariant().Contains(term)) ||
                   hasMathExpression;
        }

        private bool HasDiagramReferences(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;

            var diagramReferences = new[]
            {
                "diagram", "chart", "figure", "image", "graph", "illustration", "picture",
                "see below", "refer to", "as shown", "above", "below", "figure", "fig",
                "schematic", "flowchart", "blueprint", "sketch", "drawing", "plot",
                "biểu đồ", "sơ đồ", "hình ảnh", "minh họa", "xem dưới", "như hình"
            };

            var lowerText = text.ToLowerInvariant();

            // Also check for figure references like "Figure 1", "Fig. 2", etc.
            var hasFigureRefs = Regex.IsMatch(lowerText, @"\b(fig(ure)?\s*\.?\s*\d+)\b") ||
                               Regex.IsMatch(lowerText, @"\b(hình\s*\d+)\b");

            return diagramReferences.Any(refWord => lowerText.Contains(refWord)) || hasFigureRefs;
        }

        #endregion

        private string StripMarkdown(string markdown)
        {
            // Simple markdown stripping
            var lines = markdown.Split('\n');
            var plainLines = lines.Where(line => !line.StartsWith('#') &&
                                                !line.StartsWith('*') &&
                                                !line.StartsWith('-') &&
                                                !line.StartsWith('>') &&
                                                !string.IsNullOrWhiteSpace(line))
                                     .Select(line => Regex.Replace(line, @"\*\*(.*?)\*\*", "$1"))
                                     .Select(line => Regex.Replace(line, @"\*(.*?)\*", "$1"))
                                     .ToList();

            return string.Join("\n", plainLines);
        }

        private string HtmlToPlainText(string html)
        {
            // Simple HTML stripping
            return Regex.Replace(html, "<[^>]+>", " ")
                        .Replace("&nbsp;", " ")
                        .Replace("&lt;", "<")
                        .Replace("&gt;", ">")
                        .Replace("&amp;", "&");
        }

        private string DetectLanguage(string text)
        {
            if (Regex.IsMatch(text, @"[\u00c0-\u017f]+") ||
                text.Contains("bài") || text.Contains("học") || text.Contains("giảng"))
                return "Vietnamese";

            return "English";
        }

        private int CalculateReadingTime(int wordCount)
        {
            return Math.Max(1, (int)Math.Ceiling(wordCount / 200.0));
        }

        private int EstimatePageCount(string contentType, string fileName, int wordCount)
        {
            if (contentType.Contains("pdf"))
                return Math.Max(1, wordCount / 500);
            else if (fileName.EndsWith(".docx") || fileName.EndsWith(".doc"))
                return Math.Max(1, wordCount / 300);
            else if (fileName.EndsWith(".pptx") || fileName.EndsWith(".ppt"))
                return Math.Max(1, wordCount / 100);
            else
                return 1;
        }

        #endregion
    }
}