using AutoMapper;
using Microsoft.Extensions.Options;
using SmartPathBackend.Interfaces;
using SmartPathBackend.Interfaces.Services;
using SmartPathBackend.Models.DTOs;
using SmartPathBackend.Models.Entities;
using SmartPathBackend.Utils;
using Amazon.S3;
using Amazon.S3.Model;

namespace SmartPathBackend.Services
{
    public class MaterialService : IMaterialService
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;
        private readonly HttpClient _http;
        private readonly ImgBbOptions _imgbb;
        private readonly R2Options _r2;
        private readonly UploadPolicyOptions _policy;
        private readonly IAmazonS3 _s3;

        public MaterialService(
            IUnitOfWork uow,
            IMapper mapper,
            IHttpClientFactory httpFactory,
            IOptions<ImgBbOptions> imgbb,
            IOptions<R2Options> r2,
            IOptions<UploadPolicyOptions> policy,
            IAmazonS3 s3 
        )
        {
            _uow = uow;
            _mapper = mapper;
            _http = httpFactory.CreateClient();
            _imgbb = imgbb.Value;
            _r2 = r2.Value;
            _policy = policy.Value;
            _s3 = s3;
        }

        public async Task<MaterialResponse> UploadImageAsync(Guid uploaderId, MaterialCreateRequest meta, IFormFile file)
        {
            ValidateImage(file);

            // Chuẩn bị form-data cho ImgBB
            using var content = new MultipartFormDataContent();
            using var stream = file.OpenReadStream();
            var streamContent = new StreamContent(stream);
            streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);
            content.Add(streamContent, "image", file.FileName);
            content.Add(new StringContent(_imgbb.ApiKey), "key");
            if (_imgbb.ExpirationSeconds > 0)
                content.Add(new StringContent(_imgbb.ExpirationSeconds.ToString()), "expiration");

            var resp = await _http.PostAsync(_imgbb.UploadEndpoint, content);
            resp.EnsureSuccessStatusCode();
            var json = await resp.Content.ReadFromJsonAsync<ImgBbUploadResult>();
            var url = json?.data?.url ?? throw new Exception("ImgBB upload failed");

            var entity = new Material
            {
                UploaderId = uploaderId,
                PostId = meta.PostId,
                CommentId = meta.CommentId,
                MessageId = meta.MessageId,
                Title = meta.Title,
                Description = meta.Description,
                FileUrl = url,
                UploadedAt = DateTime.UtcNow
            };

            await _uow.Materials.AddAsync(entity);
            await _uow.SaveChangesAsync();

            return _mapper.Map<MaterialResponse>(entity);
        }

        public async Task<IReadOnlyList<MaterialResponse>> UploadDocumentsAsync(Guid uploaderId, MaterialCreateRequest meta, IFormFile[] files)
        {
            var results = new List<MaterialResponse>();

            foreach (var f in files)
            {
                ValidateDoc(f);

                var key = $"materials/{DateTime.UtcNow:yyyy/MM/dd}/{Guid.NewGuid()}-{Sanitize(f.FileName)}";
                var publicUrl = $"{_r2.PublicBaseUrl.TrimEnd('/')}/{key}";

                var presign = _s3.GetPreSignedURL(new GetPreSignedUrlRequest
                {
                    BucketName = _r2.Bucket,
                    Key = key,
                    Verb = HttpVerb.PUT,
                    Expires = DateTime.UtcNow.AddMinutes(_r2.PresignedUploadMinutes),
                    ContentType = f.ContentType
                });

                using var stream = f.OpenReadStream();
                using var content = new StreamContent(stream);
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(f.ContentType);
                content.Headers.ContentLength = f.Length; 

                try
                {
                    var http = new HttpClient();
                    var put = await http.PutAsync(presign, content);
                    put.EnsureSuccessStatusCode(); 

                    var entity = new Material
                    {
                        UploaderId = uploaderId,
                        PostId = meta.PostId,
                        CommentId = meta.CommentId,
                        MessageId = meta.MessageId,
                        Title = meta.Title,
                        Description = meta.Description,
                        FileUrl = publicUrl,
                        UploadedAt = DateTime.UtcNow
                    };

                    await _uow.Materials.AddAsync(entity);
                    await _uow.SaveChangesAsync();

                    results.Add(_mapper.Map<MaterialResponse>(entity));
                }
                catch
                {
                    try { await _s3.DeleteObjectAsync(_r2.Bucket, key); } catch { /* ignore */ }
                    throw;
                }
            }

            await _uow.SaveChangesAsync();
            return results;
        }

        public Task<IReadOnlyList<MaterialResponse>> GetByPostAsync(Guid postId) =>
            MapList(_uow.Materials.GetByPostAsync(postId));

        public Task<IReadOnlyList<MaterialResponse>> GetByCommentAsync(Guid commentId) =>
            MapList(_uow.Materials.GetByCommentAsync(commentId));

        public Task<IReadOnlyList<MaterialResponse>> GetByMessageAsync(Guid messageId) =>
            MapList(_uow.Materials.GetByMessageAsync(messageId));

        public async Task<bool> DeleteAsync(Guid id, Guid requesterId)
        {
            var entity = await _uow.Materials.GetByIdAsync(id);
            if (entity is null) return false;
            if (entity.UploaderId != requesterId) return false;

            _uow.Materials.Remove(entity);
            await _uow.SaveChangesAsync();
            return true;
        }

        private async Task<IReadOnlyList<MaterialResponse>> MapList(Task<IReadOnlyList<Material>> t)
        {
            var list = await t;
            return list.Select(_mapper.Map<MaterialResponse>).ToList();
        }

        private void ValidateImage(IFormFile file)
        {
            if (file.Length <= 0) throw new ArgumentException("Empty file.");
            if (file.Length > _policy.Images.MaxBytes) throw new ArgumentException("Image too large.");
            if (!_policy.Images.AllowedContentTypes.Contains(file.ContentType))
                throw new ArgumentException("Image content-type not allowed.");
        }

        private void ValidateDoc(IFormFile file)
        {
            if (file.Length <= 0) throw new ArgumentException("Empty file.");
            if (file.Length > _policy.Documents.MaxBytes) throw new ArgumentException("Document too large.");
            if (!_policy.Documents.AllowedContentTypes.Contains(file.ContentType))
                throw new ArgumentException("Document content-type not allowed.");
            if (_policy.Documents.AllowedExtensions is { Length: > 0 })
            {
                var ext = Path.GetExtension(file.FileName)?.ToLowerInvariant();
                if (!_policy.Documents.AllowedExtensions.Contains(ext))
                    throw new ArgumentException("Document extension not allowed.");
            }
        }

        private static string Sanitize(string name)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return name;
        }

        private class ImgBbUploadResult
        {
            public ImgBbData? data { get; set; }
            public class ImgBbData { public string url { get; set; } = default!; }
        }
    }
}
