using HiveCore.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HiveCore.Data.Configurations;

public class CommentConfiguration : IEntityTypeConfiguration<Comment>
{
    public void Configure(EntityTypeBuilder<Comment> builder)
    {
        builder.ToTable("Comments");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Content)
            .IsRequired()
            .HasMaxLength(1000);

        // ============================================================
        // Indexes
        // ============================================================
        builder.HasIndex(c => c.PostId)
            .HasDatabaseName("IX_Comments_PostId");

        builder.HasIndex(c => new { c.PostId, c.CreatedAt })
            .IsDescending(false, true)
            .HasDatabaseName("IX_Comments_PostId_CreatedAt_Desc");

        builder.HasIndex(c => c.AuthorId)
            .HasDatabaseName("IX_Comments_AuthorId");

        builder.HasIndex(c => c.ParentCommentId)
            .HasDatabaseName("IX_Comments_ParentCommentId");

        // ============================================================
        // Relationships
        // ============================================================
        builder.HasOne(c => c.Post)
            .WithMany(p => p.Comments)
            .HasForeignKey(c => c.PostId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.Author)
            .WithMany(u => u.Comments)
            .HasForeignKey(c => c.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.ParentComment)
            .WithMany(c => c.Replies)
            .HasForeignKey(c => c.ParentCommentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(c => c.Likes)
            .WithOne(l => l.Comment)
            .HasForeignKey(l => l.CommentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}