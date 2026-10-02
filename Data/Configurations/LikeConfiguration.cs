using HiveCore.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HiveCore.Data.Configurations;

public class LikeConfiguration : IEntityTypeConfiguration<Like>
{
    public void Configure(EntityTypeBuilder<Like> builder)
    {
        builder.ToTable("Likes");

        builder.HasKey(l => l.Id);

        // ============================================================
        // A user can like a specific post or comment only once
        // ============================================================
        builder.HasIndex(l => new { l.UserId, l.PostId })
            .IsUnique()
            .HasFilter("[PostId] IS NOT NULL")
            .HasDatabaseName("IX_Likes_User_Post_Unique");

        builder.HasIndex(l => new { l.UserId, l.CommentId })
            .IsUnique()
            .HasFilter("[CommentId] IS NOT NULL")
            .HasDatabaseName("IX_Likes_User_Comment_Unique");

        builder.HasIndex(l => l.PostId)
            .HasDatabaseName("IX_Likes_PostId");

        builder.HasIndex(l => l.CommentId)
            .HasDatabaseName("IX_Likes_CommentId");

        // ============================================================
        // Relationships
        // ============================================================
        builder.HasOne(l => l.User)
            .WithMany(u => u.Likes)
            .HasForeignKey(l => l.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(l => l.Post)
            .WithMany(p => p.Likes)
            .HasForeignKey(l => l.PostId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(l => l.Comment)
            .WithMany(c => c.Likes)
            .HasForeignKey(l => l.CommentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}