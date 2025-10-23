namespace SmartPathBackend.Utils
{
    public class ImgBbOptions
    {
        public string ApiKey { get; set; } = default!;
        public string UploadEndpoint { get; set; } = "https://api.imgbb.com/1/upload";
        public int ExpirationSeconds { get; set; } = 0;
    }

    public class R2Options
    {
        public string AccountId { get; set; } = default!;
        public string AccessKeyId { get; set; } = default!;
        public string SecretAccessKey { get; set; } = default!;
        public string Bucket { get; set; } = default!;
        public string ServiceUrl { get; set; } = default!;     
        public string PublicBaseUrl { get; set; } = default!;  
        public int PresignedUploadMinutes { get; set; } = 10;
        public int PresignedDownloadMinutes { get; set; } = 5;
    }

    public class UploadPolicyOptions
    {
        public UploadGroup Images { get; set; } = new();
        public UploadGroup Documents { get; set; } = new();
        public class UploadGroup
        {
            public long MaxBytes { get; set; }
            public string[] AllowedContentTypes { get; set; } = Array.Empty<string>();
            public string[]? AllowedExtensions { get; set; }
        }
    }
}
