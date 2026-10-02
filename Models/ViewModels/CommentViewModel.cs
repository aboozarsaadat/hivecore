using System.ComponentModel.DataAnnotations;

namespace HiveCore.Models.ViewModels;

public class CommentViewModel
{
    [Required(ErrorMessage = "Comment cannot be empty.")]
    [MaxLength(1000, ErrorMessage = "Comment cannot exceed 1000 characters.")]
    public string Content { get; set; } = string.Empty;

    [Required]
    public Guid PostId { get; set; }

    public Guid? ParentCommentId { get; set; }
}