using HiveCore.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HiveCore.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.Property(u => u.DisplayName)
            .IsRequired()
            .HasMaxLength(80);

        builder.Property(u => u.Bio)
            .HasMaxLength(500);

        builder.Property(u => u.Location)
            .HasMaxLength(200);

        builder.Property(u => u.Website)
            .HasMaxLength(200);

        builder.Property(u => u.AvatarUrl)
            .HasMaxLength(500);

        builder.Property(u => u.CoverUrl)
            .HasMaxLength(500);

        builder.Property(u => u.Pronouns)
            .HasMaxLength(100);

        builder.Property(u => u.PreferredLanguage)
            .HasMaxLength(10)
            .HasDefaultValue("en");

        builder.Property(u => u.TimeZone)
            .HasMaxLength(100)
            .HasDefaultValue("UTC");

        builder.Property(u => u.SuspensionReason)
            .HasMaxLength(500);

        builder.Property(u => u.LastLoginIp)
            .HasMaxLength(64);

        // ============================================================
        // Indexes
        // ============================================================
        builder.HasIndex(u => u.UserName)
            .IsUnique()
            .HasDatabaseName("IX_Users_UserName_Unique");

        builder.HasIndex(u => u.NormalizedUserName)
            .IsUnique()
            .HasDatabaseName("IX_Users_NormalizedUserName_Unique");

        builder.HasIndex(u => u.Email)
            .HasDatabaseName("IX_Users_Email");

        builder.HasIndex(u => u.IsVerified)
            .HasDatabaseName("IX_Users_IsVerified");

        builder.HasIndex(u => u.CreatedAt)
            .HasDatabaseName("IX_Users_CreatedAt");

        // ============================================================
        // Relationships
        // ============================================================
        builder.HasOne(u => u.ProfileTheme)
            .WithOne(pt => pt.User)
            .HasForeignKey<ProfileTheme>(pt => pt.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(u => u.MusicTracks)
            .WithOne(mt => mt.User)
            .HasForeignKey(mt => mt.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}