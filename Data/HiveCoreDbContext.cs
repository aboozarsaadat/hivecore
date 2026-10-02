using HiveCore.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HiveCore.Data;

/// <summary>
/// Main EF Core DbContext for HiveCore.
/// Inherits IdentityDbContext to include all Identity tables.
///
/// DESIGN NOTES:
/// -----------------------------------------------------------------
/// 1. All ambiguous relationships (multiple navigations to the same
///    entity type) are configured EXPLICITLY in OnModelCreating.
///
/// 2. ALL foreign keys use DeleteBehavior.Restrict (NOT Cascade).
///    Why? Because HiveCore uses SOFT DELETE for User, Post, and Comment.
///    Hard-deletes are never performed by the application.
///
///    SQL Server forbids "multiple cascade paths" — for example, when a
///    User is referenced by both Recipient and Actor on Notifications.
///    Using Restrict everywhere is the simplest way to avoid these
///    SQL Server errors (1785) while keeping behavior correct.
///
///    If a future admin purge script needs to hard-delete data, it must
///    delete rows in the correct order: Likes → Comments → Media →
///    Bookmarks → Notifications → Posts → Users.
///
/// 3. Global query filters are applied ONLY to Post and Comment.
///    NOT to User — because Post.Author is required and would become
///    null for soft-deleted authors, causing NREs in MapToDto.
/// </summary>
public class HiveCoreDbContext : IdentityDbContext<User, IdentityRole<Guid>, Guid>
{
    public HiveCoreDbContext(DbContextOptions<HiveCoreDbContext> options)
        : base(options)
    {
    }

    // ============================================================
    // DbSets
    // ============================================================
    public DbSet<Post> Posts => Set<Post>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<Like> Likes => Set<Like>();
    public DbSet<Bookmark> Bookmarks => Set<Bookmark>();
    public DbSet<Follow> Follows => Set<Follow>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<Media> Media => Set<Media>();
    public DbSet<ProfileTheme> ProfileThemes => Set<ProfileTheme>();
    public DbSet<MusicTrack> MusicTracks => Set<MusicTrack>();

    // ============================================================
    // Model configuration
    // ============================================================
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // ============================================================
        // 1. Apply IEntityTypeConfiguration classes from this assembly
        //    (UserConfiguration, PostConfiguration, CommentConfiguration,
        //     FollowConfiguration, LikeConfiguration)
        // ============================================================
        builder.ApplyConfigurationsFromAssembly(typeof(HiveCoreDbContext).Assembly);

        // ============================================================
        // 2. Rename Identity tables
        // ============================================================
        builder.Entity<User>(b => b.ToTable("Users"));
        builder.Entity<IdentityRole<Guid>>(b => b.ToTable("Roles"));
        builder.Entity<IdentityUserRole<Guid>>(b => b.ToTable("UserRoles"));
        builder.Entity<IdentityUserClaim<Guid>>(b => b.ToTable("UserClaims"));
        builder.Entity<IdentityUserLogin<Guid>>(b => b.ToTable("UserLogins"));
        builder.Entity<IdentityUserToken<Guid>>(b => b.ToTable("UserTokens"));
        builder.Entity<IdentityRoleClaim<Guid>>(b => b.ToTable("RoleClaims"));

        // ============================================================
        // 3. NOTIFICATION — two relationships with User
        //    Both must be Restrict to avoid SQL Server error 1785.
        // ============================================================
        builder.Entity<Notification>(entity =>
        {
            entity.ToTable("Notifications");
            entity.HasKey(n => n.Id);

            entity.Property(n => n.Message).HasMaxLength(1000);
            entity.Property(n => n.Url).HasMaxLength(500);
            entity.Property(n => n.ImageUrl).HasMaxLength(500);
            entity.Property(n => n.Type).HasConversion<int>();

            // Recipient (required) — Restrict (soft-delete for users)
            entity.HasOne(n => n.Recipient)
                  .WithMany(u => u.Notifications)
                  .HasForeignKey(n => n.RecipientId)
                  .OnDelete(DeleteBehavior.Restrict);

            // Actor (optional) — Restrict
            entity.HasOne(n => n.Actor)
                  .WithMany(u => u.SentNotifications)
                  .HasForeignKey(n => n.ActorId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(n => new { n.RecipientId, n.IsRead })
                  .HasDatabaseName("IX_Notifications_Recipient_IsRead");

            entity.HasIndex(n => new { n.RecipientId, n.CreatedAt })
                  .IsDescending(false, true)
                  .HasDatabaseName("IX_Notifications_Recipient_CreatedAt_Desc");

            entity.HasIndex(n => n.ActorId)
                  .HasDatabaseName("IX_Notifications_ActorId");
        });

        // ============================================================
        // 4. BOOKMARK
        // ============================================================
        builder.Entity<Bookmark>(entity =>
        {
            entity.ToTable("Bookmarks");
            entity.HasKey(b => b.Id);

            entity.HasIndex(b => new { b.UserId, b.PostId })
                  .IsUnique()
                  .HasDatabaseName("IX_Bookmarks_User_Post_Unique");

            entity.HasIndex(b => b.PostId).HasDatabaseName("IX_Bookmarks_PostId");
            entity.HasIndex(b => b.UserId).HasDatabaseName("IX_Bookmarks_UserId");

            entity.HasOne(b => b.User)
                  .WithMany()
                  .HasForeignKey(b => b.UserId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(b => b.Post)
                  .WithMany(p => p.Bookmarks)
                  .HasForeignKey(b => b.PostId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ============================================================
        // 5. MEDIA
        // ============================================================
        builder.Entity<Media>(entity =>
        {
            entity.ToTable("Media");
            entity.HasKey(m => m.Id);

            entity.Property(m => m.Url).IsRequired().HasMaxLength(500);
            entity.Property(m => m.ThumbnailUrl).HasMaxLength(500);
            entity.Property(m => m.FileName).IsRequired().HasMaxLength(255);
            entity.Property(m => m.OriginalFileName).HasMaxLength(255);
            entity.Property(m => m.ContentType).HasMaxLength(100);
            entity.Property(m => m.Extension).HasMaxLength(10);
            entity.Property(m => m.AltText).HasMaxLength(500);
            entity.Property(m => m.Type).HasConversion<int>();

            entity.HasOne(m => m.UploadedBy)
                  .WithMany(u => u.Media)
                  .HasForeignKey(m => m.UploadedById)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(m => m.Post)
                  .WithMany(p => p.Media)
                  .HasForeignKey(m => m.PostId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(m => m.MusicTrack)
                  .WithMany()
                  .HasForeignKey(m => m.MusicTrackId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(m => m.UploadedById).HasDatabaseName("IX_Media_UploadedById");
            entity.HasIndex(m => m.PostId).HasDatabaseName("IX_Media_PostId");
            entity.HasIndex(m => m.MusicTrackId).HasDatabaseName("IX_Media_MusicTrackId");
        });

        // ============================================================
        // 6. PROFILE THEME — 1:1 with User
        // ============================================================
        builder.Entity<ProfileTheme>(entity =>
        {
            entity.ToTable("ProfileThemes");
            entity.HasKey(t => t.Id);

            entity.Property(t => t.PrimaryColor).HasMaxLength(20);
            entity.Property(t => t.SecondaryColor).HasMaxLength(20);
            entity.Property(t => t.AccentColor).HasMaxLength(20);
            entity.Property(t => t.BackgroundColor).HasMaxLength(20);
            entity.Property(t => t.TextColor).HasMaxLength(20);
            entity.Property(t => t.BackgroundGradient).HasMaxLength(500);
            entity.Property(t => t.HeaderGradient).HasMaxLength(500);
            entity.Property(t => t.FontFamily).HasMaxLength(100);
            entity.Property(t => t.BackgroundImageUrl).HasMaxLength(500);
            entity.Property(t => t.BackgroundMode).HasMaxLength(20);
            entity.Property(t => t.LayoutStyle).HasMaxLength(50);

            entity.HasOne(t => t.User)
                  .WithOne(u => u.ProfileTheme)
                  .HasForeignKey<ProfileTheme>(t => t.UserId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(t => t.UserId)
                  .IsUnique()
                  .HasDatabaseName("IX_ProfileThemes_UserId_Unique");
        });

        // ============================================================
        // 7. MUSIC TRACK
        // ============================================================
        builder.Entity<MusicTrack>(entity =>
        {
            entity.ToTable("MusicTracks");
            entity.HasKey(m => m.Id);

            entity.Property(m => m.Title).IsRequired().HasMaxLength(200);
            entity.Property(m => m.Artist).IsRequired().HasMaxLength(200);
            entity.Property(m => m.Album).HasMaxLength(200);
            entity.Property(m => m.CoverUrl).HasMaxLength(500);
            entity.Property(m => m.AudioUrl).HasMaxLength(500);
            entity.Property(m => m.Provider).HasMaxLength(50);
            entity.Property(m => m.ExternalUrl).HasMaxLength(500);

            entity.HasOne(m => m.User)
                  .WithMany(u => u.MusicTracks)
                  .HasForeignKey(m => m.UserId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(m => new { m.UserId, m.DisplayOrder })
                  .HasDatabaseName("IX_MusicTracks_User_Order");
        });

        // ============================================================
        // 8. OVERRIDE cascades from external Configuration classes
        //    (PostConfiguration, CommentConfiguration, LikeConfiguration)
        //
        //    Since we call ApplyConfigurationsFromAssembly BEFORE these
        //    lines, our settings here win.
        // ============================================================

        // Post → Author (was Cascade in PostConfiguration → Restrict)
        builder.Entity<Post>()
               .HasOne(p => p.Author)
               .WithMany(u => u.Posts)
               .HasForeignKey(p => p.AuthorId)
               .OnDelete(DeleteBehavior.Restrict);

        // Post → ParentPost (already Restrict, but explicit)
        builder.Entity<Post>()
               .HasOne(p => p.ParentPost)
               .WithMany(p => p.Replies)
               .HasForeignKey(p => p.ParentPostId)
               .OnDelete(DeleteBehavior.Restrict);

        // Post → Comments (was Cascade in CommentConfiguration → Restrict)
        builder.Entity<Comment>()
               .HasOne(c => c.Post)
               .WithMany(p => p.Comments)
               .HasForeignKey(c => c.PostId)
               .OnDelete(DeleteBehavior.Restrict);

        // Post → Likes (was Cascade in LikeConfiguration → Restrict)
        builder.Entity<Like>()
               .HasOne(l => l.Post)
               .WithMany(p => p.Likes)
               .HasForeignKey(l => l.PostId)
               .OnDelete(DeleteBehavior.Restrict);

        // Comment → Likes (was Cascade in LikeConfiguration → Restrict)
        builder.Entity<Like>()
               .HasOne(l => l.Comment)
               .WithMany(c => c.Likes)
               .HasForeignKey(l => l.CommentId)
               .OnDelete(DeleteBehavior.Restrict);

        // ============================================================
        // 9. GLOBAL QUERY FILTERS
        // ------------------------------------------------------------
        // Only Post and Comment. NOT User.
        // Rationale: Post.Author is required; filtering User would
        // make Post.Author null for soft-deleted authors → NRE.
        // Deleted users are handled at the service layer.
        // ============================================================
        builder.Entity<Post>().HasQueryFilter(p => !p.IsDeleted);
        builder.Entity<Comment>().HasQueryFilter(c => !c.IsDeleted);
    }

    // ============================================================
    // Auto-update timestamps on SaveChanges
    // ============================================================
    public override int SaveChanges()
    {
        ApplyTimestamps();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void ApplyTimestamps()
    {
        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State == EntityState.Added)
            {
                TrySetProperty(entry, "CreatedAt", now);
                TrySetProperty(entry, "UpdatedAt", now);
            }
            else if (entry.State == EntityState.Modified)
            {
                TrySetProperty(entry, "UpdatedAt", now);
            }
        }
    }

    private static void TrySetProperty(
        Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry,
        string propertyName,
        object value)
    {
        var prop = entry.Metadata.FindProperty(propertyName);
        if (prop != null && prop.ClrType == typeof(DateTime))
        {
            entry.Property(propertyName).CurrentValue = value;
        }
    }
}