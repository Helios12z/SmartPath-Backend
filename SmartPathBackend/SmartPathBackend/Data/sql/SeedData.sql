-- ============================================================
-- SeedData.sql (idempotent)
-- ============================================================

-- 0) Extensions
CREATE EXTENSION IF NOT EXISTS "pgcrypto";

-- ============================================================
-- 1) SCHEMA
-- ============================================================

-- USERS
CREATE TABLE IF NOT EXISTS "Users" (
  "Id" UUID PRIMARY KEY,
  "Email" VARCHAR(255) UNIQUE NOT NULL,
  "Password" VARCHAR(255),
  "Username" VARCHAR(50) UNIQUE NOT NULL,
  "PhoneNumber" VARCHAR(50),
  "FullName" VARCHAR(100),
  "Major" VARCHAR(100),
  "Faculty" TEXT,
  "YearOfStudy" INT,
  "Bio" TEXT,
  "AvatarUrl" TEXT,
  "Role" VARCHAR(20) NOT NULL,
  "Point" INT,
  "CreatedAt" TIMESTAMP
);

-- BADGES
CREATE TABLE IF NOT EXISTS "Badges" (
  "Id" UUID PRIMARY KEY,
  "Point" INT NOT NULL,
  "Name" VARCHAR(50) NOT NULL,
  CONSTRAINT uq_badges_name UNIQUE ("Name"),
  CONSTRAINT uq_badges_point UNIQUE ("Point")
);

-- CATEGORIES
CREATE TABLE IF NOT EXISTS "Categories" (
  "Id" UUID PRIMARY KEY,
  "Name" VARCHAR(50) NOT NULL UNIQUE
);

-- POSTS
CREATE TABLE IF NOT EXISTS "Posts" (
  "Id" UUID PRIMARY KEY,
  "AuthorId" UUID NOT NULL REFERENCES "Users"("Id") ON DELETE CASCADE,
  "Title" VARCHAR(255),
  "Content" TEXT,
  "IsQuestion" BOOLEAN,
  "CreatedAt" TIMESTAMP,
  "UpdatedAt" TIMESTAMP,
  "IsDeletedAt" TIMESTAMP
);

-- CATEGORY_POST (junction)
CREATE TABLE IF NOT EXISTS "CategoryPost" (
  "PostId" UUID NOT NULL REFERENCES "Posts"("Id") ON DELETE CASCADE,
  "CategoryId" UUID NOT NULL REFERENCES "Categories"("Id") ON DELETE CASCADE,
  CONSTRAINT pk_category_post PRIMARY KEY ("PostId","CategoryId")
);

-- COMMENTS
CREATE TABLE IF NOT EXISTS "Comments" (
  "Id" UUID PRIMARY KEY,
  "AuthorId" UUID NOT NULL REFERENCES "Users"("Id") ON DELETE CASCADE,
  "PostId" UUID NOT NULL REFERENCES "Posts"("Id") ON DELETE CASCADE,
  "Content" TEXT,
  "CreatedAt" TIMESTAMP,
  "ParentCommentId" UUID NULL REFERENCES "Comments"("Id") ON DELETE SET NULL
);

-- REACTIONS
CREATE TABLE IF NOT EXISTS "Reactions" (
  "Id" UUID PRIMARY KEY,
  "UserId" UUID NOT NULL REFERENCES "Users"("Id") ON DELETE CASCADE,
  "PostId" UUID NULL REFERENCES "Posts"("Id") ON DELETE CASCADE,
  "CommentId" UUID NULL REFERENCES "Comments"("Id") ON DELETE CASCADE,
  "IsPositive" BOOLEAN NOT NULL,
  CONSTRAINT ck_reactions_one_target CHECK (
    ("PostId" IS NOT NULL AND "CommentId" IS NULL)
    OR ("PostId" IS NULL AND "CommentId" IS NOT NULL)
  )
);
-- Unique partial indexes
CREATE UNIQUE INDEX IF NOT EXISTS uq_reactions_user_post
  ON "Reactions"("UserId","PostId") WHERE "CommentId" IS NULL;
CREATE UNIQUE INDEX IF NOT EXISTS uq_reactions_user_comment
  ON "Reactions"("UserId","CommentId") WHERE "PostId" IS NULL;

-- FRIENDSHIPS
CREATE TABLE IF NOT EXISTS "Friendships" (
  "Id" UUID PRIMARY KEY,
  "FollowerId" UUID NOT NULL REFERENCES "Users"("Id") ON DELETE CASCADE,
  "FollowedUserId" UUID NOT NULL REFERENCES "Users"("Id") ON DELETE CASCADE,
  "CreatedAt" TIMESTAMP NOT NULL,
  CONSTRAINT uq_friendship UNIQUE ("FollowerId","FollowedUserId")
);

-- CHATS
CREATE TABLE IF NOT EXISTS "Chats" (
  "Id" UUID PRIMARY KEY
);

-- MESSAGES
CREATE TABLE IF NOT EXISTS "Messages" (
  "Id" UUID PRIMARY KEY,
  "ChatId" UUID NOT NULL REFERENCES "Chats"("Id") ON DELETE CASCADE,
  "SenderId" UUID NOT NULL REFERENCES "Users"("Id") ON DELETE CASCADE,
  "Content" TEXT,
  "CreatedAt" TIMESTAMP
);

-- MATERIALS
CREATE TABLE IF NOT EXISTS "Materials" (
  "Id" UUID PRIMARY KEY,
  "Url" TEXT,
  "Title" TEXT,
  "CreatedAt" TIMESTAMP
);

-- NOTIFICATIONS
CREATE TABLE IF NOT EXISTS "Notifications" (
  "Id" UUID PRIMARY KEY,
  "ReceiverId" UUID NOT NULL REFERENCES "Users"("Id") ON DELETE CASCADE,
  "Type" TEXT,
  "Content" TEXT,
  "Url" TEXT,
  "CreatedAt" TIMESTAMP,
  "IsRead" BOOLEAN NOT NULL DEFAULT FALSE
);

-- REPORTS
CREATE TABLE IF NOT EXISTS "Reports" (
  "Id" UUID PRIMARY KEY,
  "ReporterId" UUID NOT NULL REFERENCES "Users"("Id") ON DELETE CASCADE,
  "Reason" TEXT,
  "Details" TEXT,
  "CreatedAt" TIMESTAMP
);

-- SYSTEMLOGS
CREATE TABLE IF NOT EXISTS "SystemLogs" (
  "Id" UUID PRIMARY KEY,
  "Level" TEXT,
  "Message" TEXT,
  "CreatedAt" TIMESTAMP
);

-- BOTCONVERSATIONS
CREATE TABLE IF NOT EXISTS "BotConversations" (
  "Id" UUID PRIMARY KEY,
  "OwnerId" UUID NOT NULL REFERENCES "Users"("Id") ON DELETE CASCADE,
  "Title" TEXT,
  "CreatedAt" TIMESTAMP,
  "UpdatedAt" TIMESTAMP
);

-- BOTMESSAGES
CREATE TABLE IF NOT EXISTS "BotMessages" (
  "Id" UUID PRIMARY KEY,
  "ConversationId" UUID NOT NULL REFERENCES "BotConversations"("Id") ON DELETE CASCADE,
  "Role" INT NOT NULL,
  "Content" TEXT,
  "SenderId" UUID NULL REFERENCES "Users"("Id") ON DELETE SET NULL,
  "CreatedAt" TIMESTAMP
);

-- REPUTATIONCHECKPOINTS
CREATE TABLE IF NOT EXISTS "ReputationCheckpoints" (
  "Id" UUID PRIMARY KEY,
  "ContentType" TEXT NOT NULL,
  "ContentId" UUID NOT NULL,
  "LikeBandsApplied" INT NOT NULL DEFAULT 0,
  "DislikeBandsApplied" INT NOT NULL DEFAULT 0,
  CONSTRAINT uq_rep_ck UNIQUE ("ContentType","ContentId")
);

-- ============================================================
-- 2) DATA
-- ============================================================

-- USERS (10)
INSERT INTO "Users" ("Id","Email","Password","Username","PhoneNumber","FullName","Major","Faculty","YearOfStudy","Bio","AvatarUrl","Role","Point","CreatedAt")
VALUES
('11111111-1111-1111-1111-111111111111','alice@demo.local',NULL,'alice',NULL,'Alice Nguyen','CS','Engineering',2,'Loves algorithms',NULL,'student',120, NOW()),
('22222222-2222-2222-2222-222222222222','bob@demo.local',NULL,'bob',NULL,'Bob Tran','CS','Engineering',3,'Backend enthusiast',NULL,'student',240, NOW()),
('33333333-3333-3333-3333-333333333333','carol@demo.local',NULL,'carol',NULL,'Carol Pham','Math','Science',1,'Linear algebra fan',NULL,'student',380, NOW()),
('44444444-4444-4444-4444-444444444444','david@demo.local',NULL,'david',NULL,'David Le','IT','Engineering',4,'DB & DevOps',NULL,'student',560, NOW()),
('55555555-5555-5555-5555-555555555555','eve@demo.local',NULL,'eve',NULL,'Eve Dang','SE','Engineering',2,'Frontend hobbyist',NULL,'student',720, NOW()),
('66666666-6666-6666-6666-666666666666','frank@demo.local',NULL,'frank',NULL,'Frank Vo','CS','Engineering',3,'Systems learner',NULL,'student',80, NOW()),
('77777777-7777-7777-7777-777777777777','grace@demo.local',NULL,'grace',NULL,'Grace Ho','Math','Science',1,'Graph theory',NULL,'student',910, NOW()),
('88888888-8888-8888-8888-888888888888','heidi@demo.local',NULL,'heidi',NULL,'Heidi Do','CS','Engineering',2,'UI/UX',NULL,'student',40, NOW()),
('99999999-9999-9999-9999-999999999999','ivan@demo.local',NULL,'ivan',NULL,'Ivan Phan','CS','Engineering',4,'Security curious',NULL,'student',150, NOW()),
('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa','judy@demo.local',NULL,'judy',NULL,'Judy Truong','SE','Engineering',3,'Testing advocate',NULL,'student',305, NOW())
ON CONFLICT ("Id") DO NOTHING;

-- BADGES (10)
INSERT INTO "Badges" ("Id","Point","Name") VALUES
(gen_random_uuid(),   0, 'Intern'),
(gen_random_uuid(), 100, 'Wolf Coder'),
(gen_random_uuid(), 250, 'Fresher'),
(gen_random_uuid(), 350, 'Demonic Coder'),
(gen_random_uuid(), 500, 'Junior Dev'),
(gen_random_uuid(), 650, 'Dragon Coder'),
(gen_random_uuid(), 800, 'Lightning Dev'),
(gen_random_uuid(), 900, 'Super Senior'),
(gen_random_uuid(), 950, 'Code God'),
(gen_random_uuid(),1000, 'Champion')
ON CONFLICT ("Name") DO NOTHING;

-- CATEGORIES (10)
INSERT INTO "Categories" ("Id","Name") VALUES
(gen_random_uuid(),'General'),
(gen_random_uuid(),'Q&A'),
(gen_random_uuid(),'Tutorials'),
(gen_random_uuid(),'Mathematics'),
(gen_random_uuid(),'Computer Science'),
(gen_random_uuid(),'Databases'),
(gen_random_uuid(),'Algorithms'),
(gen_random_uuid(),'Data Structures'),
(gen_random_uuid(),'DevOps'),
(gen_random_uuid(),'Web Development')
ON CONFLICT ("Name") DO NOTHING;

-- POSTS (10)
INSERT INTO "Posts" ("Id","AuthorId","Title","Content","IsQuestion","CreatedAt","UpdatedAt","IsDeletedAt") VALUES
('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1','11111111-1111-1111-1111-111111111111','Welcome to SmartPath','First post content',FALSE, NOW(), NOW(), NULL),
('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa2','22222222-2222-2222-2222-222222222222','Study algorithms?','Any recommended resources?',TRUE, NOW(), NOW(), NULL),
('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa3','33333333-3333-3333-3333-333333333333','Linear Algebra tips','Share your best tips',FALSE, NOW(), NOW(), NULL),
('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa4','44444444-4444-4444-4444-444444444444','DB normalization','3NF vs BCNF',TRUE, NOW(), NOW(), NULL),
('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa5','55555555-5555-5555-5555-555555555555','Git workflow','Git flow vs trunk-based',FALSE, NOW(), NOW(), NULL),
('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa6','66666666-6666-6666-6666-666666666666','Pointers in C','How to avoid segfault?',TRUE, NOW(), NOW(), NULL),
('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa7','77777777-7777-7777-7777-777777777777','Graph problems','Minimum cut examples',FALSE, NOW(), NOW(), NULL),
('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa8','88888888-8888-8888-8888-888888888888','UI libraries','Shadcn vs Mantine?',TRUE, NOW(), NOW(), NULL),
('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa9','99999999-9999-9999-9999-999999999999','JWT refresh','Best practices',FALSE, NOW(), NOW(), NULL),
('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa10','aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa','Unit testing','xUnit vs NUnit',TRUE, NOW(), NOW(), NULL)
ON CONFLICT ("Id") DO NOTHING;

-- CATEGORY_POST (10)
WITH cat AS (
  SELECT "Id","Name" FROM "Categories" ORDER BY "Name" LIMIT 10
)
INSERT INTO "CategoryPost" ("PostId","CategoryId") VALUES
('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1', (SELECT "Id" FROM cat WHERE "Name"='General')),
('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa2', (SELECT "Id" FROM cat WHERE "Name"='Algorithms')),
('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa3', (SELECT "Id" FROM cat WHERE "Name"='Mathematics')),
('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa4', (SELECT "Id" FROM cat WHERE "Name"='Databases')),
('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa5', (SELECT "Id" FROM cat WHERE "Name"='Web Development')),
('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa6', (SELECT "Id" FROM cat WHERE "Name"='Computer Science')),
('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa7', (SELECT "Id" FROM cat WHERE "Name"='Data Structures')),
('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa8', (SELECT "Id" FROM cat WHERE "Name"='General')),
('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa9', (SELECT "Id" FROM cat WHERE "Name"='Computer Science')),
('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa10', (SELECT "Id" FROM cat WHERE "Name"='Q&A'))
ON CONFLICT ("PostId","CategoryId") DO NOTHING;

-- COMMENTS (10)
INSERT INTO "Comments" ("Id","AuthorId","PostId","Content","CreatedAt","ParentCommentId") VALUES
('bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1','22222222-2222-2222-2222-222222222222','aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1','Great to be here!', NOW(), NULL),
('bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb2','33333333-3333-3333-3333-333333333333','aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa2','CLRS + LeetCode patterns', NOW(), NULL),
('bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb3','44444444-4444-4444-4444-444444444444','aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa3','Khan Academy is good', NOW(), NULL),
('bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb4','55555555-5555-5555-5555-555555555555','aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa4','BCNF stricter than 3NF', NOW(), NULL),
('bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb5','11111111-1111-1111-1111-111111111111','aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa2','Thanks! Any YouTube?', NOW(),'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb2'),
('bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb6','66666666-6666-6666-6666-666666666666','aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa5','Prefer trunk-based', NOW(), NULL),
('bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb7','77777777-7777-7777-7777-777777777777','aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa7','Min cut via max flow', NOW(), NULL),
('bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb8','88888888-8888-8888-8888-888888888888','aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa8','Shadcn feels modern', NOW(), NULL),
('bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb9','99999999-9999-9999-9999-999999999999','aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa9','Rotate refresh tokens', NOW(), NULL),
('bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbc10','aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa','aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa10','xUnit has nice asserts', NOW(), NULL)
ON CONFLICT ("Id") DO NOTHING;

-- REACTIONS (10)
INSERT INTO "Reactions" ("Id","UserId","PostId","CommentId","IsPositive") VALUES
(gen_random_uuid(),'33333333-3333-3333-3333-333333333333','aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1',NULL, TRUE),
(gen_random_uuid(),'44444444-4444-4444-4444-444444444444','aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1',NULL, TRUE),
(gen_random_uuid(),'55555555-5555-5555-5555-555555555555','aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa2',NULL, TRUE),
(gen_random_uuid(),'11111111-1111-1111-1111-111111111111',NULL,'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb2', TRUE),
(gen_random_uuid(),'22222222-2222-2222-2222-222222222222',NULL,'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb5', TRUE),
(gen_random_uuid(),'66666666-6666-6666-6666-666666666666','aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa5',NULL, TRUE),
(gen_random_uuid(),'77777777-7777-7777-7777-777777777777','aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa7',NULL, TRUE),
(gen_random_uuid(),'88888888-8888-8888-8888-888888888888','aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa8',NULL, TRUE),
(gen_random_uuid(),'99999999-9999-9999-9999-999999999999',NULL,'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb6', TRUE),
(gen_random_uuid(),'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa','aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaa10',NULL, TRUE)
ON CONFLICT ("Id") DO NOTHING;

-- FRIENDSHIPS (10)
INSERT INTO "Friendships" ("Id","FollowerId","FollowedUserId","CreatedAt") VALUES
(gen_random_uuid(),'11111111-1111-1111-1111-111111111111','22222222-2222-2222-2222-222222222222', NOW()),
(gen_random_uuid(),'11111111-1111-1111-1111-111111111111','33333333-3333-3333-3333-333333333333', NOW()),
(gen_random_uuid(),'22222222-2222-2222-2222-222222222222','33333333-3333-3333-3333-333333333333', NOW()),
(gen_random_uuid(),'33333333-3333-3333-3333-333333333333','44444444-4444-4444-4444-444444444444', NOW()),
(gen_random_uuid(),'44444444-4444-4444-4444-444444444444','55555555-5555-5555-5555-555555555555', NOW()),
(gen_random_uuid(),'55555555-5555-5555-5555-555555555555','66666666-6666-6666-6666-666666666666', NOW()),
(gen_random_uuid(),'66666666-6666-6666-6666-666666666666','77777777-7777-7777-7777-777777777777', NOW()),
(gen_random_uuid(),'77777777-7777-7777-7777-777777777777','88888888-8888-8888-8888-888888888888', NOW()),
(gen_random_uuid(),'88888888-8888-8888-8888-888888888888','99999999-9999-9999-9999-999999999999', NOW()),
(gen_random_uuid(),'99999999-9999-9999-9999-999999999999','aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', NOW())
ON CONFLICT ("Id") DO NOTHING;

-- CHATS (10)
INSERT INTO "Chats" ("Id") VALUES
('c0c0c0c0-0000-0000-0000-000000000001'),
('c0c0c0c0-0000-0000-0000-000000000002'),
('c0c0c0c0-0000-0000-0000-000000000003'),
('c0c0c0c0-0000-0000-0000-000000000004'),
('c0c0c0c0-0000-0000-0000-000000000005'),
('c0c0c0c0-0000-0000-0000-000000000006'),
('c0c0c0c0-0000-0000-0000-000000000007'),
('c0c0c0c0-0000-0000-0000-000000000008'),
('c0c0c0c0-0000-0000-0000-000000000009'),
('c0c0c0c0-0000-0000-0000-000000000010')
ON CONFLICT ("Id") DO NOTHING;

-- MESSAGES (10)
INSERT INTO "Messages" ("Id","ChatId","SenderId","Content","CreatedAt") VALUES
(gen_random_uuid(),'c0c0c0c0-0000-0000-0000-000000000001','11111111-1111-1111-1111-111111111111','Hi Bob!', NOW()),
(gen_random_uuid(),'c0c0c0c0-0000-0000-0000-000000000001','22222222-2222-2222-2222-222222222222','Hi Alice!', NOW()),
(gen_random_uuid(),'c0c0c0c0-0000-0000-0000-000000000002','33333333-3333-3333-3333-333333333333','Anyone up for study group?', NOW()),
(gen_random_uuid(),'c0c0c0c0-0000-0000-0000-000000000003','44444444-4444-4444-4444-444444444444','DB tips welcome', NOW()),
(gen_random_uuid(),'c0c0c0c0-0000-0000-0000-000000000004','55555555-5555-5555-5555-555555555555','Trunk-based works well', NOW()),
(gen_random_uuid(),'c0c0c0c0-0000-0000-0000-000000000005','66666666-6666-6666-6666-666666666666','C pointers are tricky', NOW()),
(gen_random_uuid(),'c0c0c0c0-0000-0000-0000-000000000006','77777777-7777-7777-7777-777777777777','Max flow solved it', NOW()),
(gen_random_uuid(),'c0c0c0c0-0000-0000-0000-000000000007','88888888-8888-8888-8888-888888888888','Shadcn is neat', NOW()),
(gen_random_uuid(),'c0c0c0c0-0000-0000-0000-000000000008','99999999-9999-9999-9999-999999999999','JWT rotation is a must', NOW()),
(gen_random_uuid(),'c0c0c0c0-0000-0000-0000-000000000009','aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa','NUnit vs xUnit thoughts', NOW())
ON CONFLICT ("Id") DO NOTHING;

-- MATERIALS (10)
INSERT INTO "Materials" ("Id","Url","Title","CreatedAt") VALUES
(gen_random_uuid(),'https://example.com/files/algorithms.pdf','Algorithms PDF', NOW()),
(gen_random_uuid(),'https://example.com/files/linear-algebra.pdf','Linear Algebra PDF', NOW()),
(gen_random_uuid(),'https://example.com/files/db-normalization.pdf','DB Normalization', NOW()),
(gen_random_uuid(),'https://example.com/files/git-workflow.pdf','Git Workflow', NOW()),
(gen_random_uuid(),'https://example.com/files/pointers-c.pdf','Pointers in C', NOW()),
(gen_random_uuid(),'https://example.com/files/graph-theory.pdf','Graph Theory', NOW()),
(gen_random_uuid(),'https://example.com/files/ui-libraries.pdf','UI Libraries', NOW()),
(gen_random_uuid(),'https://example.com/files/jwt-refresh.pdf','JWT Refresh', NOW()),
(gen_random_uuid(),'https://example.com/files/unit-testing.pdf','Unit Testing', NOW()),
(gen_random_uuid(),'https://example.com/files/data-structures.pdf','Data Structures', NOW())
ON CONFLICT ("Id") DO NOTHING;

-- NOTIFICATIONS (10)
INSERT INTO "Notifications" ("Id","ReceiverId","Type","Content","Url","CreatedAt","IsRead") VALUES
(gen_random_uuid(),'22222222-2222-2222-2222-222222222222','comment.reply','Bình luận của bạn vừa có phản hồi.','/posts/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa2', NOW(), FALSE),
(gen_random_uuid(),'33333333-3333-3333-3333-333333333333','friend.request','Bạn có yêu cầu kết bạn mới.','/friends?tab=requests', NOW(), FALSE),
(gen_random_uuid(),'44444444-4444-4444-4444-444444444444','post.like','Bài viết của bạn vừa được thích.','/posts/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa4', NOW(), FALSE),
(gen_random_uuid(),'55555555-5555-5555-5555-555555555555','badge.earned','Bạn vừa đạt huy hiệu Silver.','/achievements', NOW(), FALSE),
(gen_random_uuid(),'11111111-1111-1111-1111-111111111111','system.info','Chào mừng đến SmartPath!','/dashboard', NOW(), TRUE),
(gen_random_uuid(),'66666666-6666-6666-6666-666666666666','comment.reply','Có phản hồi mới cho bình luận của bạn.','/posts/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa5', NOW(), FALSE),
(gen_random_uuid(),'77777777-7777-7777-7777-777777777777','friend.accepted','Yêu cầu kết bạn đã được chấp nhận.','/friends', NOW(), TRUE),
(gen_random_uuid(),'88888888-8888-8888-8888-888888888888','post.pin','Bài viết của bạn đã được ghim.','/posts/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa8', NOW(), FALSE),
(gen_random_uuid(),'99999999-9999-9999-9999-999999999999','post.comment','Bài viết có bình luận mới.','/posts/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa9', NOW(), FALSE),
(gen_random_uuid(),'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa','system.info','Cập nhật nền tảng','/changelog', NOW(), TRUE)
ON CONFLICT ("Id") DO NOTHING;

-- REPORTS (10)
INSERT INTO "Reports" ("Id","ReporterId","Reason","Details","CreatedAt") VALUES
(gen_random_uuid(),'22222222-2222-2222-2222-222222222222','spam','Suspected spam in comments', NOW()),
(gen_random_uuid(),'33333333-3333-3333-3333-333333333333','abuse','Offensive language', NOW()),
(gen_random_uuid(),'44444444-4444-4444-4444-444444444444','duplicate','Duplicate question', NOW()),
(gen_random_uuid(),'55555555-5555-5555-5555-555555555555','misplaced','Wrong category', NOW()),
(gen_random_uuid(),'11111111-1111-1111-1111-111111111111','plagiarism','Copied content suspected', NOW()),
(gen_random_uuid(),'66666666-6666-6666-6666-666666666666','spam','Link dropping', NOW()),
(gen_random_uuid(),'77777777-7777-7777-7777-777777777777','abuse','Personal attack', NOW()),
(gen_random_uuid(),'88888888-8888-8888-8888-888888888888','duplicate','Asked before', NOW()),
(gen_random_uuid(),'99999999-9999-9999-9999-999999999999','offtopic','Not related to forum scope', NOW()),
(gen_random_uuid(),'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa','spam','Unwanted promo', NOW())
ON CONFLICT ("Id") DO NOTHING;

-- SYSTEMLOGS (10)
INSERT INTO "SystemLogs" ("Id","Level","Message","CreatedAt") VALUES
(gen_random_uuid(),'INFO','System boot', NOW()),
(gen_random_uuid(),'INFO','Migrations applied', NOW()),
(gen_random_uuid(),'INFO','Seed executed', NOW()),
(gen_random_uuid(),'WARN','Slow query detected', NOW()),
(gen_random_uuid(),'INFO','Background job started', NOW()),
(gen_random_uuid(),'ERROR','Failed to send email', NOW()),
(gen_random_uuid(),'INFO','Cache warmed', NOW()),
(gen_random_uuid(),'WARN','High memory usage', NOW()),
(gen_random_uuid(),'INFO','Weekly cleanup done', NOW()),
(gen_random_uuid(),'INFO','Healthcheck ok', NOW())
ON CONFLICT ("Id") DO NOTHING;

-- BOTCONVERSATIONS (10)
INSERT INTO "BotConversations" ("Id","OwnerId","Title","CreatedAt","UpdatedAt") VALUES
('d0d0d0d0-0000-0000-0000-000000000001','11111111-1111-1111-1111-111111111111','Algebra helper', NOW(), NOW()),
('d0d0d0d0-0000-0000-0000-000000000002','22222222-2222-2222-2222-222222222222','Algo assistant', NOW(), NOW()),
('d0d0d0d0-0000-0000-0000-000000000003','33333333-3333-3333-3333-333333333333','DB tutor', NOW(), NOW()),
('d0d0d0d0-0000-0000-0000-000000000004','44444444-4444-4444-4444-444444444444','UX tips', NOW(), NOW()),
('d0d0d0d0-0000-0000-0000-000000000005','55555555-5555-5555-5555-555555555555','Testing coach', NOW(), NOW()),
('d0d0d0d0-0000-0000-0000-000000000006','66666666-6666-6666-6666-666666666666','Pointers Q&A', NOW(), NOW()),
('d0d0d0d0-0000-0000-0000-000000000007','77777777-7777-7777-7777-777777777777','Graph guru', NOW(), NOW()),
('d0d0d0d0-0000-0000-0000-000000000008','88888888-8888-8888-8888-888888888888','UI mentor', NOW(), NOW()),
('d0d0d0d0-0000-0000-0000-000000000009','99999999-9999-9999-9999-999999999999','Security guide', NOW(), NOW()),
('d0d0d0d0-0000-0000-0000-000000000010','aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa','Unit test bot', NOW(), NOW())
ON CONFLICT ("Id") DO NOTHING;

-- BOTMESSAGES (10)
INSERT INTO "BotMessages" ("Id","ConversationId","Role","Content","SenderId","CreatedAt") VALUES
(gen_random_uuid(),'d0d0d0d0-0000-0000-0000-000000000001',0,'Help me with eigenvalues','11111111-1111-1111-1111-111111111111', NOW()),
(gen_random_uuid(),'d0d0d0d0-0000-0000-0000-000000000001',1,'Sure, let’s start with definition',NULL, NOW()),
(gen_random_uuid(),'d0d0d0d0-0000-0000-0000-000000000002',0,'What is Dijkstra?','22222222-2222-2222-2222-222222222222', NOW()),
(gen_random_uuid(),'d0d0d0d0-0000-0000-0000-000000000002',1,'Shortest path algorithm', NULL, NOW()),
(gen_random_uuid(),'d0d0d0d0-0000-0000-0000-000000000003',0,'Normalize 3NF vs BCNF?','33333333-3333-3333-3333-333333333333', NOW()),
(gen_random_uuid(),'d0d0d0d0-0000-0000-0000-000000000004',0,'UI spacing tips?','44444444-4444-4444-4444-444444444444', NOW()),
(gen_random_uuid(),'d0d0d0d0-0000-0000-0000-000000000005',0,'How to write tests?','55555555-5555-5555-5555-555555555555', NOW()),
(gen_random_uuid(),'d0d0d0d0-0000-0000-0000-000000000006',0,'Null pointer?','66666666-6666-6666-6666-666666666666', NOW()),
(gen_random_uuid(),'d0d0d0d0-0000-0000-0000-000000000007',0,'Min cut idea','77777777-7777-7777-7777-777777777777', NOW()),
(gen_random_uuid(),'d0d0d0d0-0000-0000-0000-000000000010',1,'Try AAA pattern', NULL, NOW())
ON CONFLICT ("Id") DO NOTHING;

-- REPUTATIONCHECKPOINTS (10)
INSERT INTO "ReputationCheckpoints" ("Id","ContentType","ContentId","LikeBandsApplied","DislikeBandsApplied") VALUES
(gen_random_uuid(),'post','aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1',1,0),
(gen_random_uuid(),'post','aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa2',1,0),
(gen_random_uuid(),'post','aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa3',2,0),
(gen_random_uuid(),'post','aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa4',1,0),
(gen_random_uuid(),'post','aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa5',2,1),
(gen_random_uuid(),'comment','bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb2',1,0),
(gen_random_uuid(),'comment','bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb5',1,0),
(gen_random_uuid(),'comment','bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb6',1,0),
(gen_random_uuid(),'post','aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa7',2,0),
(gen_random_uuid(),'post','aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa9',1,0)
ON CONFLICT ("ContentType","ContentId") DO NOTHING;