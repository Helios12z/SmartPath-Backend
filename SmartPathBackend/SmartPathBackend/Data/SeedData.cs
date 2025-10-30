using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using SmartPathBackend.Models.Entities;
using SmartPathBackend.Models.Enums;

namespace SmartPathBackend.Data
{
    public static class SeedData
    {
        private static readonly DateTime T0 = new DateTime(2025, 10, 30, 00, 00, 00, DateTimeKind.Utc);

        public static async Task SeedAsync(SmartPathDbContext db, CancellationToken ct = default)
        {
            // Đảm bảo DB/migrations đã lên
            await db.Database.MigrateAsync(ct);

            // ===== 1) USERS (10) =====
            var users = BuildUsers();
            await UpsertByIdAsync(db.Users, users, ct);

            // ===== 2) BADGES (10) ===== (unique by Name/Point)
            var badges = BuildBadges();
            await UpsertBadgesAsync(db, badges, ct);

            // ===== 3) CATEGORIES (10) ===== (unique by Name)
            var categories = BuildCategories();
            await UpsertCategoriesAsync(db, categories, ct);

            // ===== 4) POSTS (10) =====
            var posts = BuildPosts();
            await UpsertByIdAsync(db.Posts, posts, ct);

            // ===== 5) CATEGORY_POST (10) ===== (map theo Name giống SQL)
            var categoryPosts = BuildCategoryPosts(posts, categories);
            await UpsertCategoryPostsAsync(db, categoryPosts, ct);

            // ===== 6) COMMENTS (10) =====
            var comments = BuildComments();
            await UpsertByIdAsync(db.Comments, comments, ct);

            // ===== 7) REACTIONS (10) ===== (partial unique)
            var reactions = BuildReactions();
            await UpsertReactionsAsync(db, reactions, ct);

            // ===== 8) FRIENDSHIPS (10) ===== (unique pair)
            var friendships = BuildFriendships();
            await UpsertByIdAsync(db.Friendships, friendships, ct);

            // ===== 9) CHATS (10) =====
            var chats = BuildChats();
            await UpsertByIdAsync(db.Chats, chats, ct);

            // ===== 10) MESSAGES (10) =====
            var messages = BuildMessages();
            await UpsertMessagesAsync(db, messages, ct);

            // ===== 13) REPORTS (10) =====
            var reports = BuildReports();
            await UpsertByIdAsync(db.Reports, reports, ct);

            var reps = BuildReputationCheckpoints();
            await UpsertRepCheckpointsAsync(db, reps, ct);

            await db.SaveChangesAsync(ct);
        }

        // ----------------- Builders -----------------

        private static List<User> BuildUsers() => new()
        {
            new User{ Id=Guid.Parse("11111111-1111-1111-1111-111111111111"), Email="alice@demo.local", Password="$2a$12$0KOTHwVrd95i9I9mycWJ7eYMBX.9XP2PP8H4xHWSgICzCOzW7k1Bu", Username="alice", PhoneNumber=null, FullName="Alice Nguyen", Major="CS", Faculty="Engineering", YearOfStudy=2, Bio="Loves algorithms", AvatarUrl=null, Role=Models.Enums.Role.Student, Point=120, CreatedAt=T0 },
            new User{ Id=Guid.Parse("22222222-2222-2222-2222-222222222222"), Email="nnguyenminhquang786@gmail.com", Password="$2a$12$0KOTHwVrd95i9I9mycWJ7eYMBX.9XP2PP8H4xHWSgICzCOzW7k1Bu", Username="bob",   PhoneNumber=null, FullName="Nguyen Minh Quang", Major="SE", Faculty="Engineering", YearOfStudy=3, Bio="Backend enthusiast", AvatarUrl=null, Role=Models.Enums.Role.Admin, Point=240, CreatedAt=T0 },
            new User{ Id=Guid.Parse("33333333-3333-3333-3333-333333333333"), Email="carol@demo.local", Password="$2a$12$0KOTHwVrd95i9I9mycWJ7eYMBX.9XP2PP8H4xHWSgICzCOzW7k1Bu", Username="carol", PhoneNumber=null, FullName="Carol Pham",  Major="Math", Faculty="Science", YearOfStudy=1, Bio="Linear algebra fan", AvatarUrl=null, Role=Models.Enums.Role.Student, Point=380, CreatedAt=T0 },
            new User{ Id=Guid.Parse("44444444-4444-4444-4444-444444444444"), Email="david@demo.local", Password="$2a$12$0KOTHwVrd95i9I9mycWJ7eYMBX.9XP2PP8H4xHWSgICzCOzW7k1Bu", Username="david", PhoneNumber=null, FullName="David Le",    Major="IT", Faculty="Engineering", YearOfStudy=4, Bio="DB & DevOps", AvatarUrl=null, Role=Models.Enums.Role.Student, Point=560, CreatedAt=T0 },
            new User{ Id=Guid.Parse("55555555-5555-5555-5555-555555555555"), Email="eve@demo.local", Password="$2a$12$0KOTHwVrd95i9I9mycWJ7eYMBX.9XP2PP8H4xHWSgICzCOzW7k1Bu", Username="eve",   PhoneNumber=null, FullName="Eve Dang",    Major="SE", Faculty="Engineering", YearOfStudy=2, Bio="Frontend hobbyist", AvatarUrl=null, Role=Models.Enums.Role.Student, Point=720, CreatedAt=T0 },
            new User{ Id=Guid.Parse("66666666-6666-6666-6666-666666666666"), Email="frank@demo.local",Password="$2a$12$0KOTHwVrd95i9I9mycWJ7eYMBX.9XP2PP8H4xHWSgICzCOzW7k1Bu", Username="frank", PhoneNumber=null, FullName="Frank Vo",    Major="CS", Faculty="Engineering", YearOfStudy=3, Bio="Systems learner", AvatarUrl=null, Role=Models.Enums.Role.Student, Point=80,  CreatedAt=T0 },
            new User{ Id=Guid.Parse("77777777-7777-7777-7777-777777777777"), Email="grace@demo.local", Password="$2a$12$0KOTHwVrd95i9I9mycWJ7eYMBX.9XP2PP8H4xHWSgICzCOzW7k1Bu", Username="grace", PhoneNumber=null, FullName="Grace Ho",    Major="Math", Faculty="Science", YearOfStudy=1, Bio="Graph theory", AvatarUrl=null, Role=Models.Enums.Role.Student, Point=910, CreatedAt=T0 },
            new User{ Id=Guid.Parse("88888888-8888-8888-8888-888888888888"), Email="heidi@demo.local", Password="$2a$12$0KOTHwVrd95i9I9mycWJ7eYMBX.9XP2PP8H4xHWSgICzCOzW7k1Bu", Username="heidi", PhoneNumber=null, FullName="Heidi Do",    Major="CS", Faculty="Engineering", YearOfStudy=2, Bio="UI/UX", AvatarUrl=null, Role=Models.Enums.Role.Student, Point=40,  CreatedAt=T0 },
            new User{ Id=Guid.Parse("99999999-9999-9999-9999-999999999999"), Email="ivan@demo.local", Password="$2a$12$0KOTHwVrd95i9I9mycWJ7eYMBX.9XP2PP8H4xHWSgICzCOzW7k1Bu", Username="ivan",  PhoneNumber=null, FullName="Ivan Phan",   Major="CS", Faculty="Engineering", YearOfStudy=4, Bio="Security curious", AvatarUrl=null, Role=Models.Enums.Role.Student, Point=150, CreatedAt=T0 },
            new User{ Id=Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), Email="judy@demo.local", Password="$2a$12$0KOTHwVrd95i9I9mycWJ7eYMBX.9XP2PP8H4xHWSgICzCOzW7k1Bu", Username="judy",  PhoneNumber=null, FullName="Judy Truong", Major="SE", Faculty="Engineering", YearOfStudy=3, Bio="Testing advocate", AvatarUrl=null, Role=Models.Enums.Role.Student, Point=305, CreatedAt=T0 },
        };

        private static List<Badge> BuildBadges()
        {
            // gen_random_uuid() -> new Guid cho từng badge
            return new()
            {
                new Badge{ Id=Guid.NewGuid(), Point=0,    Name="Intern" },
                new Badge{ Id=Guid.NewGuid(), Point=100,  Name="Wolf Coder" },
                new Badge{ Id=Guid.NewGuid(), Point=250,  Name="Fresher" },
                new Badge{ Id=Guid.NewGuid(), Point=350,  Name="Demonic Coder" },
                new Badge{ Id=Guid.NewGuid(), Point=500,  Name="Junior Dev" },
                new Badge{ Id=Guid.NewGuid(), Point=650,  Name="Dragon Coder" },
                new Badge{ Id=Guid.NewGuid(), Point=800,  Name="Lightning Dev" },
                new Badge{ Id=Guid.NewGuid(), Point=900,  Name="Super Senior" },
                new Badge{ Id=Guid.NewGuid(), Point=950,  Name="Code God" },
                new Badge{ Id=Guid.NewGuid(), Point=1000, Name="Champion" },
            };
        }

        private static List<Category> BuildCategories()
        {
            string[] names = {
                "General","Q&A","Tutorials","Mathematics","Computer Science",
                "Databases","Algorithms","Data Structures","DevOps","Web Development"
            };
            return names.Select(n => new Category { Id = Guid.NewGuid(), Name = n }).ToList();
        }

        private static List<Post> BuildPosts() => new()
        {
            new Post{ Id=Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1"), AuthorId=Guid.Parse("11111111-1111-1111-1111-111111111111"), Title="Welcome to SmartPath", Content="First post content", IsQuestion=false, CreatedAt=T0, UpdatedAt=T0, IsDeletedAt=null },
            new Post{ Id=Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa2"), AuthorId=Guid.Parse("22222222-2222-2222-2222-222222222222"), Title="Study algorithms?", Content="Any recommended resources?", IsQuestion=true, CreatedAt=T0, UpdatedAt=T0, IsDeletedAt=null },
            new Post{ Id=Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa3"), AuthorId=Guid.Parse("33333333-3333-3333-3333-333333333333"), Title="Linear Algebra tips", Content="Share your best tips", IsQuestion=false, CreatedAt=T0, UpdatedAt=T0, IsDeletedAt=null },
            new Post{ Id=Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa4"), AuthorId=Guid.Parse("44444444-4444-4444-4444-444444444444"), Title="DB normalization", Content="3NF vs BCNF", IsQuestion=true, CreatedAt=T0, UpdatedAt=T0, IsDeletedAt=null },
            new Post{ Id=Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa5"), AuthorId=Guid.Parse("55555555-5555-5555-5555-555555555555"), Title="Git workflow", Content="Git flow vs trunk-based", IsQuestion=false, CreatedAt=T0, UpdatedAt=T0, IsDeletedAt=null },
            new Post{ Id=Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa6"), AuthorId=Guid.Parse("66666666-6666-6666-6666-666666666666"), Title="Pointers in C", Content="How to avoid segfault?", IsQuestion=true, CreatedAt=T0, UpdatedAt=T0, IsDeletedAt=null },
            new Post{ Id=Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa7"), AuthorId=Guid.Parse("77777777-7777-7777-7777-777777777777"), Title="Graph problems", Content="Minimum cut examples", IsQuestion=false, CreatedAt=T0, UpdatedAt=T0, IsDeletedAt=null },
            new Post{ Id=Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa8"), AuthorId=Guid.Parse("88888888-8888-8888-8888-888888888888"), Title="UI libraries", Content="Shadcn vs Mantine?", IsQuestion=true, CreatedAt=T0, UpdatedAt=T0, IsDeletedAt=null },
            new Post{ Id=Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa9"), AuthorId=Guid.Parse("99999999-9999-9999-9999-999999999999"), Title="JWT refresh", Content="Best practices", IsQuestion=false, CreatedAt=T0, UpdatedAt=T0, IsDeletedAt=null },
            new Post{ Id=Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa10"), AuthorId=Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), Title="Unit testing", Content="xUnit vs NUnit", IsQuestion=true, CreatedAt=T0, UpdatedAt=T0, IsDeletedAt=null },
        };

        private static List<CategoryPost> BuildCategoryPosts(List<Post> posts, List<Category> categories)
        {
            Category Cat(string name) => categories.Single(c => c.Name == name);
            return new()
            {
                new CategoryPost{ PostId=posts[0].Id, CategoryId=Cat("General").Id },
                new CategoryPost{ PostId=posts[1].Id, CategoryId=Cat("Algorithms").Id },
                new CategoryPost{ PostId=posts[2].Id, CategoryId=Cat("Mathematics").Id },
                new CategoryPost{ PostId=posts[3].Id, CategoryId=Cat("Databases").Id },
                new CategoryPost{ PostId=posts[4].Id, CategoryId=Cat("Web Development").Id },
                new CategoryPost{ PostId=posts[5].Id, CategoryId=Cat("Computer Science").Id },
                new CategoryPost{ PostId=posts[6].Id, CategoryId=Cat("Data Structures").Id },
                new CategoryPost{ PostId=posts[7].Id, CategoryId=Cat("General").Id },
                new CategoryPost{ PostId=posts[8].Id, CategoryId=Cat("Computer Science").Id },
                new CategoryPost{ PostId=posts[9].Id, CategoryId=Cat("Q&A").Id },
            };
        }

        private static List<Comment> BuildComments() => new()
        {
            new Comment{ Id=Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1"), AuthorId=Guid.Parse("22222222-2222-2222-2222-222222222222"), PostId=Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1"), Content="Great to be here!", CreatedAt=T0, ParentCommentId=null },
            new Comment{ Id=Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb2"), AuthorId=Guid.Parse("33333333-3333-3333-3333-333333333333"), PostId=Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa2"), Content="CLRS + LeetCode patterns", CreatedAt=T0, ParentCommentId=null },
            new Comment{ Id=Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb3"), AuthorId=Guid.Parse("44444444-4444-4444-4444-444444444444"), PostId=Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa3"), Content="Khan Academy is good", CreatedAt=T0, ParentCommentId=null },
            new Comment{ Id=Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb4"), AuthorId=Guid.Parse("55555555-5555-5555-5555-555555555555"), PostId=Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa4"), Content="BCNF stricter than 3NF", CreatedAt=T0, ParentCommentId=null },
            new Comment{ Id=Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb5"), AuthorId=Guid.Parse("11111111-1111-1111-1111-111111111111"), PostId=Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa2"), Content="Thanks! Any YouTube?", CreatedAt=T0, ParentCommentId=Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb2") },
            new Comment{ Id=Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb6"), AuthorId=Guid.Parse("66666666-6666-6666-6666-666666666666"), PostId=Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa5"), Content="Prefer trunk-based", CreatedAt=T0, ParentCommentId=null },
            new Comment{ Id=Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb7"), AuthorId=Guid.Parse("77777777-7777-7777-7777-777777777777"), PostId=Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa7"), Content="Min cut via max flow", CreatedAt=T0, ParentCommentId=null },
            new Comment{ Id=Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb8"), AuthorId=Guid.Parse("88888888-8888-8888-8888-888888888888"), PostId=Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa8"), Content="Shadcn feels modern", CreatedAt=T0, ParentCommentId=null },
            new Comment{ Id=Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb9"), AuthorId=Guid.Parse("99999999-9999-9999-9999-999999999999"), PostId=Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa9"), Content="Rotate refresh tokens", CreatedAt=T0, ParentCommentId=null },
            new Comment{ Id=Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbc10"), AuthorId=Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), PostId=Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa10"), Content="xUnit has nice asserts", CreatedAt=T0, ParentCommentId=null },
        };

        private static List<Reaction> BuildReactions() => new()
        {
            new Reaction{ Id=Guid.NewGuid(), UserId=Guid.Parse("33333333-3333-3333-3333-333333333333"), PostId=Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1"), CommentId=null, IsPositive=true },
            new Reaction{ Id=Guid.NewGuid(), UserId=Guid.Parse("44444444-4444-4444-4444-444444444444"), PostId=Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1"), CommentId=null, IsPositive=true },
            new Reaction{ Id=Guid.NewGuid(), UserId=Guid.Parse("55555555-5555-5555-5555-555555555555"), PostId=Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa2"), CommentId=null, IsPositive=true },
            new Reaction{ Id=Guid.NewGuid(), UserId=Guid.Parse("11111111-1111-1111-1111-111111111111"), PostId=null, CommentId=Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb2"), IsPositive=true },
            new Reaction{ Id=Guid.NewGuid(), UserId=Guid.Parse("22222222-2222-2222-2222-222222222222"), PostId=null, CommentId=Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb5"), IsPositive=true },
            new Reaction{ Id=Guid.NewGuid(), UserId=Guid.Parse("66666666-6666-6666-6666-666666666666"), PostId=Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa5"), CommentId=null, IsPositive=true },
            new Reaction{ Id=Guid.NewGuid(), UserId=Guid.Parse("77777777-7777-7777-7777-777777777777"), PostId=Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa7"), CommentId=null, IsPositive=true },
            new Reaction{ Id=Guid.NewGuid(), UserId=Guid.Parse("88888888-8888-8888-8888-888888888888"), PostId=Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa8"), CommentId=null, IsPositive=true },
            new Reaction{ Id=Guid.NewGuid(), UserId=Guid.Parse("99999999-9999-9999-9999-999999999999"), PostId=null, CommentId=Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb6"), IsPositive=true },
            new Reaction{ Id=Guid.NewGuid(), UserId=Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), PostId=Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa10"), CommentId=null, IsPositive=true },
        };

        private static List<Friendship> BuildFriendships() => new()
        {
            new Friendship{ Id=Guid.NewGuid(), FollowerId=Guid.Parse("11111111-1111-1111-1111-111111111111"), FollowedUserId=Guid.Parse("22222222-2222-2222-2222-222222222222"), CreatedAt=T0 },
            new Friendship{ Id=Guid.NewGuid(), FollowerId=Guid.Parse("11111111-1111-1111-1111-111111111111"), FollowedUserId=Guid.Parse("33333333-3333-3333-3333-333333333333"), CreatedAt=T0 },
            new Friendship{ Id=Guid.NewGuid(), FollowerId=Guid.Parse("22222222-2222-2222-2222-222222222222"), FollowedUserId=Guid.Parse("33333333-3333-3333-3333-333333333333"), CreatedAt=T0 },
            new Friendship{ Id=Guid.NewGuid(), FollowerId=Guid.Parse("33333333-3333-3333-3333-333333333333"), FollowedUserId=Guid.Parse("44444444-4444-4444-4444-444444444444"), CreatedAt=T0 },
            new Friendship{ Id=Guid.NewGuid(), FollowerId=Guid.Parse("44444444-4444-4444-4444-444444444444"), FollowedUserId=Guid.Parse("55555555-5555-5555-5555-555555555555"), CreatedAt=T0 },
            new Friendship{ Id=Guid.NewGuid(), FollowerId=Guid.Parse("55555555-5555-5555-5555-555555555555"), FollowedUserId=Guid.Parse("66666666-6666-6666-6666-666666666666"), CreatedAt=T0 },
            new Friendship{ Id=Guid.NewGuid(), FollowerId=Guid.Parse("66666666-6666-6666-6666-666666666666"), FollowedUserId=Guid.Parse("77777777-7777-7777-7777-777777777777"), CreatedAt=T0 },
            new Friendship{ Id=Guid.NewGuid(), FollowerId=Guid.Parse("77777777-7777-7777-7777-777777777777"), FollowedUserId=Guid.Parse("88888888-8888-8888-8888-888888888888"), CreatedAt=T0 },
            new Friendship{ Id=Guid.NewGuid(), FollowerId=Guid.Parse("88888888-8888-8888-8888-888888888888"), FollowedUserId=Guid.Parse("99999999-9999-9999-9999-999999999999"), CreatedAt=T0 },
            new Friendship{ Id=Guid.NewGuid(), FollowerId=Guid.Parse("99999999-9999-9999-9999-999999999999"), FollowedUserId=Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), CreatedAt=T0 },
        };

        private static List<Chat> BuildChats()
        {
            // Giữ đúng 10 GUID như SQL
            string[] ids =
            {
                "c0c0c0c0-0000-0000-0000-000000000001",
                "c0c0c0c0-0000-0000-0000-000000000002",
                "c0c0c0c0-0000-0000-0000-000000000003",
                "c0c0c0c0-0000-0000-0000-000000000004",
                "c0c0c0c0-0000-0000-0000-000000000005",
                "c0c0c0c0-0000-0000-0000-000000000006",
                "c0c0c0c0-0000-0000-0000-000000000007",
                "c0c0c0c0-0000-0000-0000-000000000008",
                "c0c0c0c0-0000-0000-0000-000000000009",
                "c0c0c0c0-0000-0000-0000-000000000010",
            };
            return ids.Select(s => new Chat { Id = Guid.Parse(s) }).ToList();
        }

        private static List<Message> BuildMessages() => new()
        {
            new Message{ Id=Guid.NewGuid(), ChatId=Guid.Parse("c0c0c0c0-0000-0000-0000-000000000001"), SenderId=Guid.Parse("11111111-1111-1111-1111-111111111111"), Content="Hi Bob!", CreatedAt=T0 },
            new Message{ Id=Guid.NewGuid(), ChatId=Guid.Parse("c0c0c0c0-0000-0000-0000-000000000001"), SenderId=Guid.Parse("22222222-2222-2222-2222-222222222222"), Content="Hi Alice!", CreatedAt=T0 },
            new Message{ Id=Guid.NewGuid(), ChatId=Guid.Parse("c0c0c0c0-0000-0000-0000-000000000002"), SenderId=Guid.Parse("33333333-3333-3333-3333-333333333333"), Content="Anyone up for study group?", CreatedAt=T0 },
            new Message{ Id=Guid.NewGuid(), ChatId=Guid.Parse("c0c0c0c0-0000-0000-0000-000000000003"), SenderId=Guid.Parse("44444444-4444-4444-4444-444444444444"), Content="DB tips welcome", CreatedAt=T0 },
            new Message{ Id=Guid.NewGuid(), ChatId=Guid.Parse("c0c0c0c0-0000-0000-0000-000000000004"), SenderId=Guid.Parse("55555555-5555-5555-5555-555555555555"), Content="Trunk-based works well", CreatedAt=T0 },
            new Message{ Id=Guid.NewGuid(), ChatId=Guid.Parse("c0c0c0c0-0000-0000-0000-000000000005"), SenderId=Guid.Parse("66666666-6666-6666-6666-666666666666"), Content="C pointers are tricky", CreatedAt=T0 },
            new Message{ Id=Guid.NewGuid(), ChatId=Guid.Parse("c0c0c0c0-0000-0000-0000-000000000006"), SenderId=Guid.Parse("77777777-7777-7777-7777-777777777777"), Content="Max flow solved it", CreatedAt=T0 },
            new Message{ Id=Guid.NewGuid(), ChatId=Guid.Parse("c0c0c0c0-0000-0000-0000-000000000007"), SenderId=Guid.Parse("88888888-8888-8888-8888-888888888888"), Content="Shadcn is neat", CreatedAt=T0 },
            new Message{ Id=Guid.NewGuid(), ChatId=Guid.Parse("c0c0c0c0-0000-0000-0000-000000000008"), SenderId=Guid.Parse("99999999-9999-9999-9999-999999999999"), Content="JWT rotation is a must", CreatedAt=T0 },
            new Message{ Id=Guid.NewGuid(), ChatId=Guid.Parse("c0c0c0c0-0000-0000-0000-000000000009"), SenderId=Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), Content="NUnit vs xUnit thoughts", CreatedAt=T0 },
        };

        private static List<Report> BuildReports() => new()
        {
            new Report{
                Id = Guid.NewGuid(),
                ReporterId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                CommentId  = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb3"),
                Reason     = "spam",
                Status     = Status.Pending,
                CreatedAt  = T0
            },

            new Report{
                Id = Guid.NewGuid(),
                ReporterId    = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                ReportedUserId= Guid.Parse("55555555-5555-5555-5555-555555555555"),
                Reason        = "abuse",
                Status        = Status.Pending,
                CreatedAt     = T0
            },

            new Report{
                Id = Guid.NewGuid(),
                ReporterId = Guid.Parse("44444444-4444-4444-4444-444444444444"),
                PostId     = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa2"),
                Reason     = "duplicate",
                Status     = Status.Pending,
                CreatedAt  = T0
            },

            // misplaced trên 1 post (Git workflow)
            new Report{
                Id = Guid.NewGuid(),
                ReporterId = Guid.Parse("55555555-5555-5555-5555-555555555555"),
                PostId     = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa5"),
                Reason     = "misplaced",
                Status     = Status.Pending,
                CreatedAt  = T0
            },

            new Report{
                Id = Guid.NewGuid(),
                ReporterId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                PostId     = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa3"),
                Reason     = "plagiarism",
                Status     = Status.Pending,
                CreatedAt  = T0
            },

            new Report{
                Id = Guid.NewGuid(),
                ReporterId = Guid.Parse("66666666-6666-6666-6666-666666666666"),
                CommentId  = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb6"),
                Reason     = "spam",
                Status     = Status.Pending,
                CreatedAt  = T0
            },

            new Report{
                Id = Guid.NewGuid(),
                ReporterId    = Guid.Parse("77777777-7777-7777-7777-777777777777"),
                ReportedUserId= Guid.Parse("88888888-8888-8888-8888-888888888888"),
                Reason        = "abuse",
                Status        = Status.Pending,
                CreatedAt     = T0
            },

            new Report{
                Id = Guid.NewGuid(),
                ReporterId = Guid.Parse("88888888-8888-8888-8888-888888888888"),
                PostId     = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa9"),
                Reason     = "duplicate",
                Status     = Status.Pending,
                CreatedAt  = T0
            },

            new Report{
                Id = Guid.NewGuid(),
                ReporterId = Guid.Parse("99999999-9999-9999-9999-999999999999"),
                PostId     = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa8"),
                Reason     = "offtopic",
                Status     = Status.Pending,
                CreatedAt  = T0
            },

            new Report{
                Id = Guid.NewGuid(),
                ReporterId    = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                ReportedUserId= Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Reason        = "spam",
                Status        = Status.Pending,
                CreatedAt     = T0
            },
        };

        private static List<ReputationCheckpoint> BuildReputationCheckpoints() => new()
        {
            new ReputationCheckpoint{ Id=Guid.NewGuid(), ContentType=ContentType.Post,    ContentId=Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1"), LikeBandsApplied=1, DislikeBandsApplied=0 },
            new ReputationCheckpoint{ Id=Guid.NewGuid(), ContentType=ContentType.Post,    ContentId=Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa2"), LikeBandsApplied=1, DislikeBandsApplied=0 },
            new ReputationCheckpoint{ Id=Guid.NewGuid(), ContentType=ContentType.Post,    ContentId=Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa3"), LikeBandsApplied=2, DislikeBandsApplied=0 },
            new ReputationCheckpoint{ Id=Guid.NewGuid(), ContentType=ContentType.Post,    ContentId=Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa4"), LikeBandsApplied=1, DislikeBandsApplied=0 },
            new ReputationCheckpoint{ Id=Guid.NewGuid(), ContentType=ContentType.Post,    ContentId=Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa5"), LikeBandsApplied=2, DislikeBandsApplied=1 },
            new ReputationCheckpoint{ Id=Guid.NewGuid(), ContentType=ContentType.Comment, ContentId=Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb2"), LikeBandsApplied=1, DislikeBandsApplied=0 },
            new ReputationCheckpoint{ Id=Guid.NewGuid(), ContentType=ContentType.Comment, ContentId=Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb5"), LikeBandsApplied=1, DislikeBandsApplied=0 },
            new ReputationCheckpoint{ Id=Guid.NewGuid(), ContentType=ContentType.Comment, ContentId=Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb6"), LikeBandsApplied=1, DislikeBandsApplied=0 },
            new ReputationCheckpoint{ Id=Guid.NewGuid(), ContentType=ContentType.Post,    ContentId=Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa7"), LikeBandsApplied=2, DislikeBandsApplied=0 },
            new ReputationCheckpoint{ Id=Guid.NewGuid(), ContentType=ContentType.Post,    ContentId=Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa9"), LikeBandsApplied=1, DislikeBandsApplied=0 },
        };

        // ----------------- Upsert helpers -----------------

        private static async Task UpsertByIdAsync<T>(DbSet<T> set, IEnumerable<T> items, CancellationToken ct) where T : class
        {
            var ctx = set.GetService<ICurrentDbContext>().Context;
            var entityType = ctx.Model.FindEntityType(typeof(T))!;
            var key = entityType.FindPrimaryKey()!;
            var keyProp = key.Properties.Single();

            // lấy các id đã có
            var existing = await set.AsNoTracking().ToListAsync(ct);
            var existingIds = new HashSet<object>(existing.Select(e => entityType.FindPrimaryKey()!.Properties.Single().PropertyInfo!.GetValue(e)!));

            foreach (var it in items)
            {
                var id = keyProp.PropertyInfo!.GetValue(it)!;
                if (!existingIds.Contains(id))
                {
                    await set.AddAsync(it, ct);
                }
            }
        }

        private static async Task UpsertBadgesAsync(SmartPathDbContext db, IEnumerable<Badge> badges, CancellationToken ct)
        {
            var existingNames = await db.Badges.AsNoTracking().Select(b => b.Name).ToListAsync(ct);
            var existingPoints = await db.Badges.AsNoTracking().Select(b => b.Point).ToListAsync(ct);
            foreach (var b in badges)
            {
                if (!existingNames.Contains(b.Name) && !existingPoints.Contains(b.Point))
                    await db.Badges.AddAsync(b, ct);
            }
        }

        private static async Task UpsertCategoriesAsync(SmartPathDbContext db, IEnumerable<Category> categories, CancellationToken ct)
        {
            var existing = await db.Categories.AsNoTracking().Select(c => c.Name).ToListAsync(ct);
            foreach (var c in categories)
            {
                if (!existing.Contains(c.Name))
                    await db.Categories.AddAsync(c, ct);
            }
        }

        private static async Task UpsertCategoryPostsAsync(SmartPathDbContext db, IEnumerable<CategoryPost> links, CancellationToken ct)
        {
            var existing = await db.CategoryPosts.AsNoTracking().ToListAsync(ct);
            var set = new HashSet<(Guid, Guid)>(existing.Select(x => (x.PostId, x.CategoryId)));
            foreach (var l in links)
            {
                if (!set.Contains((l.PostId, l.CategoryId)))
                    await db.CategoryPosts.AddAsync(l, ct);
            }
        }

        private static async Task UpsertReactionsAsync(SmartPathDbContext db, IEnumerable<Reaction> reactions, CancellationToken ct)
        {
            // Dựa vào unique partial index, coi như tồn tại khi (UserId,PostId) hoặc (UserId,CommentId) đã có
            var existPostPairs = await db.Reactions.AsNoTracking()
                .Where(r => r.PostId != null)
                .Select(r => new { r.UserId, r.PostId })
                .ToListAsync(ct);

            var existCommentPairs = await db.Reactions.AsNoTracking()
                .Where(r => r.CommentId != null)
                .Select(r => new { r.UserId, r.CommentId })
                .ToListAsync(ct);

            foreach (var r in reactions)
            {
                if (r.PostId != null)
                {
                    if (!existPostPairs.Any(x => x.UserId == r.UserId && x.PostId == r.PostId))
                        await db.Reactions.AddAsync(r, ct);
                }
                else if (r.CommentId != null)
                {
                    if (!existCommentPairs.Any(x => x.UserId == r.UserId && x.CommentId == r.CommentId))
                        await db.Reactions.AddAsync(r, ct);
                }
            }
        }

        private static async Task UpsertMessagesAsync(SmartPathDbContext db, IEnumerable<Message> messages, CancellationToken ct)
        {
            // Chèn nếu chưa có (ChatId, SenderId, Content, CreatedAt) — tạm coi là unique tự nhiên
            var existing = await db.Messages.AsNoTracking()
                .Select(m => new { m.ChatId, m.SenderId, m.Content, m.CreatedAt })
                .ToListAsync(ct);

            foreach (var m in messages)
            {
                if (!existing.Any(x => x.ChatId == m.ChatId && x.SenderId == m.SenderId && x.Content == m.Content && x.CreatedAt == m.CreatedAt))
                    await db.Messages.AddAsync(m, ct);
            }
        }

        private static async Task UpsertRepCheckpointsAsync(SmartPathDbContext db, IEnumerable<ReputationCheckpoint> reps, CancellationToken ct)
        {
            var existing = await db.ReputationCheckpoints.AsNoTracking()
                .Select(r => new { r.ContentType, r.ContentId })
                .ToListAsync(ct);

            foreach (var r in reps)
            {
                if (!existing.Any(x => x.ContentType == r.ContentType && x.ContentId == r.ContentId))
                    await db.ReputationCheckpoints.AddAsync(r, ct);
            }
        }
    }
}
