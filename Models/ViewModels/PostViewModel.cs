using System.ComponentModel.DataAnnotations;
using HiveCore.Models.DTOs;
using Microsoft.AspNetCore.Http;

namespace HiveCore.Models.ViewModels;

public class PostViewModel
{
    public Guid? Id { get; set; }

    [Required(ErrorMessage = "Post content cannot be empty.")]
    [MaxLength(2000, ErrorMessage = "Post cannot exceed 2000 characters.")]
    [Display(Name = "What's happening?")]
    public string Content { get; set; } = string.Empty;

    public Guid? ParentPostId { get; set; }

    public bool IsQuote { get; set; }

    public HiveCore.Models.Entities.PostVisibility Visibility { get; set; }
        = HiveCore.Models.Entities.PostVisibility.Public;

    public bool IsSensitive { get; set; }

    // ============================================================
    // Media — new uploads
    // ============================================================
    [Display(Name = "Media")]
    public List<IFormFile>? MediaFiles { get; set; }

    // ============================================================
    // Media — existing items (for edit form display)
    // ============================================================
    public List<MediaDto> ExistingMedia { get; set; } = new();

    // ============================================================
    // Media — IDs to be soft-deleted on save
    // ============================================================
    public List<Guid> MediaIdsToDelete { get; set; } = new();
}

public class PostDetailsViewModel
{
    public PostDto Post { get; set; } = null!;
    public CommentViewModel NewComment { get; set; } = new();
    public List<CommentDto> Comments { get; set; } = new();
    public bool IsLikedByCurrentUser { get; set; }
    public bool IsBookmarkedByCurrentUser { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }
}