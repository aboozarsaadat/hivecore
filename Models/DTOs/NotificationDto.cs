using HiveCore.Models.Entities;

namespace HiveCore.Models.DTOs;

public class NotificationDto
{
    public Guid Id { get; set; }
    public NotificationType Type { get; set; }

    public UserSummaryDto? Actor { get; set; }

    public string? Message { get; set; }
    public string? Url { get; set; }
    public string? ImageUrl { get; set; }

    public Guid? PostId { get; set; }
    public Guid? CommentId { get; set; }
    public Guid? FollowId { get; set; }

    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }

    public DateTime CreatedAt { get; set; }
    public string? RelativeTime { get; set; }
}