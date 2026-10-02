using HiveCore.Models.Entities;
using HiveCore.Utilities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HiveCore.Data;

/// <summary>
/// Seeds the database with initial roles, an admin user, sample users,
/// posts, follows, likes, comments, and profile themes.
/// Idempotent: safe to run on every startup.
/// </summary>
public static class DataSeeder
{
    // ============================================================
    // Well-known seed accounts
    // ============================================================
    private const string AdminEmail = "admin@hivecore.app";
    private const string AdminUsername = "admin";
    private const string AdminPassword = "Admin#12345";
    private const string AdminDisplayName = "HiveCore Admin";

    private const string ModeratorEmail = "mod@hivecore.app";
    private const string ModeratorUsername = "moderator";
    private const string ModeratorPassword = "Mod#12345";
    private const string ModeratorDisplayName = "HiveCore Moderator";

    private const string DemoPassword = "Demo#12345";

    public static async Task SeedAsync(
        HiveCoreDbContext context,
        UserManager<User> userManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        ILogger logger,
        CancellationToken ct = default)
    {
        try
        {
            await SeedRolesAsync(roleManager, logger);
            var admin = await SeedAdminAsync(userManager, logger);
            await SeedModeratorAsync(userManager, logger);
            var demoUsers = await SeedDemoUsersAsync(userManager, logger);

            await SeedProfileThemesAsync(context, admin, demoUsers, ct);
            await SeedFollowsAsync(context, admin, demoUsers, ct);
            await SeedPostsAsync(context, admin, demoUsers, ct);
            await SeedCommentsAndLikesAsync(context, admin, demoUsers, ct);

            logger.LogInformation("Data seeding completed.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Data seeding failed.");
            throw;
        }
    }

    // ============================================================
    // Roles
    // ============================================================
    private static async Task SeedRolesAsync(
        RoleManager<IdentityRole<Guid>> roleManager,
        ILogger logger)
    {
        var roles = new[]
        {
            Constants.Roles.Admin,
            Constants.Roles.Moderator,
            Constants.Roles.User,
            Constants.Roles.Verified,
            Constants.Roles.Suspended
        };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole<Guid>
                {
                    Id = Guid.NewGuid(),
                    Name = role,
                    NormalizedName = role.ToUpperInvariant()
                });

                logger.LogInformation("Seeded role '{Role}'", role);
            }
        }
    }

    // ============================================================
    // Admin
    // ============================================================
    private static async Task<User> SeedAdminAsync(
        UserManager<User> userManager,
        ILogger logger)
    {
        var existing = await userManager.FindByEmailAsync(AdminEmail);
        if (existing != null) return existing;

        var admin = new User
        {
            Id = Guid.NewGuid(),
            UserName = AdminUsername,
            Email = AdminEmail,
            EmailConfirmed = true,
            DisplayName = AdminDisplayName,
            Bio = "I run HiveCore. Say hi!",
            IsVerified = true,
            IsPrivate = false,
            FollowersCount = 0,
            FollowingCount = 0,
            PostsCount = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var result = await userManager.CreateAsync(admin, AdminPassword);
        if (!result.Succeeded)
        {
            logger.LogError("Failed to create admin: {Errors}",
                string.Join("; ", result.Errors.Select(e => e.Description)));
            throw new InvalidOperationException("Could not seed admin user.");
        }

        await userManager.AddToRoleAsync(admin, Constants.Roles.Admin);
        await userManager.AddToRoleAsync(admin, Constants.Roles.User);
        await userManager.AddToRoleAsync(admin, Constants.Roles.Verified);

        logger.LogInformation("Seeded admin user '{Username}' ({Email}).", AdminUsername, AdminEmail);
        return admin;
    }

    // ============================================================
    // Moderator
    // ============================================================
    private static async Task<User?> SeedModeratorAsync(
        UserManager<User> userManager,
        ILogger logger)
    {
        if (await userManager.FindByEmailAsync(ModeratorEmail) != null) return null;

        var mod = new User
        {
            Id = Guid.NewGuid(),
            UserName = ModeratorUsername,
            Email = ModeratorEmail,
            EmailConfirmed = true,
            DisplayName = ModeratorDisplayName,
            Bio = "Keeping HiveCore a friendly place.",
            IsVerified = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var result = await userManager.CreateAsync(mod, ModeratorPassword);
        if (!result.Succeeded)
        {
            logger.LogWarning("Failed to create moderator: {Errors}",
                string.Join("; ", result.Errors.Select(e => e.Description)));
            return null;
        }

        await userManager.AddToRoleAsync(mod, Constants.Roles.Moderator);
        await userManager.AddToRoleAsync(mod, Constants.Roles.User);

        logger.LogInformation("Seeded moderator user '{Username}'.", ModeratorUsername);
        return mod;
    }

    // ============================================================
    // Demo users
    // ============================================================
    private static readonly (string Username, string Email, string DisplayName, string Bio, bool Verified)[] DemoUsers =
    {
        ("jane",   "jane@hivecore.app",   "Jane Doe",     "Coffee, code, and cats.", true),
        ("ali",    "ali@hivecore.app",    "Ali Rezaei",   "Backend dev. Building things with C#.", false),
        ("sara",   "sara@hivecore.app",   "Sara Ahmadi",  "Designer & illustrator.", true),
        ("max",    "max@hivecore.app",    "Max Müller",   "Photographer. Wandering.", false),
        ("luna",   "luna@hivecore.app",   "Luna Park",    "Frontend. Coffee. Repeat.", false),
        ("omar",   "omar@hivecore.app",   "Omar Khaled",  "Data scientist. Football fan.", false),
        ("nina",   "nina@hivecore.app",   "Nina Petrova", "Music producer. Pet lover.", true),
        ("leo",    "leo@hivecore.app",    "Leo Costa",    "Startup founder. Reader.", false)
    };

    private static async Task<List<User>> SeedDemoUsersAsync(
        UserManager<User> userManager,
        ILogger logger)
    {
        var users = new List<User>();

        foreach (var (username, email, display, bio, verified) in DemoUsers)
        {
            var existing = await userManager.FindByEmailAsync(email);
            if (existing != null)
            {
                users.Add(existing);
                continue;
            }

            var user = new User
            {
                Id = Guid.NewGuid(),
                UserName = username,
                Email = email,
                EmailConfirmed = true,
                DisplayName = display,
                Bio = bio,
                IsVerified = verified,
                FollowersCount = 0,
                FollowingCount = 0,
                PostsCount = 0,
                CreatedAt = DateTime.UtcNow.AddDays(-Random.Shared.Next(5, 90)),
                UpdatedAt = DateTime.UtcNow
            };

            var result = await userManager.CreateAsync(user, DemoPassword);
            if (!result.Succeeded)
            {
                logger.LogWarning("Failed to create demo user {Username}: {Errors}",
                    username, string.Join("; ", result.Errors.Select(e => e.Description)));
                continue;
            }

            await userManager.AddToRoleAsync(user, Constants.Roles.User);
            if (verified) await userManager.AddToRoleAsync(user, Constants.Roles.Verified);

            users.Add(user);
        }

        logger.LogInformation("Seeded {Count} demo users.", users.Count);
        return users;
    }

    // ============================================================
    // Profile themes
    // ============================================================
    private static readonly (string Primary, string Secondary, string Accent, string Bg)[] Palette =
    {
        ("#FF6B35", "#F7C59F", "#EFEFD0", "#004E89"),
        ("#1D9BF0", "#A8D8FF", "#E0F0FF", "#0A3D62"),
        ("#E0245E", "#FFB7CC", "#FFE0EA", "#4A0E2C"),
        ("#00BA7C", "#A8E6CF", "#DCEDC1", "#0B3D2E"),
        ("#7856FF", "#C6B7FF", "#E8E0FF", "#1F0F4A"),
        ("#F59E0B", "#FDE68A", "#FEF3C7", "#78350F")
    };

    private static async Task SeedProfileThemesAsync(
        HiveCoreDbContext context,
        User admin,
        List<User> users,
        CancellationToken ct)
    {
        var all = new List<User> { admin };
        all.AddRange(users);

        foreach (var user in all)
        {
            var hasTheme = await context.ProfileThemes
                .AnyAsync(t => t.UserId == user.Id, ct);

            if (hasTheme) continue;

            var (primary, secondary, accent, bg) = Palette[Random.Shared.Next(Palette.Length)];

            context.ProfileThemes.Add(new ProfileTheme
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                PrimaryColor = primary,
                SecondaryColor = secondary,
                AccentColor = accent,
                BackgroundColor = bg,
                TextColor = "#FFFFFF",
                BackgroundMode = "solid",
                LayoutStyle = "default",
                ShowBadges = true,
                ShowMusicPlayer = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        await context.SaveChangesAsync(ct);
    }

    // ============================================================
    // Follows
    // ============================================================
    private static async Task SeedFollowsAsync(
        HiveCoreDbContext context,
        User admin,
        List<User> users,
        CancellationToken ct)
    {
        var all = new List<User> { admin };
        all.AddRange(users);

        foreach (var follower in all)
        {
            // Each user follows 2-5 random others
            var targets = all
                .Where(u => u.Id != follower.Id)
                .OrderBy(_ => Random.Shared.Next())
                .Take(Random.Shared.Next(2, 6))
                .ToList();

            foreach (var target in targets)
            {
                var exists = await context.Follows.AnyAsync(f =>
                    f.FollowerId == follower.Id && f.FollowingId == target.Id, ct);

                if (exists) continue;

                context.Follows.Add(new Follow
                {
                    Id = Guid.NewGuid(),
                    FollowerId = follower.Id,
                    FollowingId = target.Id,
                    IsPending = false,
                    CreatedAt = DateTime.UtcNow.AddDays(-Random.Shared.Next(1, 30)),
                    AcceptedAt = DateTime.UtcNow
                });

                follower.FollowingCount++;
                target.FollowersCount++;
            }
        }

        await context.SaveChangesAsync(ct);
    }

    // ============================================================
    // Posts
    // ============================================================
    private static readonly string[] SamplePosts =
    {
        "Hello HiveCore! 👋 Excited to be here.",
        "Just shipped a new feature. Feels good. #dev #dotnet",
        "Coffee count today: 4 ☕️ Need to slow down.",
        "Reading about #SignalR and real-time apps. Mind blown 🤯",
        "Sunset from my window tonight 🌅 #photography",
        "Anyone else loving ASP.NET Core 8? #csharp #dotnet",
        "New design exploration for a client project. #design",
        "Morning run done ✅ 5km in 26 min.",
        "Trying out the new chronological feed. So much better!",
        "Weekend plans: nothing. Absolutely nothing. 😌",
        "Thoughts on distributed caching with Redis? #architecture",
        "This UI redesign is finally coming together 🎨 #ui",
        "Just published a new blog post about EF Core performance. Link in bio!",
        "Debugging is just being the detective in a crime movie where you are also the murderer.",
        "New music project in the works 🎵 #music",
        "Sometimes the best code is the code you delete.",
        "Grateful for this community 🙏",
        "Hot take: chronological feeds > algorithms.",
        "Building in public is scary but worth it.",
        "First post of the day! What are you working on? #buildinpublic"
    };

    private static async Task SeedPostsAsync(
        HiveCoreDbContext context,
        User admin,
        List<User> users,
        CancellationToken ct)
    {
        var existingPosts = await context.Posts.CountAsync(ct);
        if (existingPosts >= 10) return;

        var allAuthors = new List<User> { admin };
        allAuthors.AddRange(users);

        foreach (var author in allAuthors)
        {
            var postCount = Random.Shared.Next(2, 5);

            for (int i = 0; i < postCount; i++)
            {
                var content = SamplePosts[Random.Shared.Next(SamplePosts.Length)];
                var createdAt = DateTime.UtcNow.AddHours(-Random.Shared.Next(1, 96));

                context.Posts.Add(new Post
                {
                    Id = Guid.NewGuid(),
                    Content = content,
                    AuthorId = author.Id,
                    Visibility = PostVisibility.Public,
                    LikesCount = 0,
                    CommentsCount = 0,
                    RepostsCount = 0,
                    BookmarksCount = 0,
                    ViewsCount = Random.Shared.Next(5, 500),
                    CreatedAt = createdAt,
                    UpdatedAt = createdAt
                });

                author.PostsCount++;
            }
        }

        await context.SaveChangesAsync(ct);
    }

    // ============================================================
    // Comments + Likes
    // ============================================================
    private static readonly string[] SampleComments =
    {
        "Great point!",
        "Totally agree 👍",
        "I've been thinking the same thing.",
        "Thanks for sharing!",
        "This is awesome 🔥",
        "Interesting perspective.",
        "Couldn't agree more.",
        "Beautiful shot!",
        "Keep it up!",
        "Needed to hear this today."
    };

    private static async Task SeedCommentsAndLikesAsync(
        HiveCoreDbContext context,
        User admin,
        List<User> users,
        CancellationToken ct)
    {
        var allUsers = new List<User> { admin };
        allUsers.AddRange(users);

        var posts = await context.Posts
            .OrderBy(p => p.CreatedAt)
            .Take(30)
            .ToListAsync(ct);

        foreach (var post in posts)
        {
            // Likes
            var likers = allUsers
                .Where(u => u.Id != post.AuthorId)
                .OrderBy(_ => Random.Shared.Next())
                .Take(Random.Shared.Next(0, 6))
                .ToList();

            foreach (var liker in likers)
            {
                var exists = await context.Likes.AnyAsync(l =>
                    l.UserId == liker.Id && l.PostId == post.Id, ct);
                if (exists) continue;

                context.Likes.Add(new Like
                {
                    Id = Guid.NewGuid(),
                    UserId = liker.Id,
                    PostId = post.Id,
                    CreatedAt = DateTime.UtcNow.AddMinutes(-Random.Shared.Next(1, 1440))
                });
                post.LikesCount++;
            }

            // Comments
            var commenters = allUsers
                .OrderBy(_ => Random.Shared.Next())
                .Take(Random.Shared.Next(0, 3))
                .ToList();

            foreach (var c in commenters)
            {
                var text = SampleComments[Random.Shared.Next(SampleComments.Length)];

                context.Comments.Add(new Comment
                {
                    Id = Guid.NewGuid(),
                    Content = text,
                    PostId = post.Id,
                    AuthorId = c.Id,
                    CreatedAt = DateTime.UtcNow.AddMinutes(-Random.Shared.Next(1, 720)),
                    UpdatedAt = DateTime.UtcNow
                });
                post.CommentsCount++;
            }
        }

        await context.SaveChangesAsync(ct);
    }
}