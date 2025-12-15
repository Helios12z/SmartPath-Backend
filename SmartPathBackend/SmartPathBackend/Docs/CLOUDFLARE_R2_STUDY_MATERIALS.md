# Cloudflare R2 Integration for Study Materials

## Overview
Implemented Cloudflare R2 integration for study materials, ensuring that accepted materials are uploaded to Cloudflare R2 storage, similar to how materials in posts/comments are handled.

## Implementation Details

### 1. Temporary File Storage
When a study material is uploaded:
- The file is saved to a temporary location: `{TempPath}/StudyMaterials/{MaterialId}/{FileName}`
- The temporary path is stored in the `FileUrl` field initially
- This allows the file to be available during the review process

### 2. Cloudflare R2 Upload Process
When an admin accepts a study material:
1. The temporary file is read from disk
2. Uploaded to Cloudflare R2 with the key: `study-materials/{MaterialId}/{FileName}`
3. The public URL is stored in the database: `{R2PublicBaseUrl}/study-materials/{MaterialId}/{FileName}`
4. Temporary file is deleted after successful upload

When a material is rejected:
- The temporary file is immediately deleted
- `FileUrl` is set to "File deleted due to rejection"

### 3. R2 Configuration
The service uses the same R2 configuration as MaterialService:
- **Bucket Name**: Configured in R2Options
- **Public Base URL**: Configured in R2Options
- **Access Control**: Files are uploaded with `PublicRead` ACL
- **Content Type**: Preserved from original upload

### 4. Error Handling
- If R2 upload fails during acceptance, the material is still accepted
- Error message is stored in FileUrl for tracking
- Automatic cleanup of temporary files
- Comprehensive logging for debugging

## API Flow

### Upload Flow:
1. **User Upload** → File saved to temporary location
2. **Database Entry** → Material with `Status.Pending`
3. **AI Analysis** → Content analysis on temporary file
4. **Admin Review** → Decision made by admin

### Accept Flow:
1. **Admin Accepts** → Material status changed to `Accepted`
2. **R2 Upload** → File uploaded to Cloudflare R2
3. **URL Update** → FileUrl updated with R2 public URL
4. **Cleanup** → Temporary file deleted

### Reject Flow:
1. **Admin Rejects** → Material status changed to `Rejected`
2. **Cleanup** → Temporary file deleted
3. **URL Update** → FileUrl set to deletion notice

## Benefits

1. **Storage Efficiency**: Only accepted materials are stored in R2
2. **Cost Optimization**: No storage costs for rejected materials
3. **Performance**: Public CDN access through Cloudflare
4. **Scalability**: Cloudflare R2 scales automatically
5. **Reliability**: Files persisted after acceptance

## File Structure in R2

```
Bucket/
└── study-materials/
    ├── {materialId}/
    │   └── {originalFileName}
    ├── {materialId}/
    │   └── {originalFileName}
    └── ...
```

## Security Considerations

- Files are uploaded with public read access
- Content type is preserved for proper browser handling
- Temporary files are securely deleted from server disk
- No sensitive information in file keys

## Monitoring and Logging

- All R2 upload attempts are logged
- Success/failure of uploads tracked
- Temporary file cleanup logged
- Error details captured for debugging

## Future Enhancements

1. **Private Files**: Option for private access control
2. **Versioning**: Support for file updates
3. **CDN Caching**: Configure cache headers
4. **File Compression**: Automatic compression for supported types
5. **Thumbnail Generation**: Create thumbnails for image files