using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SmartPathBackend.Models.Entities;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace SmartPathBackend.Data
{
    public class SmartPathDbContext : DbContext
    {
        public SmartPathDbContext(DbContextOptions<SmartPathDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users => Set<User>();
        public DbSet<Post> Posts => Set<Post>();
        public DbSet<Comment> Comments => Set<Comment>();
        public DbSet<Reaction> Reactions => Set<Reaction>();
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<CategoryPost> CategoryPosts => Set<CategoryPost>();
        public DbSet<Report> Reports => Set<Report>();
        public DbSet<SystemLog> SystemLogs => Set<SystemLog>();
        public DbSet<Badge> Badges => Set<Badge>();
        public DbSet<Friendship> Friendships => Set<Friendship>();
        public DbSet<Chat> Chats => Set<Chat>();
        public DbSet<Message> Messages => Set<Message>();
        public DbSet<Material> Materials => Set<Material>();
        public DbSet<Notification> Notifications => Set<Notification>();
        public DbSet<BotConversation> BotConversations => Set<BotConversation>();
        public DbSet<BotMessage> BotMessages => Set<BotMessage>();
        public DbSet<ReputationCheckpoint> ReputationCheckpoints { get; set; } = default!;
        public DbSet<KnowledgeDocument> KnowledgeDocuments { get; set; } = default!;
        public DbSet<KnowledgeChunk> KnowledgeChunks { get; set; } = default!;
        public DbSet<MaterialCategory> MaterialCategories => Set<MaterialCategory>();
        public DbSet<StudyMaterial> StudyMaterials => Set<StudyMaterial>();
        public DbSet<StudyMaterialReview> StudyMaterialReviews => Set<StudyMaterialReview>();
        public DbSet<StudyMaterialRating> StudyMaterialRatings => Set<StudyMaterialRating>();
        public DbSet<PostSearchIndex> PostSearchIndices => Set<PostSearchIndex>();
        public DbSet<StudyMaterialSearchIndex> StudyMaterialSearchIndices => Set<StudyMaterialSearchIndex>();
        public DbSet<SearchQueryLog> SearchQueryLogs => Set<SearchQueryLog>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.HasPostgresExtension("vector");

            modelBuilder.Entity<KnowledgeDocument>(e =>
            {
                e.ToTable("knowledge_documents");
                e.HasKey(x => x.Id);
                e.Property(x => x.Title).HasMaxLength(512);
                e.Property(x => x.CreatedAt).HasDefaultValueSql("now()");
                e.HasMany(x => x.Chunks)
                 .WithOne(x => x.Document)
                 .HasForeignKey(x => x.DocumentId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<KnowledgeChunk>(e =>
            {
                e.ToTable("knowledge_chunks");
                e.HasKey(x => x.Id);

                e.Property(x => x.ChunkIndex).IsRequired();
                e.Property(x => x.Content).IsRequired();

                e.Property(x => x.Embedding).HasColumnType("vector(1024)");

                e.Property(x => x.CreatedAt).HasDefaultValueSql("now()");
                e.HasIndex(x => x.DocumentId);
            });

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();

            modelBuilder.Entity<Post>()
                .HasOne(p => p.Author)
                .WithMany(u => u.Posts)
                .HasForeignKey(p => p.AuthorId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Comment>()
                .HasOne(c => c.Post)
                .WithMany(p => p.Comments)
                .HasForeignKey(c => c.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Comment>()
                .HasOne(c => c.Author)
                .WithMany(u => u.Comments)
                .HasForeignKey(c => c.AuthorId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Comment>()
                .HasOne(c => c.ParentComment)
                .WithMany(c => c.Replies)
                .HasForeignKey(c => c.ParentCommentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CategoryPost>()
                .HasKey(cp => new { cp.PostId, cp.CategoryId });

            modelBuilder.Entity<CategoryPost>()
                .HasOne(cp => cp.Post)
                .WithMany(p => p.CategoryPosts)
                .HasForeignKey(cp => cp.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CategoryPost>()
                .HasOne(cp => cp.Category)
                .WithMany(c => c.CategoryPosts)
                .HasForeignKey(cp => cp.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Reaction>(eb =>
            {
                eb.HasOne(r => r.Post)
                  .WithMany(p => p.Reactions)
                  .HasForeignKey(r => r.PostId)
                  .IsRequired(false)
                  .OnDelete(DeleteBehavior.Cascade);

                eb.HasOne(r => r.Comment)
                  .WithMany(c => c.Reactions)
                  .HasForeignKey(r => r.CommentId)
                  .IsRequired(false)
                  .OnDelete(DeleteBehavior.Cascade);

                eb.HasOne(r => r.User)
                  .WithMany(u => u.Reactions)
                  .HasForeignKey(r => r.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

                eb.ToTable(t => t.HasCheckConstraint(
                    "ck_reactions_one_target",
                    "((\"PostId\" IS NOT NULL AND \"CommentId\" IS NULL) OR (\"PostId\" IS NULL AND \"CommentId\" IS NOT NULL))"
                ));

                eb.HasIndex(r => new { r.UserId, r.PostId })
                  .IsUnique()
                  .HasFilter("\"PostId\" IS NOT NULL");

                eb.HasIndex(r => new { r.UserId, r.CommentId })
                  .IsUnique()
                  .HasFilter("\"CommentId\" IS NOT NULL");
            });


            modelBuilder.Entity<Report>()
                .HasOne(r => r.Reporter)
                .WithMany()
                .HasForeignKey(r => r.ReporterId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Friendship>()
                .HasOne(f => f.Follower)
                .WithMany()
                .HasForeignKey(f => f.FollowerId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Friendship>()
                .HasOne(f => f.FollowedUser)
                .WithMany()
                .HasForeignKey(f => f.FollowedUserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Message>()
                .HasOne(m => m.Chat)
                .WithMany(c => c.Messages)
                .HasForeignKey(m => m.ChatId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Message>()
                .HasOne(m => m.Sender)
                .WithMany(u => u.Messages)
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Report>()
                .Property(r => r.CreatedAt);

            modelBuilder.Entity<Post>()
                .Property(p => p.CreatedAt);

            modelBuilder.Entity<User>()
                .Property(u => u.CreatedAt);

            modelBuilder.Entity<BotConversation>(b =>
            {
                b.HasKey(x => x.Id);
                b.HasOne(x => x.Owner)
                 .WithMany()
                 .HasForeignKey(x => x.OwnerId)
                 .OnDelete(DeleteBehavior.Cascade);
                b.Property(x => x.Title).HasMaxLength(256);
                b.HasIndex(x => new { x.OwnerId, x.CreatedAt });
                b.HasIndex(x => new { x.OwnerId, x.UpdatedAt });
            });

            modelBuilder.Entity<BotMessage>(b =>
            {
                b.HasKey(x => x.Id);
                b.Property(x => x.Role).HasConversion<int>();
                b.Property(x => x.Content).IsRequired();
                b.HasOne(x => x.Conversation)
                 .WithMany(c => c.Messages)
                 .HasForeignKey(x => x.ConversationId)
                 .OnDelete(DeleteBehavior.Cascade);
                b.HasIndex(x => new { x.ConversationId, x.CreatedAt });
                b.HasIndex(x => new { x.SenderId, x.CreatedAt });
            });

            modelBuilder.Entity<ReputationCheckpoint>(e =>
            {
                e.HasKey(x => x.Id);
                e.HasIndex(x => new { x.ContentType, x.ContentId }).IsUnique();
                e.Property(x => x.LikeBandsApplied).HasDefaultValue(0);
                e.Property(x => x.DislikeBandsApplied).HasDefaultValue(0);
            });

            modelBuilder.Entity<MaterialCategory>()
                .HasIndex(x => x.Slug).IsUnique();

            modelBuilder.Entity<MaterialCategory>()
                .HasOne(x => x.Parent)
                .WithMany(x => x.Children)
                .HasForeignKey(x => x.ParentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StudyMaterial>(b =>
            {
                b.HasIndex(x => new { x.CategoryId, x.Status, x.CreatedAt });
                b.ToTable(t => t.HasCheckConstraint(
                    "ck_studymaterials_one_source",
                    "((\"SourceType\" = 1 AND \"FileUrl\" IS NOT NULL AND \"SourceUrl\" IS NULL) OR " +
                    "(\"SourceType\" = 2 AND \"SourceUrl\" IS NOT NULL AND \"FileUrl\" IS NULL))"
                ));
            });

            // Search Index configurations
            modelBuilder.Entity<PostSearchIndex>(e =>
            {
                e.HasKey(x => x.Id);
                e.HasIndex(x => x.PostId).IsUnique();
                e.HasIndex(x => new { x.CreatedAt, x.IsQuestion });
                e.HasIndex(x => x.AuthorId);

                // Embedding property is marked with [NotMapped] attribute

                e.HasIndex(x => x.Title);
                e.HasIndex(x => x.Content);
            });

            modelBuilder.Entity<StudyMaterialSearchIndex>(e =>
            {
                e.HasKey(x => x.Id);
                e.HasIndex(x => x.StudyMaterialId).IsUnique();
                e.HasIndex(x => new { x.CategoryId, x.IsApproved, x.CreatedAt });
                e.HasIndex(x => x.UploaderId);

                // Embedding property is marked with [NotMapped] attribute

                e.HasIndex(x => x.Title);
                e.HasIndex(x => x.Description);
            });

            modelBuilder.Entity<StudyMaterialRating>(e =>
            {
                e.HasKey(x => x.Id);
                e.HasIndex(x => new { x.MaterialId, x.UserId }).IsUnique(); // One rating per user per material
                e.HasIndex(x => x.MaterialId);
                e.HasIndex(x => x.UserId);
                e.HasIndex(x => x.CreatedAt);

                // Ensure rating is between 1 and 5
                e.HasCheckConstraint("ck_rating_range", "\"Rating\" >= 1 AND \"Rating\" <= 5");
            });

            modelBuilder.Entity<SearchQueryLog>(e =>
            {
                e.HasKey(x => x.Id);
                e.HasIndex(x => x.UserId);
                e.HasIndex(x => x.CreatedAt);
                e.HasIndex(x => new { x.Query, x.CreatedAt });
            });
        }
    }
}
