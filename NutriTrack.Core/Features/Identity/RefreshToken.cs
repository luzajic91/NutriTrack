namespace NutriTrack.Core.Features.Identity;

public record RefreshTokenCommand(string RefreshToken) : IRequest<LoginResult>;

public class RefreshTokenValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty();
    }
}

public class RefreshTokenHandler : IRequestHandler<RefreshTokenCommand, LoginResult>
{
    private readonly NutriTrackDbContext _db;
    private readonly JwtTokenService _jwt;

    public RefreshTokenHandler(NutriTrackDbContext db, JwtTokenService jwt)
    {
        _db = db;
        _jwt = jwt;
    }

    public async Task<LoginResult> Handle(RefreshTokenCommand cmd, CancellationToken ct)
    {
        var existing = await _db.RefreshTokens
            .Include(r => r.User)
            .ThenInclude(u => u.Role)
            .FirstOrDefaultAsync(r => r.Token == cmd.RefreshToken, ct)
            ?? throw new NotFoundException("Refresh token not found.");

        if (!existing.IsActive)
            throw new ForbiddenException("Refresh token is no longer active.");

        var newAccessToken = _jwt.GenerateAccessToken(
            existing.User.UserId, existing.User.Role.Name);
        var newRefreshToken = _jwt.GenerateRefreshToken();

        existing.RevokedAt = DateTime.UtcNow;
        existing.ReplacedByToken = newRefreshToken;

        _db.Add(new RefreshToken
        {
            UserId = existing.UserId,
            Token = newRefreshToken,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        });

        await _db.SaveChangesAsync(ct);

        return new LoginResult(newAccessToken, newRefreshToken);
    }
}