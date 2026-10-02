using HiveCore.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HiveCore.Data.Configurations;

public class FollowConfiguration : IEntityTypeConfiguration<Follow>
{
    public void Configure(EntityTypeBuilder<Follow> builder)
    {
        builder.ToTable("Follows");

        builder.HasKey(f => f.Id);

        // ============================================================
        // Unique composite index — a user can follow another user only once
        // ============================================================
        builder.HasIndex(f => new { f.FollowerId, f.FollowingId })
            .IsUnique()
            .HasDatabaseName("IX_Follows_Follower_Following_Unique");

        builder.HasIndex(f => f.FollowingId)
            .HasDatabaseName("IX_Follows_FollowingId");

        builder.HasIndex(f => new { f.FollowingId, f.IsPending })
            .HasDatabaseName("IX_Follows_FollowingId_IsPending");

        // ============================================================
        // Relationships
        // ============================================================
        builder.HasOne(f => f.Follower)
            .WithMany(u => u.Following)
            .HasForeignKey(f => f.FollowerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(f => f.Following)
            .WithMany(u => u.Followers)
            .HasForeignKey(f => f.FollowingId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}