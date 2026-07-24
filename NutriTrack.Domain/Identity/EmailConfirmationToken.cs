using System;

namespace NutriTrack.Domain.Identity;

public class EmailConfirmationToken
{
    public int EmailConfirmationTokenId { get; set; }
    public int UserId { get; set; }
    public string Token { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? ConsumedAt { get; set; }

    public User User { get; set; } = default!;

    public bool IsConsumed => ConsumedAt is not null;
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsActive => !IsConsumed && !IsExpired;
}
