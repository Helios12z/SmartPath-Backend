# IntelligentFileSummarizer Implementation

## Overview
The `IntelligentFileSummarizer` is a comprehensive file analysis and text extraction service that processes various document formats to provide intelligent summaries and metadata extraction for study materials.

## Supported File Formats

### 1. PDF Documents
- **Library**: PdfPig (v0.1.12-alpha)
- **Features**:
  - Full text extraction from all pages
  - Table extraction and formatting
  - Structured content recognition
  - Page limit (50 pages) for performance
  - Word count and page tracking

### 2. Microsoft Word Documents
- **DOCX**: Full support using OpenXML SDK
  - Paragraph extraction
  - Table content extraction
  - Header and footer text
- **DOC**: Legacy format (placeholder for additional library)

### 3. Microsoft Excel Spreadsheets (XLSX)
- Cell value extraction
- Shared string table support
- Worksheet identification
- Table formatting in output

### 4. Microsoft PowerPoint Presentations (PPTX)
- Slide text extraction
- Speaker notes extraction
- Slide numbering

### 5. Text-Based Formats
- Plain text (.txt)
- Markdown (.md) - with markdown stripping
- HTML (.htm, .html) - with tag removal
- Rich Text Format (.rtf) - basic control word removal

## Content Analysis Features

### 1. Educational Content Detection
- **Indicators**:
  - English: "learn", "study", "tutorial", "lesson", "course", etc.
  - Vietnamese: "chương", "bài học", "học tập", "giảng bài", etc.
  - Academic: "research", "methodology", "analysis", "experiment"
- **Structure Analysis**: Numbered lists, sections, questions
- **Threshold**: Minimum 3 indicators OR 2 + structure

### 2. Quality Scoring (0.0 - 1.0)
Base score: 0.3
- Length factor (+/- 0.2)
- Sentence structure (+/- 0.1)
- Vocabulary diversity (+/- 0.1)
- Academic language (+0.1)
- Spam indicators (-0.15)

### 3. Academic Level Determination
- **Beginner**: Basic, elementary, introduction terms
- **Intermediate**: Default level
- **Advanced**: Research, algorithm, complexity terms
- **Professional**: Industry, enterprise, certification terms
- Supports both English and Vietnamese

### 4. Topic Extraction
14 categories with keyword matching:
- Computer Science
- Data Science
- Web Development
- Mobile Development
- Business
- Mathematics
- Science
- Engineering
- Design
- Education
- Language
- Health
- Social Sciences
- Technology

### 5. Special Content Detection
- **Code**: Programming keywords, symbols, code blocks
- **Formulas**: Mathematical expressions, symbols, equations
- **Diagrams**: Figure references, chart mentions, visual content indicators

## Text Extraction Strategy

### Smart Sampling
- **< 1000 words**: Full text
- **> 1000 words**: First 300 + middle 300 + last 300 words
- Maintains context while managing token limits

### Performance Considerations
- PDF page limit (50 pages)
- Cancellation token support throughout
- Memory-efficient stream processing
- Async/await pattern for all I/O operations

## AI Integration
The generated summary provides:
- Content type and sample text
- Educational assessment
- Quality metrics
- Academic level
- Key topics
- Special content flags
- Reading time estimation
- Language detection (English/Vietnamese)

## Error Handling
- Graceful fallbacks for unsupported formats
- Detailed error logging
- User-friendly error messages
- Continues processing even with partial failures

## Dependencies
- `PdfPig` for PDF processing
- `DocumentFormat.OpenXml` for Office documents
- Built-in .NET libraries for text processing
- Regular expressions for pattern matching

## Future Enhancements
- OCR for scanned PDFs/images
- Support for legacy DOC format
- Advanced table structure preservation
- Image content analysis
- Audio/video transcription
- Multi-language expansion

## Usage Example
```csharp
var result = await summarizer.SummarizeFileAsync(
    title: "Introduction to Algorithms",
    description: "Computer science algorithms textbook",
    fileName: "algorithms.pdf",
    fileSize: 5242880,
    contentType: "application/pdf",
    fileStream: fileStream
);
```

The result provides comprehensive metadata for AI review and categorization of study materials.