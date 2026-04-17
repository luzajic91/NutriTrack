namespace NutriTrack.Core.Features.Identity;

// IRequest with no type param = IRequest<Unit>
public record RevokeTokenCommand(string RefreshToken) : IRequest;

public class RevokeTokenValidator : AbstractValidator<RevokeTokenCommand>
{
    public RevokeTokenValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty();
    }
}

public class RevokeTokenHandler : IRequestHandler<RevokeTokenCommand>
{
    private readonly NutriTrackDbContext _db;

    public RevokeTokenHandler(NutriTrackDbContext db) => _db = db;

    public async Task<Unit> Handle(RevokeTokenCommand cmd, CancellationToken ct)
    {
        var token = await _db.RefreshTokens
            .FirstOrDefaultAsync(r => r.Token == cmd.RefreshToken, ct)
            ?? throw new NotFoundException("Refresh token not found.");

        if (!token.IsActive)
            throw new ForbiddenException("Refresh token is already inactive.");

        token.RevokedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return Unit.Value;
    }
}