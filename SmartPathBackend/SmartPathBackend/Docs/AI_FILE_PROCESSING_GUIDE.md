# AI File Processing Guide for Study Materials

## Overview

This guide explains how the AI system processes uploaded study materials, including text extraction, accuracy checking, and handling different file types including images.

## Current Implementation Status

### ❌ **Important Limitation**
Currently, the AI **does NOT read the actual file content** during the review process. The `extractedText` parameter is passed as `null` to the AI reviewer.

```csharp
// Current implementation in StudyMaterialLibraryService.cs
var aiReview = await _aiReviewer.ReviewAsync(
    category?.Path ?? "",
    material.Title,
    material.Description,
    null, // ❌ No file content extracted yet!
    new List<string>(),
    ct);
```

## What AI Currently Analyzes

### 1. **Title Analysis**
- AI reads the material title
- Checks if title matches selected category
- Identifies keywords and subject matter

### 2. **Description Analysis**
- AI analyzes the description text
- Evaluates content relevance to category
- Assesses educational value indicators

### 3. **Category Matching**
- Compares title + description against category path
- Checks for consistency in topic categorization
- Suggests better category alternatives

## Available Text Extraction Capabilities

The system **has** text extraction capabilities (used in KnowledgeService) but they're **not yet integrated** into the material review process:

### ✅ **Supported File Types for Text Extraction**

#### 1. **PDF Documents**
```csharp
// PDF text extraction using PdfText library
return PdfText.ExtractText(bytes) ?? string.Empty;
```
- Extracts text from PDF content
- Handles both text-based and scanned PDFs
- Preserves formatting and structure

#### 2. **Microsoft Word Documents**
- **DOCX**: `application/vnd.openxmlformats-officedocument.wordprocessingml.document`
- **DOC**: `application/msword`
```csharp
// DOCX extraction
using var wordDoc = WordprocessingDocument.Open(ms, false);
return wordDoc.MainDocumentPart?.Document?.Body?.InnerText ?? string.Empty;

// DOC extraction (uses Tika)
var extractor = new TextExtractor();
return extractor.Extract(bytes)?.Text ?? string.Empty;
```

#### 3. **PowerPoint Presentations**
- **PPTX**: `application/vnd.openxmlformats-officedocument.presentationml.presentation`
- **PPT**: `application/vnd.ms-powerpoint`

#### 4. **Excel Spreadsheets**
- **XLSX**: `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`
- **XLS**: `application/vnd.ms-excel`

#### 5. **Plain Text Files**
```csharp
// Plain text files (.txt, .csv, etc.)
using var r = new StreamReader(stream, Encoding.UTF8);
return await r.ReadToEndAsync();
```

#### 6. **HTML Files**
```csharp
// HTML content extraction
var html = await r.ReadToEndAsync();
return HtmlToPlainText(html);
```

#### 7. **Markdown Files**
```csharp
// Markdown files (.md)
var md = await r.ReadToEndAsync();
return StripMarkdown(md);
```

### ⚠️ **Not Yet Implemented**
- Full file content reading in material review
- Image OCR (Optical Character Recognition)
- Audio/video transcription
- Scanned image text extraction

## Image Handling

### Current Behavior
When users upload images:

1. **File is stored** with the material
2. **Basic metadata** is saved (size, MIME type)
3. **AI Review** only sees title and description
4. **No image analysis** is performed

### Supported Image Formats
Based on the file handling, these formats are likely supported:
- **JPEG/JPG**: `image/jpeg`
- **PNG**: `image/png`
- **GIF**: `image/gif`
- **BMP**: `image/bmp`
- **WebP**: `image/webp`
- **SVG**: `image/svg+xml`

### What Happens with Images

```csharp
// In StudyMaterialLibraryService.cs - CreateAsync
if (meta.SourceType == StudyMaterialSourceType.File && file != null)
{
    material.FileUrl = $"/uploads/materials/{material.Id}/{file.FileName}";
    material.MimeType = file.ContentType;
    material.FileSize = file.Length;
}
```

**Images are:**
- ✅ Stored in the file system/cloud storage
- ✅ Accessible via download URLs
- ✅ File size and type recorded
- ❌ **NOT analyzed by AI** (no OCR implemented)
- ❌ **No content extraction** for categorization

## Proposed Enhancement: Full AI File Processing

### 1. **Integrate Text Extraction**

```csharp
// Enhanced implementation proposal
private async Task<string?> ExtractFileTextAsync(IFormFile file, Guid materialId, CancellationToken ct)
{
    try
    {
        using var stream = file.OpenReadStream();
        return await ExtractTextAsync(stream, file.ContentType, file.FileName, ct);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to extract text from file {FileName}", file.FileName);
        return null;
    }
}

// Then in CreateAsync:
var extractedText = await ExtractFileTextAsync(file, material.Id, ct);
var aiReview = await _aiReviewer.ReviewAsync(
    category?.Path ?? "",
    material.Title,
    material.Description,
    extractedText, // ✅ Now with actual file content!
    await GetCandidateCategoryPaths(),
    ct
);
```

### 2. **Image OCR Integration**

```csharp
// Using Tesseract or Azure Cognitive Services
private async Task<string?> ExtractTextFromImageAsync(IFormFile imageFile, CancellationToken ct)
{
    // Option 1: Tesseract OCR
    using var engine = new TesseractEngine("eng", EngineMode.Default);
    using var img = Pix.LoadFromMemory(await GetBytesAsync(imageFile));
    using var page = engine.Process(img);
    return page.GetText();

    // Option 2: Azure Cognitive Services
    var computerVision = new ComputerVisionClient(apiKey, endpoint);
    var ocrResult = await computerVision.RecognizePrintedTextInStreamAsync(imageFile.OpenReadStream());
    return string.Join(" ", ocrResult.Regions.SelectMany(r => r.Lines.Select(l => l.Text)));
}
```

### 3. **Enhanced AI Prompt with File Content**

```csharp
// Enhanced AI review prompt
var enhancedPrompt = $"""
CategoryPath: {categoryPath}
Title: {title}
Description: {description ?? ""}

File Content (extracted text):
{extractedText?.Substring(0, Math.Min(4000, extractedText.Length)) ?? ""}

Candidate Categories: {string.Join(" | ", candidateCategoryPaths.Take(20))}

Tasks:
1. Analyze if the content matches the selected category
2. Verify the material is educational and appropriate
3. Check for quality and completeness
4. Suggest better category if mismatched

Return JSON only:
{{
  "categoryMatch": true|false,
  "confidence": 0.0-1.0,
  "reason": "Detailed analysis...",
  "suggestedCategoryPath": null|"...",
  "qualityScore": 0.0-1.0,
  "isEducational": true|false,
  "appropriateness": "Appropriate|Questionable|Inappropriate"
}}
""";
```

## File Size and Processing Limitations

### Current Limits
- **Text Extraction**: Processes up to 2000 characters for AI analysis
- **File Storage**: No explicit size limits mentioned
- **Memory Usage**: Files loaded into memory for extraction

### Recommended Best Practices

#### 1. **File Size Limits**
```csharp
// Recommended file size limits
private const long MaxFileSize = 50 * 1024 * 1024; // 50MB
private const long MaxImageSize = 10 * 1024 * 1024; // 10MB

// Validation in upload method
if (file.Length > MaxFileSize)
{
    throw new ArgumentException("File size exceeds maximum limit of 50MB");
}
```

#### 2. **Supported File Types**
```csharp
private readonly HashSet<string> SupportedExtensions = new()
{
    // Documents
    ".pdf", ".doc", ".docx", ".txt", ".rtf", ".odt",
    ".ppt", ".pptx", ".xls", ".xlsx", ".csv",
    // Images
    ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".svg",
    // Other
    ".md", ".html", ".htm"
};
```

#### 3. **Processing Strategy**
```csharp
// Async processing with progress tracking
public class FileProcessingJob
{
    public Guid MaterialId { get; set; }
    public string FileName { get; set; }
    public long FileSize { get; set; }
    public DateTime StartedAt { get; set; }
    public ProcessingStatus Status { get; set; }
    public string? ErrorMessage { get; set; }
    public float Progress { get; set; }
}
```

## Security and Privacy Considerations

### 1. **File Scanning**
```csharp
// Anti-virus scanning integration
private async Task<bool> ScanFileAsync(Stream fileStream)
{
    using var scanner = new AntiVirusScanner();
    return await scanner.ScanAsync(fileStream);
}
```

### 2. **Content Validation**
```csharp
// Content-based validation
private bool ValidateContent(string extractedText)
{
    // Check for malicious content patterns
    var suspiciousPatterns = new[]
    {
        "eval\\s*\\(",
        "javascript:",
        "<script",
        "exec\\s*\\("
    };

    return !suspiciousPatterns.Any(pattern =>
        Regex.IsMatch(extractedText, pattern, RegexOptions.IgnoreCase));
}
```

### 3. **Privacy Protection**
```csharp
// Anonymize sensitive information in extracted text
private string SanitizeExtractedText(string text)
{
    // Remove emails, phone numbers, etc.
    var sanitized = Regex.Replace(text, @"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b", "[EMAIL]");
    sanitized = Regex.Replace(sanitized, @"\b\d{3}[-.]?\d{3}[-.]?\d{4}\b", "[PHONE]");
    return sanitized;
}
```

## Performance Optimization

### 1. **Asynchronous Processing**
```csharp
// Background job processing
_ = Task.Run(async () =>
{
    try
    {
        var extractedText = await ExtractFileTextAsync(file, materialId, ct);
        var aiReview = await _aiReviewer.ReviewAsync(...);
        // Save results
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "File processing failed");
    }
});
```

### 2. **Chunked Processing**
```csharp
// Process large files in chunks
private async Task ProcessLargeFileAsync(Stream stream, Func<string, Task> processor)
{
    const int chunkSize = 1024 * 1024; // 1MB chunks
    using var reader = new StreamReader(stream);
    var buffer = new char[chunkSize];

    while (await reader.ReadAsync(buffer, 0, buffer.Length) > 0)
    {
        var chunk = new string(buffer);
        await processor(chunk);
    }
}
```

### 3. **Caching**
```csharp
// Cache extraction results
private readonly IMemoryCache _cache;

public async Task<string> GetExtractedTextAsync(Guid materialId)
{
    return await _cache.GetOrCreateAsync($"extracted_text_{materialId}",
        async () => await ExtractFromDatabaseAsync(materialId));
}
```

## Monitoring and Analytics

### 1. **Processing Metrics**
```csharp
// Track file processing statistics
public class FileProcessingMetrics
{
    public int TotalFilesProcessed { get; set; }
    public int SuccessfulExtractions { get; set; }
    public int FailedExtractions { get; set; }
    public Dictionary<string, int> FileTypeCounts { get; set; }
    public double AverageProcessingTime { get; set; }
}
```

### 2. **Error Tracking**
```csharp
// Log processing failures
private void LogProcessingError(Guid materialId, string fileName, Exception ex)
{
    _logger.LogError(ex, "File processing failed for material {MaterialId}, file {FileName}",
        materialId, fileName);

    // Update error metrics
    _metricsService.IncrementFailedExtractions();
}
```

## Implementation Roadmap

### Phase 1: Basic Text Extraction (Immediate)
1. Integrate existing text extraction into material review
2. Process common document formats (PDF, DOCX, TXT)
3. Enhance AI prompt with extracted content

### Phase 2: Image OCR (Short-term)
1. Implement OCR for image files
2. Extract text from screenshots and diagrams
3. Handle scanned document images

### Phase 3: Advanced Features (Long-term)
1. Video/audio transcription
2. Multi-language support
3. Advanced content quality assessment
4. Automatic keyword extraction and tagging

## Conclusion

While the current AI system provides valuable category matching and quality assessment based on titles and descriptions, **full file content analysis is not yet implemented**. The system has the technical capability to extract text from various file formats, but it needs integration into the material review workflow.

For images, the system can store and serve them but **does not perform OCR** or content analysis. This would require additional services or libraries to implement text extraction from visual content.

Implementing full file processing would significantly improve the accuracy and reliability of the AI review system by analyzing actual content rather than just metadata.