using AutoMapper;
using Microsoft.EntityFrameworkCore;
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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

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
        }
    }
}
