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

            // ===== 3.1) MATERIAL CATEGORIES (Tree for Study Library) ===== (unique by Slug)
            var materialCategories = BuildMaterialCategories();
            await UpsertMaterialCategoriesAsync(db, materialCategories, ct);

            // ===== 4) POSTS (100+ for pagination testing) =====
            var posts = BuildPosts();
            await UpsertByIdAsync(db.Posts, posts, ct);

            // ===== 5) CATEGORY_POST (map posts to categories) =====
            var categoryPosts = BuildCategoryPosts(posts, categories);
            await UpsertCategoryPostsAsync(db, categoryPosts, ct);

            // ===== 6) COMMENTS (200+ for pagination testing) =====
            var comments = BuildComments();
            await UpsertByIdAsync(db.Comments, comments, ct);

            // ===== 7) REACTIONS (100+) ===== (partial unique)
            var reactions = BuildReactions();
            await UpsertReactionsAsync(db, reactions, ct);

            // ===== 8) CHATS (20) =====
            var chats = BuildChats();
            await UpsertByIdAsync(db.Chats, chats, ct);

            // ===== 9) MESSAGES (500+ for cursor pagination testing) =====
            var messages = BuildMessages(chats);
            await UpsertMessagesAsync(db, messages, ct);

            // ===== 10) FRIENDSHIPS (30) ===== (unique pair)
            var friendships = BuildFriendships();
            await UpsertByIdAsync(db.Friendships, friendships, ct);

            // ===== 11) STUDY MATERIALS (50+ for pagination testing) =====
            var studyMaterials = BuildStudyMaterials();
            await UpsertStudyMaterialsAsync(db, studyMaterials, ct);

            // ===== 12) REPORTS (20) =====
            var reports = BuildReports();
            await UpsertByIdAsync(db.Reports, reports, ct);

            var reps = BuildReputationCheckpoints();
            await UpsertRepCheckpointsAsync(db, reps, ct);

            await db.SaveChangesAsync(ct);
        }

        // ----------------- Builders -----------------

        private static List<User> BuildUsers() => new()
        {
            new User{
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Email = "alice@demo.local",
                Password = "$2a$12$0KOTHwVrd95i9I9mycWJ7eYMBX.9XP2PP8H4xHWSgICzCOzW7k1Bu",
                Username = "alice",
                PhoneNumber = null,
                FullName = "Alice Nguyen",
                Major = "CS",
                Faculty = "Engineering",
                YearOfStudy = 2,
                Bio = "Loves algorithms",
                AvatarUrl = null,
                Role = Role.Student,
                Point = 120,
                CreatedAt = T0,
                IsBanned = false,
                BannedUntil = null,
                BanReason = null
            },
            new User{
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Email = "nnguyenminhquang786@gmail.com",
                Password = "$2a$12$0KOTHwVrd95i9I9mycWJ7eYMBX.9XP2PP8H4xHWSgICzCOzW7k1Bu",
                Username = "bob",
                PhoneNumber = null,
                FullName = "Nguyen Minh Quang",
                Major = "SE",
                Faculty = "Engineering",
                YearOfStudy = 3,
                Bio = "Backend enthusiast",
                AvatarUrl = null,
                Role = Role.Admin,
                Point = 240,
                CreatedAt = T0,
                IsBanned = false,
                BannedUntil = null,
                BanReason = null
            },
            new User{
                Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                Email = "carol@demo.local",
                Password = "$2a$12$0KOTHwVrd95i9I9mycWJ7eYMBX.9XP2PP8H4xHWSgICzCOzW7k1Bu",
                Username = "carol",
                PhoneNumber = null,
                FullName = "Carol Pham",
                Major = "Math",
                Faculty = "Science",
                YearOfStudy = 1,
                Bio = "Linear algebra fan",
                AvatarUrl = null,
                Role = Role.Student,
                Point = 380,
                CreatedAt = T0,
                IsBanned = false,
                BannedUntil = null,
                BanReason = null
            },
            new User{
                Id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
                Email = "david@demo.local",
                Password = "$2a$12$0KOTHwVrd95i9I9mycWJ7eYMBX.9XP2PP8H4xHWSgICzCOzW7k1Bu",
                Username = "david",
                PhoneNumber = null,
                FullName = "David Le",
                Major = "IT",
                Faculty = "Engineering",
                YearOfStudy = 4,
                Bio = "DB & DevOps",
                AvatarUrl = null,
                Role = Role.Student,
                Point = 560,
                CreatedAt = T0,
                IsBanned = false,
                BannedUntil = null,
                BanReason = null
            },
            new User{
                Id = Guid.Parse("55555555-5555-5555-5555-555555555555"),
                Email = "eve@demo.local",
                Password = "$2a$12$0KOTHwVrd95i9I9mycWJ7eYMBX.9XP2PP8H4xHWSgICzCOzW7k1Bu",
                Username = "eve",
                PhoneNumber = null,
                FullName = "Eve Dang",
                Major = "SE",
                Faculty = "Engineering",
                YearOfStudy = 2,
                Bio = "Frontend hobbyist",
                AvatarUrl = null,
                Role = Role.Student,
                Point = 720,
                CreatedAt = T0,
                IsBanned = true,                                   // ví dụ đang bị ban
                BannedUntil = DateTime.UtcNow.AddDays(7),          // hết hạn ban sau 7 ngày
                BanReason = "Spam links"
            },
            new User{
                Id = Guid.Parse("66666666-6666-6666-6666-666666666666"),
                Email = "frank@demo.local",
                Password = "$2a$12$0KOTHwVrd95i9I9mycWJ7eYMBX.9XP2PP8H4xHWSgICzCOzW7k1Bu",
                Username = "frank",
                PhoneNumber = null,
                FullName = "Frank Vo",
                Major = "CS",
                Faculty = "Engineering",
                YearOfStudy = 3,
                Bio = "Systems learner",
                AvatarUrl = null,
                Role = Role.Student,
                Point = 80,
                CreatedAt = T0,
                IsBanned = false,
                BannedUntil = null,
                BanReason = null
            },
            new User{
                Id = Guid.Parse("77777777-7777-7777-7777-777777777777"),
                Email = "grace@demo.local",
                Password = "$2a$12$0KOTHwVrd95i9I9mycWJ7eYMBX.9XP2PP8H4xHWSgICzCOzW7k1Bu",
                Username = "grace",
                PhoneNumber = null,
                FullName = "Grace Ho",
                Major = "Math",
                Faculty = "Science",
                YearOfStudy = 1,
                Bio = "Graph theory",
                AvatarUrl = null,
                Role = Role.Student,
                Point = 910,
                CreatedAt = T0,
                IsBanned = false,
                BannedUntil = null,
                BanReason = null
            },
            new User{
                Id = Guid.Parse("88888888-8888-8888-8888-888888888888"),
                Email = "heidi@demo.local",
                Password = "$2a$12$0KOTHwVrd95i9I9mycWJ7eYMBX.9XP2PP8H4xHWSgICzCOzW7k1Bu",
                Username = "heidi",
                PhoneNumber = null,
                FullName = "Heidi Do",
                Major = "CS",
                Faculty = "Engineering",
                YearOfStudy = 2,
                Bio = "UI/UX",
                AvatarUrl = null,
                Role = Role.Student,
                Point = 40,
                CreatedAt = T0,
                IsBanned = false,
                BannedUntil = null,
                BanReason = null
            },
            new User{
                Id = Guid.Parse("99999999-9999-9999-9999-999999999999"),
                Email = "ivan@demo.local",
                Password = "$2a$12$0KOTHwVrd95i9I9mycWJ7eYMBX.9XP2PP8H4xHWSgICzCOzW7k1Bu",
                Username = "ivan",
                PhoneNumber = null,
                FullName = "Ivan Phan",
                Major = "CS",
                Faculty = "Engineering",
                YearOfStudy = 4,
                Bio = "Security curious",
                AvatarUrl = null,
                Role = Role.Student,
                Point = 150,
                CreatedAt = T0,
                IsBanned = false,
                BannedUntil = null,
                BanReason = null
            },
            new User{
                Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                Email = "judy@demo.local",
                Password = "$2a$12$0KOTHwVrd95i9I9mycWJ7eYMBX.9XP2PP8H4xHWSgICzCOzW7k1Bu",
                Username = "judy",
                PhoneNumber = null,
                FullName = "Judy Truong",
                Major = "SE",
                Faculty = "Engineering",
                YearOfStudy = 3,
                Bio = "Testing advocate",
                AvatarUrl = null,
                Role = Role.Student,
                Point = 305,
                CreatedAt = T0,
                IsBanned = false,
                BannedUntil = null,
                BanReason = null
            },
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
            return new()
            {
                new Category { Id = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccc01"), Name = "General" },
                new Category { Id = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccc02"), Name = "Q&A" },
                new Category { Id = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccc03"), Name = "Tutorials" },
                new Category { Id = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccc04"), Name = "Mathematics" },
                new Category { Id = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccc05"), Name = "Computer Science" },
                new Category { Id = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccc06"), Name = "Databases" },
                new Category { Id = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccc07"), Name = "Algorithms" },
                new Category { Id = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccc08"), Name = "Data Structures" },
                new Category { Id = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccc09"), Name = "DevOps" },
                new Category { Id = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccc10"), Name = "Web Development" }
            };
        }

        private static List<Post> BuildPosts()
        {
            var posts = new List<Post>();
            var userIds = new[]
            {
                Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Guid.Parse("33333333-3333-3333-3333-333333333333"),
                Guid.Parse("44444444-4444-4444-4444-444444444444"),
                Guid.Parse("55555555-5555-5555-5555-555555555555"),
                Guid.Parse("66666666-6666-6666-6666-666666666666"),
                Guid.Parse("77777777-7777-7777-7777-777777777777"),
                Guid.Parse("88888888-8888-8888-8888-888888888888"),
                Guid.Parse("99999999-9999-9999-9999-999999999999"),
                Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")
            };

            var titles = new[]
            {
                "Welcome to SmartPath", "Study algorithms?", "Linear Algebra tips", "DB normalization", "Git workflow",
                "Pointers in C", "Graph problems", "UI libraries", "JWT refresh", "Unit testing",
                "React vs Vue", "Docker containers", "Kubernetes basics", "AWS services", "TypeScript patterns",
                "Node.js best practices", "Python async/await", "Rust ownership", "Go concurrency", "Java Spring Boot",
                "Microservices architecture", "REST API design", "GraphQL basics", "WebSocket implementation", "Serverless functions",
                "CI/CD pipelines", "Infrastructure as Code", "Monitoring and logging", "Security best practices", "Performance optimization",
                "Database indexing", "Caching strategies", "Load balancing", "Rate limiting", "Authentication methods",
                "OAuth 2.0 flow", "JWT implementation", "Session management", "Cookie security", "CSRF protection",
                "SQL injection prevention", "XSS prevention", "Input validation", "Error handling", "Logging strategies",
                "Testing strategies", "TDD principles", "Mock objects", "Integration testing", "E2E testing",
                "Code review checklist", "Git commit messages", "Branching strategies", "Merge conflict resolution", "Code formatting",
                "Code documentation", "API documentation", "README templates", "Changelog management", "Version control",
                "Agile methodology", "Scrum practices", "Kanban boards", "User stories", "Sprint planning",
                "Technical debt", "Refactoring strategies", "Code smells", "Design patterns", "SOLID principles",
                "Clean code", "Code maintainability", "Software architecture", "System design", "Distributed systems",
                "Cloud computing", "Serverless architecture", "Event-driven architecture", "Microservices vs monolith", "API gateway",
                "Service mesh", "Circuit breaker pattern", "Retry mechanisms", "Dead letter queues", "Message brokers",
                "Apache Kafka", "RabbitMQ", "Redis pub/sub", "WebSockets", "Server-Sent Events",
                "Real-time applications", "Collaborative editing", "Conflict resolution", "Operational transformation", "CRDTs",
                "Blockchain basics", "Smart contracts", "DeFi protocols", "NFTs", "Web3 development",
                "Machine learning", "Deep learning", "Neural networks", "Computer vision", "Natural language processing",
                "Data preprocessing", "Feature engineering", "Model evaluation", "Hyperparameter tuning", "Model deployment",
                "MLOps practices", "Model monitoring", "A/B testing", "Experiment design", "Data visualization",
                "Dashboard design", "Business intelligence", "Data warehousing", "ETL pipelines", "Stream processing",
                "Apache Spark", "Apache Flink", "Real-time analytics", "Batch processing", "Data lakes",
                "Data governance", "Data privacy", "GDPR compliance", "Data security", "Data backup strategies",
                "High availability", "Disaster recovery", "Backup strategies", "Replication", "Sharding",
                "Database partitioning", "Vertical scaling", "Horizontal scaling", "Auto-scaling", "Performance tuning",
                "Memory management", "CPU optimization", "Network optimization", "Storage optimization", "Cloud cost optimization",
                "DevOps culture", "Site reliability engineering", "Incident response", "Post-mortem analysis", "Root cause analysis",
                "Debugging techniques", "Profiling tools", "Memory leaks", "CPU profiling", "Network debugging",
                "Mobile development", "iOS development", "Android development", "Cross-platform development", "Progressive Web Apps",
                "Flutter framework", "React Native", "Ionic framework", "Cordova plugins", "Native app development",
                "App store optimization", "Mobile UX design", "Responsive design", "Mobile-first approach", "Touch interactions",
                "Gesture recognition", "Voice interfaces", "Accessibility", "WCAG compliance", "Inclusive design"
            };

            var contents = new[]
            {
                "First post content", "Any recommended resources?", "Share your best tips", "3NF vs BCNF", "Git flow vs trunk-based",
                "How to avoid segfault?", "Minimum cut examples", "Shadcn vs Mantine?", "Best practices", "xUnit vs NUnit",
                "Which frontend framework do you prefer and why?", "How do you manage container orchestration?", "What are the benefits of container orchestration?", "Which cloud provider do you recommend?", "Share your favorite TypeScript patterns",
                "Best practices for Node.js development", "Understanding async/await in Python", "Rust ownership system explained", "Go channels and goroutines", "Spring Boot configuration tips",
                "When to use microservices", "REST API design principles", "GraphQL vs REST APIs", "Implementing real-time communication", "Serverless function examples",
                "CI/CD best practices", "Infrastructure as Code tools", "Monitoring solutions comparison", "Security checklist for web apps", "Performance optimization techniques",
                "Database indexing strategies", "When and how to use caching", "Load balancing algorithms", "Implementing rate limiting", "Authentication methods comparison",
                "OAuth 2.0 implementation guide", "JWT token security best practices", "Session management strategies", "Cookie security settings", "CSRF protection implementation",
                "Preventing SQL injection attacks", "XSS prevention techniques", "Input validation best practices", "Error handling patterns", "Logging strategies and tools",
                "Different testing approaches", "Test-driven development workflow", "When and how to use mocks", "Integration testing strategies", "End-to-end testing tools",
                "Code review best practices", "Writing better commit messages", "Git workflow comparison", "Resolving merge conflicts", "Code formatting tools",
                "Documentation best practices", "API documentation tools", "README template examples", "Changelog maintenance", "Version control strategies",
                "Agile principles in practice", "Scrum ceremony guidelines", "Kanban workflow optimization", "Writing effective user stories", "Sprint planning techniques",
                "Managing technical debt", "Refactoring code examples", "Common code smells", "Design pattern implementations", "Applying SOLID principles",
                "Writing clean, maintainable code", "Code quality metrics", "Software architecture patterns", "System design interview questions", "Distributed system challenges",
                "Cloud service comparison", "When to use serverless", "Event-driven architecture benefits", "Microservices pros and cons", "API gateway patterns",
                "Service mesh implementations", "Circuit breaker pattern explained", "Retry strategies and patterns", "Dead letter queue implementations", "Message broker comparison",
                "Kafka vs RabbitMQ", "Redis use cases", "Pub/sub patterns", "WebSocket implementation examples", "SSE vs WebSockets",
                "Building collaborative apps", "Real-time data synchronization", "Conflict resolution strategies", "Operational transformation explained", "Understanding CRDTs",
                "Blockchain fundamentals", "Smart contract development", "DeFi protocol examples", "NFT marketplace development", "Web3 development tools",
                "ML project workflow", "Deep learning framework comparison", "Neural network architectures", "Computer vision applications", "NLP techniques and tools",
                "Data cleaning techniques", "Feature selection methods", "Model evaluation metrics", "Hyperparameter optimization", "Model deployment strategies",
                "MLOps best practices", "Model monitoring in production", "A/B testing framework", "Experiment design principles", "Data visualization tools",
                "Dashboard design principles", "BI tool comparison", "Data warehouse design", "ETL pipeline examples", "Stream processing architectures",
                "Spark vs Flink comparison", "Real-time analytics platforms", "Batch processing optimization", "Data lake best practices", "Big data processing",
                "Data governance frameworks", "Privacy protection techniques", "GDPR implementation guide", "Data security best practices", "Backup and recovery strategies",
                "High availability patterns", "Disaster recovery planning", "Backup strategy design", "Database replication", "Sharding strategies",
                "Partitioning large tables", "Scaling up vs scaling out", "Auto-scaling implementations", "Performance tuning tips", "Cost optimization strategies",
                "Memory leak detection", "CPU profiling tools", "Network performance optimization", "Storage optimization", "Cloud cost management",
                "DevOps culture building", "SRE practices and principles", "Incident response procedures", "Post-mortem templates", "Root cause analysis methods",
                "Debugging techniques and tools", "Performance profiling", "Memory leak patterns", "CPU profiling examples", "Network debugging tools",
                "Mobile development platforms", "iOS development tips", "Android best practices", "Cross-platform tools comparison", "PWA development guide",
                "Flutter vs React Native", "Ionic framework tutorial", "Cordova plugin development", "Native app performance", "App deployment strategies",
                "ASO optimization tips", "Mobile UX guidelines", "Responsive design techniques", "Mobile development approach", "Touch interaction patterns",
                "Gesture recognition implementation", "Voice interface design", "Accessibility guidelines", "WCAG compliance checklist", "Inclusive design principles"
            };

            // Original 10 posts with AI review data
            posts.Add(new Post {
                Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1"),
                AuthorId = userIds[0],
                Title = titles[0],
                Content = contents[0],
                IsQuestion = false,
                CreatedAt = T0,
                UpdatedAt = T0,
                IsDeletedAt = null,
                Status = Status.Accepted,
                AiConfidence = 0.95,
                AiCategoryMatch = true,
                AiReason = "High quality welcome post with clear content",
                ReviewedAt = T0.AddMinutes(5)
            });
            posts.Add(new Post {
                Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa2"),
                AuthorId = userIds[1],
                Title = titles[1],
                Content = contents[1],
                IsQuestion = true,
                CreatedAt = T0,
                UpdatedAt = T0,
                IsDeletedAt = null,
                Status = Status.Accepted,
                AiConfidence = 0.88,
                AiCategoryMatch = true,
                AiReason = "Good question with specific request for resources",
                ReviewedAt = T0.AddMinutes(5)
            });
            posts.Add(new Post {
                Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa3"),
                AuthorId = userIds[2],
                Title = titles[2],
                Content = contents[2],
                IsQuestion = false,
                CreatedAt = T0,
                UpdatedAt = T0,
                IsDeletedAt = null,
                Status = Status.Accepted,
                AiConfidence = 0.92,
                AiCategoryMatch = true,
                AiReason = "Helpful tips shared on linear algebra",
                ReviewedAt = T0.AddMinutes(5)
            });
            posts.Add(new Post {
                Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa4"),
                AuthorId = userIds[3],
                Title = titles[3],
                Content = contents[3],
                IsQuestion = true,
                CreatedAt = T0,
                UpdatedAt = T0,
                IsDeletedAt = null,
                Status = Status.Accepted,
                AiConfidence = 0.85,
                AiCategoryMatch = true,
                AiReason = "Relevant database normalization question",
                ReviewedAt = T0.AddMinutes(5)
            });
            posts.Add(new Post {
                Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa5"),
                AuthorId = userIds[4],
                Title = titles[4],
                Content = contents[4],
                IsQuestion = false,
                CreatedAt = T0,
                UpdatedAt = T0,
                IsDeletedAt = null,
                Status = Status.Accepted,
                AiConfidence = 0.90,
                AiCategoryMatch = true,
                AiReason = "Well-explained Git workflow comparison",
                ReviewedAt = T0.AddMinutes(5)
            });
            posts.Add(new Post {
                Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa6"),
                AuthorId = userIds[5],
                Title = titles[5],
                Content = contents[5],
                IsQuestion = true,
                CreatedAt = T0,
                UpdatedAt = T0,
                IsDeletedAt = null,
                Status = Status.Accepted,
                AiConfidence = 0.87,
                AiCategoryMatch = true,
                AiReason = "Common programming question with good context",
                ReviewedAt = T0.AddMinutes(5)
            });
            posts.Add(new Post {
                Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa7"),
                AuthorId = userIds[6],
                Title = titles[6],
                Content = contents[6],
                IsQuestion = false,
                CreatedAt = T0,
                UpdatedAt = T0,
                IsDeletedAt = null,
                Status = Status.Accepted,
                AiConfidence = 0.93,
                AiCategoryMatch = true,
                AiReason = "Educational content with clear graph examples",
                ReviewedAt = T0.AddMinutes(5)
            });
            posts.Add(new Post {
                Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa8"),
                AuthorId = userIds[7],
                Title = titles[7],
                Content = contents[7],
                IsQuestion = true,
                CreatedAt = T0,
                UpdatedAt = T0,
                IsDeletedAt = null,
                Status = Status.Accepted,
                AiConfidence = 0.89,
                AiCategoryMatch = true,
                AiReason = "Good comparison question about UI frameworks",
                ReviewedAt = T0.AddMinutes(5)
            });
            posts.Add(new Post {
                Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa9"),
                AuthorId = userIds[8],
                Title = titles[8],
                Content = contents[8],
                IsQuestion = false,
                CreatedAt = T0,
                UpdatedAt = T0,
                IsDeletedAt = null,
                Status = Status.Accepted,
                AiConfidence = 0.91,
                AiCategoryMatch = true,
                AiReason = "Informative JWT refresh implementation guide",
                ReviewedAt = T0.AddMinutes(5)
            });
            posts.Add(new Post {
                Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa10"),
                AuthorId = userIds[9],
                Title = titles[9],
                Content = contents[9],
                IsQuestion = true,
                CreatedAt = T0,
                UpdatedAt = T0,
                IsDeletedAt = null,
                Status = Status.Accepted,
                AiConfidence = 0.86,
                AiCategoryMatch = true,
                AiReason = "Relevant question about unit testing practices",
                ReviewedAt = T0.AddMinutes(5)
            });

            // Add 90 more posts for pagination testing
            var random = new Random(42); // Fixed seed for reproducible results
            for (int i = 10; i < 100; i++)
            {
                var titleIndex = random.Next(0, titles.Length);
                var contentIndex = random.Next(0, contents.Length);
                var userIndex = random.Next(0, userIds.Length);
                var isQuestion = random.Next(0, 3) == 0; // 1/3 chance of being a question

                // Random AI review values
                var confidence = random.NextDouble() * 0.5 + 0.5; // 0.5 to 1.0
                var categoryMatch = random.NextDouble() > 0.2; // 80% chance of matching category
                var status = confidence >= 0.7 && categoryMatch ? Status.Accepted :
                           confidence >= 0.4 && categoryMatch ? Status.Pending : Status.Rejected;

                posts.Add(new Post
                {
                    Id = Guid.NewGuid(),
                    AuthorId = userIds[userIndex],
                    Title = titles[titleIndex] + $" #{i-9}",
                    Content = contents[contentIndex] + $" (Post {i-9})",
                    IsQuestion = isQuestion,
                    CreatedAt = T0.AddMinutes(random.Next(0, 10080)), // Random time within past week
                    UpdatedAt = T0.AddMinutes(random.Next(0, 10080)),
                    IsDeletedAt = null,
                    Status = status,
                    AiConfidence = confidence,
                    AiCategoryMatch = categoryMatch,
                    AiReason = status == Status.Accepted ? "Good quality content" :
                              status == Status.Pending ? "Needs manual review" : "Content requires improvement",
                    ReviewedAt = T0.AddMinutes(random.Next(1, 60))
                });
            }

            return posts;
        }

        private static List<CategoryPost> BuildCategoryPosts(List<Post> posts, List<Category> categories)
        {
            var categoryPosts = new List<CategoryPost>();
            var categoryIds = categories.Select(c => c.Id).ToList();
            var random = new Random(42); // Fixed seed for reproducible results

            // Original 10 category posts for the first 10 posts
            categoryPosts.Add(new CategoryPost { PostId = posts[0].Id, CategoryId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccc01") }); // General
            categoryPosts.Add(new CategoryPost { PostId = posts[1].Id, CategoryId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccc07") }); // Algorithms
            categoryPosts.Add(new CategoryPost { PostId = posts[2].Id, CategoryId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccc04") }); // Mathematics
            categoryPosts.Add(new CategoryPost { PostId = posts[3].Id, CategoryId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccc06") }); // Databases
            categoryPosts.Add(new CategoryPost { PostId = posts[4].Id, CategoryId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccc10") }); // Web Development
            categoryPosts.Add(new CategoryPost { PostId = posts[5].Id, CategoryId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccc05") }); // Computer Science
            categoryPosts.Add(new CategoryPost { PostId = posts[6].Id, CategoryId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccc08") }); // Data Structures
            categoryPosts.Add(new CategoryPost { PostId = posts[7].Id, CategoryId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccc01") }); // General
            categoryPosts.Add(new CategoryPost { PostId = posts[8].Id, CategoryId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccc05") }); // Computer Science
            categoryPosts.Add(new CategoryPost { PostId = posts[9].Id, CategoryId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccc02") }); // Q&A

            // Add category mappings for the remaining 90 posts
            for (int i = 10; i < posts.Count && i < 100; i++)
            {
                var numCategories = random.Next(1, 4); // 1-3 categories per post
                var selectedCategories = new HashSet<Guid>();

                for (int j = 0; j < numCategories; j++)
                {
                    var categoryIndex = random.Next(0, categoryIds.Count);
                    var categoryId = categoryIds[categoryIndex];

                    if (!selectedCategories.Contains(categoryId))
                    {
                        selectedCategories.Add(categoryId);
                        categoryPosts.Add(new CategoryPost { PostId = posts[i].Id, CategoryId = categoryId });
                    }
                }
            }

            return categoryPosts;
        }

        private static List<Comment> BuildComments()
        {
            var userIds = new[]
            {
                Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Guid.Parse("33333333-3333-3333-3333-333333333333"),
                Guid.Parse("44444444-4444-4444-4444-444444444444"),
                Guid.Parse("55555555-5555-5555-5555-555555555555"),
                Guid.Parse("66666666-6666-6666-6666-666666666666"),
                Guid.Parse("77777777-7777-7777-7777-777777777777"),
                Guid.Parse("88888888-8888-8888-8888-888888888888"),
                Guid.Parse("99999999-9999-9999-9999-999999999999"),
                Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")
            };

            var postIdsForComments = new[]
            {
                Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1"),
                Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa2"),
                Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa3"),
                Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa4"),
                Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa5"),
                Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa6"),
                Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa7"),
                Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa8"),
                Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa9"),
                Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa10"),
            };

            var commentContents = new[]
            {
                "Great to be here!", "CLRS + LeetCode patterns", "Khan Academy is good", "BCNF stricter than 3NF", "Thanks! Any YouTube?",
                "Prefer trunk-based", "Min cut via max flow", "Shadcn feels modern", "Rotate refresh tokens", "xUnit has nice asserts",
                "I disagree with this approach", "Have you considered TypeScript?", "This is exactly what I was looking for", "Thanks for sharing", "Could you elaborate more?",
                "What about performance implications?", "This worked for me", "I had the same issue", "Try using a different library", "Excellent explanation",
                "This is outdated information", "What about error handling?", "How do you test this?", "Great example", "I would add more validation",
                "What's the complexity?", "Can you share the code?", "This is not production ready", "Excellent point", "Have you benchmarked this?",
                "What about security?", "I prefer a different approach", "This is overengineered", "Simple and elegant", "What are the alternatives?",
                "This won't scale", "Consider using caching", "What about edge cases?", "Nice work", "I have a better solution",
                "This is exactly what I needed", "What about maintainability?", "Have you considered async?", "This is inefficient", "Great article!",
                "What's the learning curve?", "Can you provide more examples?", "This is too complex", "I love this approach", "What about backwards compatibility?",
                "Have you tested in production?", "This is exactly right", "What about monitoring?", "I would add logging", "What about error recovery?",
                "This is amazing", "Can you make it simpler?", "What about testing?", "This is brilliant", "What are the trade-offs?",
                "I implemented this and it works", "This is exactly what I was thinking", "What about documentation?", "This is perfect", "Can you write a follow-up?",
                "This saved me hours", "What about CI/CD?", "I would add unit tests", "This is the best answer", "What about deployment?",
                "Can you share the repository?", "This is comprehensive", "What about versioning?", "I learned a lot from this", "What about security best practices?",
                "This is exactly what we need", "Can you add more details?", "This is well explained", "I have a question about this", "What about scalability?",
                "This works perfectly", "I need more information", "This is very helpful", "What about performance testing?", "This is exactly right",
                "I tried this and failed", "What about debugging?", "This is too simple", "I need more examples", "What about production usage?",
                "This is exactly what I wanted", "Can you make it more robust?", "This is a good start", "I need help with implementation", "What about dependencies?",
                "This is life-changing", "Can you add error handling?", "This is missing something", "I have a better idea", "What about integration testing?"
            };

            var random = new Random(42);
            var allComments = new List<Comment>(capacity: 220);

            // ✅ Map: mỗi post có danh sách top-level comment ids riêng
            var topLevelByPost = new Dictionary<Guid, List<Guid>>();
            var createdAtById = new Dictionary<Guid, DateTime>(); // (optional) để đảm bảo reply không sớm hơn parent

            void AddTopLevel(Comment c)
            {
                allComments.Add(c);

                if (!topLevelByPost.TryGetValue(c.PostId, out var list))
                    topLevelByPost[c.PostId] = list = new List<Guid>();

                list.Add(c.Id);
                createdAtById[c.Id] = c.CreatedAt;
            }

            void AddAny(Comment c)
            {
                allComments.Add(c);
                createdAtById[c.Id] = c.CreatedAt;
            }

            // ===== Original comments (top-level) =====
            var c1 = new Comment { Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1"), AuthorId = userIds[1], PostId = postIdsForComments[0], Content = commentContents[0], CreatedAt = T0, ParentCommentId = null };
            var c2 = new Comment { Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb2"), AuthorId = userIds[2], PostId = postIdsForComments[1], Content = commentContents[1], CreatedAt = T0, ParentCommentId = null };
            var c3 = new Comment { Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb3"), AuthorId = userIds[3], PostId = postIdsForComments[2], Content = commentContents[2], CreatedAt = T0, ParentCommentId = null };
            var c4 = new Comment { Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb4"), AuthorId = userIds[4], PostId = postIdsForComments[3], Content = commentContents[3], CreatedAt = T0, ParentCommentId = null };
            var c5 = new Comment { Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb6"), AuthorId = userIds[5], PostId = postIdsForComments[4], Content = commentContents[5], CreatedAt = T0, ParentCommentId = null };
            var c6 = new Comment { Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb7"), AuthorId = userIds[6], PostId = postIdsForComments[5], Content = commentContents[6], CreatedAt = T0, ParentCommentId = null };
            var c7 = new Comment { Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb8"), AuthorId = userIds[7], PostId = postIdsForComments[6], Content = commentContents[7], CreatedAt = T0, ParentCommentId = null };
            var c8 = new Comment { Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb9"), AuthorId = userIds[8], PostId = postIdsForComments[7], Content = commentContents[8], CreatedAt = T0, ParentCommentId = null };
            var c9 = new Comment { Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbc10"), AuthorId = userIds[9], PostId = postIdsForComments[8], Content = commentContents[9], CreatedAt = T0, ParentCommentId = null };

            AddTopLevel(c1);
            AddTopLevel(c2);
            AddTopLevel(c3);
            AddTopLevel(c4);
            AddTopLevel(c5);
            AddTopLevel(c6);
            AddTopLevel(c7);
            AddTopLevel(c8);
            AddTopLevel(c9);

            // reply cho comment2 (cùng postIdsForComments[1]) ✅
            AddAny(new Comment
            {
                Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb5"),
                AuthorId = userIds[0],
                PostId = postIdsForComments[1],
                Content = commentContents[4],
                CreatedAt = T0.AddMinutes(5),
                ParentCommentId = c2.Id
            });

            // ===== Generate more comments =====
            while (allComments.Count < 200)
            {
                var userId = userIds[random.Next(userIds.Length)];
                var content = commentContents[random.Next(commentContents.Length)] + $" (Comment {allComments.Count})";

                var postId = postIdsForComments[random.Next(postIdsForComments.Length)];
                var isReply = random.Next(0, 3) > 0; // 2/3 chance reply

                Guid? parentId = null;
                DateTime createdAt = T0.AddMinutes(random.Next(0, 10080));

                if (isReply &&
                    topLevelByPost.TryGetValue(postId, out var tops) &&
                    tops.Count > 0)
                {
                    parentId = tops[random.Next(tops.Count)];

                    // (optional) đảm bảo reply không sớm hơn parent
                    if (createdAtById.TryGetValue(parentId.Value, out var parentAt) && createdAt <= parentAt)
                        createdAt = parentAt.AddMinutes(random.Next(1, 180)); // reply sau parent 1..180 phút
                }

                var newComment = new Comment
                {
                    Id = Guid.NewGuid(),
                    AuthorId = userId,
                    PostId = postId,
                    Content = content,
                    CreatedAt = createdAt,
                    ParentCommentId = parentId
                };

                AddAny(newComment);

                // nếu là top-level thì đưa vào đúng bucket của post ✅
                if (parentId == null)
                {
                    if (!topLevelByPost.TryGetValue(postId, out var list))
                        topLevelByPost[postId] = list = new List<Guid>();
                    list.Add(newComment.Id);
                }
            }

            return allComments;
        }

        private static List<Reaction> BuildReactions()
        {
            var reactions = new List<Reaction>();
            var userIds = new[]
            {
                Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Guid.Parse("33333333-3333-3333-3333-333333333333"),
                Guid.Parse("44444444-4444-4444-4444-444444444444"),
                Guid.Parse("55555555-5555-5555-5555-555555555555"),
                Guid.Parse("66666666-6666-6666-6666-666666666666"),
                Guid.Parse("77777777-7777-7777-7777-777777777777"),
                Guid.Parse("88888888-8888-8888-8888-888888888888"),
                Guid.Parse("99999999-9999-9999-9999-999999999999"),
                Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")
            };

            var random = new Random(42);
            var usedPostReactions = new HashSet<(Guid UserId, Guid PostId)>();
            var usedCommentReactions = new HashSet<(Guid UserId, Guid CommentId)>();

            // Original 10 reactions
            reactions.Add(new Reaction { Id = Guid.NewGuid(), UserId = Guid.Parse("33333333-3333-3333-3333-333333333333"), PostId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1"), CommentId = null, IsPositive = true });
            usedPostReactions.Add((Guid.Parse("33333333-3333-3333-3333-333333333333"), Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1")));

            reactions.Add(new Reaction { Id = Guid.NewGuid(), UserId = Guid.Parse("44444444-4444-4444-4444-444444444444"), PostId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1"), CommentId = null, IsPositive = true });
            usedPostReactions.Add((Guid.Parse("44444444-4444-4444-4444-444444444444"), Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1")));

            reactions.Add(new Reaction { Id = Guid.NewGuid(), UserId = Guid.Parse("55555555-5555-5555-5555-555555555555"), PostId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa2"), CommentId = null, IsPositive = true });
            usedPostReactions.Add((Guid.Parse("55555555-5555-5555-5555-555555555555"), Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa2")));

            reactions.Add(new Reaction { Id = Guid.NewGuid(), UserId = Guid.Parse("11111111-1111-1111-1111-111111111111"), PostId = null, CommentId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb2"), IsPositive = true });
            usedCommentReactions.Add((Guid.Parse("11111111-1111-1111-1111-111111111111"), Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb2")));

            reactions.Add(new Reaction { Id = Guid.NewGuid(), UserId = Guid.Parse("22222222-2222-2222-2222-222222222222"), PostId = null, CommentId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb5"), IsPositive = true });
            usedCommentReactions.Add((Guid.Parse("22222222-2222-2222-2222-222222222222"), Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb5")));

            reactions.Add(new Reaction { Id = Guid.NewGuid(), UserId = Guid.Parse("66666666-6666-6666-6666-666666666666"), PostId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa5"), CommentId = null, IsPositive = true });
            usedPostReactions.Add((Guid.Parse("66666666-6666-6666-6666-666666666666"), Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa5")));

            reactions.Add(new Reaction { Id = Guid.NewGuid(), UserId = Guid.Parse("77777777-7777-7777-7777-777777777777"), PostId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa7"), CommentId = null, IsPositive = true });
            usedPostReactions.Add((Guid.Parse("77777777-7777-7777-7777-777777777777"), Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa7")));

            reactions.Add(new Reaction { Id = Guid.NewGuid(), UserId = Guid.Parse("88888888-8888-8888-8888-888888888888"), PostId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa8"), CommentId = null, IsPositive = true });
            usedPostReactions.Add((Guid.Parse("88888888-8888-8888-8888-888888888888"), Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa8")));

            reactions.Add(new Reaction { Id = Guid.NewGuid(), UserId = Guid.Parse("99999999-9999-9999-9999-999999999999"), PostId = null, CommentId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb6"), IsPositive = true });
            usedCommentReactions.Add((Guid.Parse("99999999-9999-9999-9999-999999999999"), Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb6")));

            reactions.Add(new Reaction { Id = Guid.NewGuid(), UserId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), PostId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa10"), CommentId = null, IsPositive = true });
            usedPostReactions.Add((Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa10")));

            // Generate reactions for first 10 posts
            var postIds = new[]
            {
                Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1"),
                Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa2"),
                Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa3"),
                Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa4"),
                Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa5"),
                Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa6"),
                Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa7"),
                Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa8"),
                Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa9"),
                Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa10")
            };

            // Generate unique post reactions
            for (int i = 0; i < 10; i++)
            {
                var postId = postIds[i];
                var reactionCount = random.Next(3, 8); // 3-7 reactions per post to avoid conflicts

                for (int j = 0; j < reactionCount; j++)
                {
                    int attempts = 0;
                    Guid userId;
                    do
                    {
                        userId = userIds[random.Next(0, userIds.Length)];
                        attempts++;
                        if (attempts > 10) break; // Avoid infinite loop
                    } while (usedPostReactions.Contains((userId, postId)) && attempts < 10);

                    if (!usedPostReactions.Contains((userId, postId)))
                    {
                        var isPositive = random.Next(0, 4) > 0; // 75% positive reactions

                        reactions.Add(new Reaction
                        {
                            Id = Guid.NewGuid(),
                            UserId = userId,
                            PostId = postId,
                            CommentId = null,
                            IsPositive = isPositive
                        });

                        usedPostReactions.Add((userId, postId));
                    }
                }
            }

            // Generate reactions for comments
            var commentIds = new List<Guid>
            {
                Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1"),
                Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb3"),
                Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb4"),
                Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb7"),
                Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb8"),
                Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb9"),
                Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbc10")
            };

            // Generate unique comment reactions (skip those that already have reactions)
            for (int i = 0; i < commentIds.Count; i++)
            {
                var commentId = commentIds[i];
                var reactionCount = random.Next(1, 4); // 1-3 reactions per comment

                for (int j = 0; j < reactionCount; j++)
                {
                    int attempts = 0;
                    Guid userId;
                    do
                    {
                        userId = userIds[random.Next(0, userIds.Length)];
                        attempts++;
                        if (attempts > 10) break;
                    } while (usedCommentReactions.Contains((userId, commentId)) && attempts < 10);

                    if (!usedCommentReactions.Contains((userId, commentId)))
                    {
                        var isPositive = random.Next(0, 5) > 0; // 80% positive reactions for comments

                        reactions.Add(new Reaction
                        {
                            Id = Guid.NewGuid(),
                            UserId = userId,
                            PostId = null,
                            CommentId = commentId,
                            IsPositive = isPositive
                        });

                        usedCommentReactions.Add((userId, commentId));
                    }
                }
            }

            return reactions;
        }

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

        private static List<MaterialCategory> BuildMaterialCategories()
        {
            // Helper tạo node
            MaterialCategory Node(
                string name,
                string slug,
                Guid id,
                Guid? parentId,
                string path,
                int level,
                int sortOrder
            ) => new MaterialCategory
            {
                Id = id,
                Name = name,
                Slug = slug,
                ParentId = parentId,
                Path = path,
                Level = level,
                SortOrder = sortOrder,
                IsActive = true,
                CreatedAt = T0,
                UpdatedAt = T0
            };

            // Root
            var csId = Guid.Parse("c0000000-0000-0000-0000-000000000001");
            var mathId = Guid.Parse("c0000000-0000-0000-0000-000000000002");
            var dbId = Guid.Parse("c0000000-0000-0000-0000-000000000003");
            var webId = Guid.Parse("c0000000-0000-0000-0000-000000000004");
            var devopsId = Guid.Parse("c0000000-0000-0000-0000-000000000005");
            var aiId = Guid.Parse("c0000000-0000-0000-0000-000000000006");

            var list = new List<MaterialCategory>
            {
                Node("Computer Science", "cs", csId, null, "cs", 0, 1),
                Node("Mathematics", "math", mathId, null, "math", 0, 2),
                Node("Databases", "databases", dbId, null, "databases", 0, 3),
                Node("Web Development", "web", webId, null, "web", 0, 4),
                Node("DevOps", "devops", devopsId, null, "devops", 0, 5),
                Node("AI / Machine Learning", "ai", aiId, null, "ai", 0, 6),
            };

            // CS children
            var algoId = Guid.Parse("c0000000-0000-0000-0000-000000000101");
            var dsId = Guid.Parse("c0000000-0000-0000-0000-000000000102");
            var osId = Guid.Parse("c0000000-0000-0000-0000-000000000103");
            var netId = Guid.Parse("c0000000-0000-0000-0000-000000000104");

            list.AddRange(new[]
            {
                Node("Algorithms", "algorithms", algoId, csId, "cs/algorithms", 1, 1),
                Node("Data Structures", "data-structures", dsId, csId, "cs/data-structures", 1, 2),
                Node("Operating Systems", "operating-systems", osId, csId, "cs/operating-systems", 1, 3),
                Node("Computer Networks", "networks", netId, csId, "cs/networks", 1, 4),
            });

            // Algorithms sub
            list.AddRange(new[]
            {
                Node("Graph", "graph", Guid.Parse("c0000000-0000-0000-0000-000000000201"), algoId, "cs/algorithms/graph", 2, 1),
                Node("Dynamic Programming", "dynamic-programming", Guid.Parse("c0000000-0000-0000-0000-000000000202"), algoId, "cs/algorithms/dynamic-programming", 2, 2),
                Node("Greedy", "greedy", Guid.Parse("c0000000-0000-0000-0000-000000000203"), algoId, "cs/algorithms/greedy", 2, 3),
            });

            // Data Structures sub
            list.AddRange(new[]
            {
                Node("Array & String", "array-string", Guid.Parse("c0000000-0000-0000-0000-000000000211"), dsId, "cs/data-structures/array-string", 2, 1),
                Node("Linked List", "linked-list", Guid.Parse("c0000000-0000-0000-0000-000000000212"), dsId, "cs/data-structures/linked-list", 2, 2),
                Node("Tree", "tree", Guid.Parse("c0000000-0000-0000-0000-000000000213"), dsId, "cs/data-structures/tree", 2, 3),
                Node("Hash Table", "hash-table", Guid.Parse("c0000000-0000-0000-0000-000000000214"), dsId, "cs/data-structures/hash-table", 2, 4),
            });

            // Math children
            list.AddRange(new[]
            {
                Node("Linear Algebra", "linear-algebra", Guid.Parse("c0000000-0000-0000-0000-000000000301"), mathId, "math/linear-algebra", 1, 1),
                Node("Calculus", "calculus", Guid.Parse("c0000000-0000-0000-0000-000000000302"), mathId, "math/calculus", 1, 2),
                Node("Probability & Statistics", "probability-statistics", Guid.Parse("c0000000-0000-0000-0000-000000000303"), mathId, "math/probability-statistics", 1, 3),
            });

            // Databases children
            var sqlId = Guid.Parse("c0000000-0000-0000-0000-000000000401");
            var nosqlId = Guid.Parse("c0000000-0000-0000-0000-000000000402");
            list.AddRange(new[]
            {
                Node("SQL", "sql", sqlId, dbId, "databases/sql", 1, 1),
                Node("NoSQL", "nosql", nosqlId, dbId, "databases/nosql", 1, 2),
            });

            list.AddRange(new[]
            {
                Node("PostgreSQL", "postgresql", Guid.Parse("c0000000-0000-0000-0000-000000000411"), sqlId, "databases/sql/postgresql", 2, 1),
                Node("Indexing & Query Tuning", "indexing-query-tuning", Guid.Parse("c0000000-0000-0000-0000-000000000412"), sqlId, "databases/sql/indexing-query-tuning", 2, 2),
                Node("Redis", "redis", Guid.Parse("c0000000-0000-0000-0000-000000000421"), nosqlId, "databases/nosql/redis", 2, 1),
                Node("MongoDB", "mongodb", Guid.Parse("c0000000-0000-0000-0000-000000000422"), nosqlId, "databases/nosql/mongodb", 2, 2),
            });

            // Web children
            list.AddRange(new[]
            {
                Node("Frontend", "frontend", Guid.Parse("c0000000-0000-0000-0000-000000000501"), webId, "web/frontend", 1, 1),
                Node("Backend", "backend", Guid.Parse("c0000000-0000-0000-0000-000000000502"), webId, "web/backend", 1, 2),
                Node("Security", "security", Guid.Parse("c0000000-0000-0000-0000-000000000503"), webId, "web/security", 1, 3),
            });

            // DevOps children
            list.AddRange(new[]
            {
                Node("Docker", "docker", Guid.Parse("c0000000-0000-0000-0000-000000000601"), devopsId, "devops/docker", 1, 1),
                Node("Kubernetes", "kubernetes", Guid.Parse("c0000000-0000-0000-0000-000000000602"), devopsId, "devops/kubernetes", 1, 2),
                Node("CI/CD", "cicd", Guid.Parse("c0000000-0000-0000-0000-000000000603"), devopsId, "devops/cicd", 1, 3),
                Node("Observability", "observability", Guid.Parse("c0000000-0000-0000-0000-000000000604"), devopsId, "devops/observability", 1, 4),
            });

            // AI children
            list.AddRange(new[]
            {
                Node("Machine Learning Basics", "ml-basics", Guid.Parse("c0000000-0000-0000-0000-000000000701"), aiId, "ai/ml-basics", 1, 1),
                Node("NLP", "nlp", Guid.Parse("c0000000-0000-0000-0000-000000000702"), aiId, "ai/nlp", 1, 2),
                Node("LLM & RAG", "llm-rag", Guid.Parse("c0000000-0000-0000-0000-000000000703"), aiId, "ai/llm-rag", 1, 3),
            });

            return list;
        }

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

        private static async Task UpsertMaterialCategoriesAsync(SmartPathDbContext db, IEnumerable<MaterialCategory> categories, CancellationToken ct)
        {
            // Unique by Slug (đúng thiết kế: Slug unique)
            var existingSlugs = await db.MaterialCategories.AsNoTracking()
                .Select(x => x.Slug)
                .ToListAsync(ct);

            var existing = new HashSet<string>(existingSlugs);

            foreach (var c in categories)
            {
                if (!existing.Contains(c.Slug))
                    await db.MaterialCategories.AddAsync(c, ct);
            }
        }

        private static List<Chat> BuildChats()
        {
            var chats = new List<Chat>();
            var userIds = new[]
            {
                Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Guid.Parse("33333333-3333-3333-3333-333333333333"),
                Guid.Parse("44444444-4444-4444-4444-444444444444"),
                Guid.Parse("55555555-5555-5555-5555-555555555555"),
                Guid.Parse("66666666-6666-6666-6666-666666666666"),
                Guid.Parse("77777777-7777-7777-7777-777777777777"),
                Guid.Parse("88888888-8888-8888-8888-888888888888"),
                Guid.Parse("99999999-9999-9999-9999-999999999999"),
                Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")
            };

            // Create 20 chats between different user pairs
            var random = new Random(42);
            var chatPairs = new HashSet<(Guid, Guid)>();

            for (int i = 0; i < 20; i++)
            {
                Guid user1, user2;
                do
                {
                    user1 = userIds[random.Next(0, userIds.Length)];
                    user2 = userIds[random.Next(0, userIds.Length)];
                } while (user1 == user2 || chatPairs.Contains((user1, user2)) || chatPairs.Contains((user2, user1)));

                chatPairs.Add((user1, user2));

                // Normalize the pair (member1Id should be the smaller GUID)
                var normalizedPair = user1.CompareTo(user2) <= 0 ? (user1, user2) : (user2, user1);

                chats.Add(new Chat
                {
                    Id = Guid.NewGuid(),
                    Member1Id = normalizedPair.Item1,
                    Member2Id = normalizedPair.Item2,
                    CreatedAt = T0.AddHours(random.Next(0, 1680)) // Random time within past 70 days
                });
            }

            return chats;
        }

        private static List<Message> BuildMessages(List<Chat> chats)
        {
            var messages = new List<Message>();
            var random = new Random(42);
            var messageContents = new[]
            {
                "Hey, how are you doing?", "Did you see the latest post?", "Can you help me with this issue?", "Thanks for your help!", "Great work!",
                "Let's discuss this further", "I agree with your approach", "What do you think about this?", "Sure, let's connect tomorrow", "Looking forward to it",
                "Have you tried this solution?", "This is interesting", "Can we schedule a meeting?", "I'll send you the files", "Check your email",
                "What's your availability?", "Let me know when you're free", "Thanks for the quick response", "I appreciate your help", "No problem!",
                "How's the project going?", "I need some advice", "Can you review my code?", "Sure thing!", "I'll get back to you",
                "Let's catch up soon", "Great idea!", "I hadn't thought of that", "This makes sense", "Thanks for sharing",
                "What's the next step?", "I'm working on it", "Almost done", "Need more time?", "Take your time",
                "Good morning!", "How was your weekend?", "Any plans for today?", "Working from home today", "Same here",
                "Coffee break?", "Ready for the meeting?", "I'll join in 5 minutes", "Thanks for waiting", "No worries",
                "Did you see the email?", "I'll check and get back", "Let's sync up", "Sounds good", "Talk soon",
                "Happy Friday!", "Weekend plans?", "Have a great weekend!", "You too!", "See you Monday",
                "Quick question", "Sure, go ahead", "What's on your mind?", "I need some clarification", "Happy to help",
                "Learning anything new?", "Started a new course", "That's awesome!", "Highly recommend it", "Thanks for the tip",
                "How's the new project?", "Making progress", "That's great to hear", "Still figuring some things", "You'll get there"
            };

            // Generate 500+ messages across all chats
            foreach (var chat in chats)
            {
                var messageCount = random.Next(20, 50); // 20-49 messages per chat
                var lastMessageTime = chat.CreatedAt;

                for (int i = 0; i < messageCount; i++)
                {
                    var senderId = random.Next(0, 2) == 0 ? chat.Member1Id : chat.Member2Id;
                    var contentIndex = random.Next(0, messageContents.Length);
                    var timeOffset = random.Next(5, 300); // 5 minutes to 5 hours between messages

                    lastMessageTime = lastMessageTime.AddMinutes(timeOffset);

                    messages.Add(new Message
                    {
                        Id = Guid.NewGuid(),
                        ChatId = chat.Id,
                        SenderId = senderId,
                        Content = messageContents[contentIndex],
                        CreatedAt = lastMessageTime,
                        IsRead = random.Next(0, 3) > 0 // 2/3 chance of being read
                    });
                }
            }

            return messages.OrderBy(m => m.CreatedAt).ToList();
        }

        private static List<StudyMaterial> BuildStudyMaterials()
        {
            var materials = new List<StudyMaterial>();
            var userIds = new[]
            {
                Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Guid.Parse("33333333-3333-3333-3333-333333333333"),
                Guid.Parse("44444444-4444-4444-4444-444444444444"),
                Guid.Parse("55555555-5555-5555-5555-555555555555"),
                Guid.Parse("66666666-6666-6666-6666-666666666666"),
                Guid.Parse("77777777-7777-7777-7777-777777777777"),
                Guid.Parse("88888888-8888-8888-8888-888888888888"),
                Guid.Parse("99999999-9999-9999-9999-999999999999"),
                Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")
            };

            var titles = new[]
            {
                "Introduction to Algorithms", "Linear Algebra Fundamentals", "Web Development Bootcamp", "Database Design Patterns",
                "Machine Learning Basics", "React.js Complete Guide", "Python for Data Science", "System Design Interview",
                "Docker and Kubernetes", "AWS Cloud Architecture", "JavaScript ES6+", "Node.js Microservices",
                "CSS Grid and Flexbox", "TypeScript Handbook", "GraphQL Tutorial", "REST API Best Practices",
                "Git and GitHub Mastery", "DevOps Fundamentals", "CI/CD Pipelines", "Agile Methodology Guide",
                "Clean Code Principles", "Design Patterns in OOP", "Data Structures and Algorithms", "Network Security Basics",
                "Mobile App Development", "iOS Programming Guide", "Android Development Tutorial", "Flutter Framework",
                "Vue.js 3 Guide", "Angular Best Practices", "MongoDB Database Design", "PostgreSQL Performance",
                "Redis Cache Patterns", "Message Queue Systems", "Apache Kafka Guide", "RabbitMQ Tutorial",
                "Elasticsearch Guide", "Logstash and Kibana", "Monitoring and Alerting", "Performance Optimization",
                "Testing Strategies", "Unit Testing with Jest", "Integration Testing Guide", "E2E Testing with Cypress",
                "Cloud Native Applications", "Serverless Architecture", "Microservices Patterns", "API Gateway Design",
                "Event-Driven Architecture", "CQRS Pattern", "Domain-Driven Design", "Software Architecture"
            };

            var descriptions = new[]
            {
                "Comprehensive guide covering all fundamental concepts with practical examples",
                "Step-by-step tutorial with real-world projects and exercises",
                "In-depth exploration with code samples and best practices",
                "Complete reference guide with detailed explanations and examples",
                "Practical approach with hands-on labs and assignments",
                "Advanced techniques and patterns for professional development",
                "Beginner-friendly introduction with clear explanations",
                "Expert-level guide with cutting-edge techniques and methodologies",
                "Industry-standard practices and real-world applications",
                "Comprehensive course with video tutorials and downloadable resources"
            };

            var random = new Random(42);

            // Get all material category IDs
            var categoryIds = new[]
            {
                Guid.Parse("c0000000-0000-0000-0000-000000000101"), // Algorithms
                Guid.Parse("c0000000-0000-0000-0000-000000000301"), // Linear Algebra
                Guid.Parse("c0000000-0000-0000-0000-000000000501"), // Frontend
                Guid.Parse("c0000000-0000-0000-0000-000000000411"), // PostgreSQL
                Guid.Parse("c0000000-0000-0000-0000-000000000701"), // ML Basics
            };

            // Generate 50+ study materials
            for (int i = 0; i < 55; i++)
            {
                var titleIndex = random.Next(0, titles.Length);
                var descriptionIndex = random.Next(0, descriptions.Length);
                var uploaderIndex = random.Next(0, userIds.Length);
                var categoryIndex = random.Next(0, categoryIds.Length);
                var status = random.Next(0, 4) switch
                {
                    0 => Status.Accepted,
                    1 => Status.Pending,
                    2 => Status.Accepted,
                    _ => Status.Accepted // 75% accepted
                };

                materials.Add(new StudyMaterial
                {
                    Id = Guid.NewGuid(),
                    UploaderId = userIds[uploaderIndex],
                    CategoryId = categoryIds[categoryIndex],
                    Title = titles[titleIndex],
                    Description = descriptions[descriptionIndex],
                    SourceType = StudyMaterialSourceType.File,
                    Status = status,
                    CreatedAt = T0.AddDays(random.Next(-30, 30)),
                    FileUrl = $"https://example.com/materials/{Guid.NewGuid()}.pdf",
                    MimeType = "application/pdf",
                    FileSize = random.Next(1024 * 1024, 50 * 1024 * 1024), // 1MB to 50MB
                    AiConfidence = random.NextDouble(),
                    AiReason = "AI-generated content analysis",
                    AiCategoryMatch = random.NextDouble() > 0.3
                });
            }

            return materials;
        }

        private static async Task UpsertStudyMaterialsAsync(SmartPathDbContext db, IEnumerable<StudyMaterial> materials, CancellationToken ct)
        {
            var existing = await db.StudyMaterials.AsNoTracking()
                .Select(m => new { m.Id, m.Title })
                .ToListAsync(ct);

            var existingTitles = new HashSet<string>(existing.Select(e => e.Title));

            foreach (var material in materials)
            {
                if (!existingTitles.Contains(material.Title))
                {
                    await db.StudyMaterials.AddAsync(material, ct);
                }
            }
        }
    }
}
