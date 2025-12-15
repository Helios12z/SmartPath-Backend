using Amazon.Runtime;
using Amazon.S3;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SmartPathBackend.Data;
using SmartPathBackend.Interfaces;
using SmartPathBackend.Interfaces.Repositories;
using SmartPathBackend.Interfaces.Services;
using SmartPathBackend.Models.DTOs;
using SmartPathBackend.Models.Options;
using SmartPathBackend.Repositories;
using SmartPathBackend.Services;
using SmartPathBackend.Utils;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
var jwt = builder.Configuration.GetSection("Jwt").Get<JwtOptions>();
var keyBytes = Convert.FromBase64String(jwt.Base64Key);
var signingKey = new SymmetricSecurityKey(keyBytes);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = signingKey,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = ClaimTypes.Name,
            RoleClaimType = ClaimTypes.Role
        };

        o.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) &&
                    path.StartsWithSegments("/hubs/message"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        policy =>
        {
            policy
                .WithOrigins("http://localhost:3000", "https://localhost:3000", "https://smartpath.id.vn")
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
});

builder.Services.AddSignalR()
    .AddJsonProtocol(o => { o.PayloadSerializerOptions.PropertyNamingPolicy = null; })
    .AddHubOptions<MessageHub>(o =>   
     {
         o.EnableDetailedErrors = true;                       
         o.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
         o.KeepAliveInterval = TimeSpan.FromSeconds(15);
     });

builder.Services.AddHttpClient();

builder.Services.AddSingleton<IAmazonS3>(sp =>
{
    var r2 = sp.GetRequiredService<IOptions<R2Options>>().Value;

    var cfg = new AmazonS3Config
    {
        ServiceURL = r2.ServiceUrl.TrimEnd('/'),   
        ForcePathStyle = true
    };

    var creds = new BasicAWSCredentials(r2.AccessKeyId, r2.SecretAccessKey);
    return new AmazonS3Client(creds, cfg);
});

builder.Services.Configure<LLMOptions>(builder.Configuration.GetSection("LLM"));

builder.Services.AddHttpClient("Gemini", (sp, http) =>
{
    var opt = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<LLMOptions>>().Value;
    http.BaseAddress = new Uri(opt.BaseUrl ?? "https://generativelanguage.googleapis.com");
    if (!string.IsNullOrWhiteSpace(opt.ApiKey))
        http.DefaultRequestHeaders.Add("x-goog-api-key", opt.ApiKey);
    http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
});

builder.Services.AddHttpClient("LocalLLM", c =>
{
    c.BaseAddress = new Uri("http://127.0.0.1:8000/v1");
    c.Timeout = TimeSpan.FromMinutes(5);
    c.DefaultRequestHeaders.ConnectionClose = false;
}).ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
{
    PooledConnectionLifetime = TimeSpan.FromMinutes(10),
    ConnectTimeout = TimeSpan.FromSeconds(10)
});

builder.Services.AddHttpClient<IEmbedderService, OllamaEmbedderService>();

builder.Services.Configure<ImgBbOptions>(builder.Configuration.GetSection("ImgBB"));
builder.Services.Configure<R2Options>(builder.Configuration.GetSection("R2"));
builder.Services.Configure<UploadPolicyOptions>(builder.Configuration.GetSection("UploadPolicy"));

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IPostRepository, PostRepository>();
builder.Services.AddScoped<ICommentRepository, CommentRepository>();
builder.Services.AddScoped<IReactionRepository, ReactionRepository>();
builder.Services.AddScoped<IReportRepository, ReportRepository>();
builder.Services.AddScoped<IFriendshipRepository, FriendshipRepository>();
builder.Services.AddScoped<IChatRepository, ChatRepository>();
builder.Services.AddScoped<IMessageRepository, MessageRepository>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<ISystemLogRepository, SystemLogRepository>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<IMaterialRepository, MaterialRepository>();
builder.Services.AddScoped<IBadgeRepository, BadgeRepository>();
builder.Services.AddScoped<IBotConversationRepository, BotConversationRepository>();
builder.Services.AddScoped<IBotMessageRepository, BotMessageRepository>();
builder.Services.AddScoped<IKnowledgeRepository, KnowledgeRepository>();
builder.Services.AddScoped<IMaterialCategoryRepository, MaterialCategoryRepository>();
builder.Services.AddScoped<IStudyMaterialRepository, StudyMaterialRepository>();

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IPostService, PostService>();
builder.Services.AddScoped<ICommentService, CommentService>();
builder.Services.AddScoped<IReactionService, ReactionService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<ISystemLogService, SystemLogService>();
builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddScoped<IMessageService, MessageService>();
builder.Services.AddScoped<IFriendshipService, FriendshipService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IMaterialService, MaterialService>();
builder.Services.AddScoped<IBadgeService, BadgeService>();
builder.Services.AddScoped<IBotService, BotService>();
builder.Services.AddScoped<ILLMProvider, GeminiLLMProvider>();
builder.Services.AddScoped<ILLMService, LLMService>();
builder.Services.AddScoped<ILLMProvider, LocalLLMProvider>();
builder.Services.AddScoped<IReputationService, ReputationService>();
builder.Services.AddScoped<IKnowledgeService, KnowledgeService>();
builder.Services.AddScoped<ISearchService, SearchService>();
builder.Services.AddScoped<IMaterialCategoryTreeService, MaterialCategoryTreeService>();
builder.Services.AddScoped<IStudyMaterialLibraryService, StudyMaterialLibraryService>();
builder.Services.AddScoped<IStudyMaterialAiReviewer, StudyMaterialAiReviewer>();

builder.Services.AddDbContext<SmartPathDbContext>(options =>
{
    var cs = builder.Configuration.GetConnectionString("DefaultConnection")!;
    options.UseNpgsql(cs, npgsql =>
    {
        npgsql.UseVector();         
        // npgsql.EnableRetryOnFailure(); 
    });
});

builder.Services.AddAutoMapper(cfg => {
}, typeof(MappingProfile).Assembly);

builder.Services.AddControllers().AddJsonOptions(o =>
{
    o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()); 
});

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapHub<MessageHub>("/hubs/message");

//data will be seed if no users exist
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SmartPathDbContext>();
    await db.Database.MigrateAsync();
    var needSeed = !await db.Users.AnyAsync(); 
    if (needSeed)
    {
        await SeedData.SeedAsync(db);
    }
}

app.Run();
