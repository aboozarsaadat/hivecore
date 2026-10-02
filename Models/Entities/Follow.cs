namespace HiveCore.Models.Entities;

/// <summary>
/// Represents a follow relationship from one user to another.
/// </summary>
public class Follow
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>The user who follows.</summary>
    public Guid FollowerId { get; set; }
    public User Follower { get; set; } = null!;

    /// <summary>The user being followed.</summary>
    public Guid FollowingId { get; set; }
    public User Following { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Whether the follow request is pending approval (for private accounts).</summary>
    public bool IsPending { get; set; } = false;

    public DateTime? AcceptedAt { get; set; }
}