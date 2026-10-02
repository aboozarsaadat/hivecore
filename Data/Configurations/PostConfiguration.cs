using HiveCore.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HiveCore.Data.Configurations;

public class PostConfiguration : IEntityTypeConfiguration<Post>
{
    public void Configure(EntityTypeBuilder<Post> builder)
    {
        builder.ToTable("Posts");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Content)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(p => p.ModerationNote)
            .HasMaxLength(500);

        builder.Property(p => p.Visibility)
            .HasConversion<int>()
            .HasDefaultValue(PostVisibility.Public);

        // ============================================================
        // Indexes
        // ============================================================
        builder.HasIndex(p => p.AuthorId)
            .HasDatabaseName("IX_Posts_AuthorId");

        builder.HasIndex(p => p.CreatedAt)
            .IsDescending()
            .HasDatabaseName("IX_Posts_CreatedAt_Desc");

        builder.HasIndex(p => new { p.AuthorId, p.CreatedAt })
            .IsDescending(false, true)
            .HasDatabaseName("IX_Posts_AuthorId_CreatedAt_Desc");

        builder.HasIndex(p => p.ParentPostId)
            .HasDatabaseName("IX_Posts_ParentPostId");

        builder.HasIndex(p => new { p.Visibility, p.CreatedAt })
            .IsDescending(false, true)
            .HasDatabaseName("IX_Posts_Visibility_CreatedAt_Desc");

        // ============================================================
        // Relationships
        // ============================================================
        builder.HasOne(p => p.Author)
            .WithMany(u => u.Posts)
            .HasForeignKey(p => p.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.ParentPost)
            .WithMany(p => p.Replies)
            .HasForeignKey(p => p.ParentPostId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(p => p.Comments)
            .WithOne(c => c.Post)
            .HasForeignKey(c => c.PostId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Likes)
            .WithOne(l => l.Post)
            .HasForeignKey(l => l.PostId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Bookmarks)
            .WithOne(b => b.Post)
            .HasForeignKey(b => b.PostId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Media)
            .WithOne(m => m.Post)
            .HasForeignKey(m => m.PostId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}