using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace NutriTrack.Domain.Identity;

public class User
{
    public int UserId { get; set; }
    public int RoleId { get; set; }
    public string Email { get; set; } = default!;
    public string PasswordHash { get; set; } = default!;

    public Role Role { get; set; } = default!;
    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
}