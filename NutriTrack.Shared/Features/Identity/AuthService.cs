using NutriTrack.Shared.Email;
using NutriTrack.Shared.Models.Auth;

namespace NutriTrack.Shared.Features.Identity;

public class AuthService
{
    // One day, not a week. The client stores this in localStorage, so any XSS can read it;
    // a shorter window is the cheapest way to limit what a stolen one is worth. An active
    // session rotates well inside a day, so this is invisible to daily users.
    private const int RefreshTokenLifetimeDays = 1;
    private const int EmailConfirmationLifetimeHours = 24;

    /// <summary>
    /// Verified against when no user matches, so an unknown email costs the same as a known one.
    /// Generated rather than hardcoded so it always carries the same work factor as stored
    /// hashes; the one-off cost is paid on first use, not at startup.
    /// </summary>
    private static readonly Lazy<string> DummyPasswordHashFactory =
        new(() => BCrypt.Net.BCrypt.HashPassword("not-a-real-password"));

    private static string DummyPasswordHash => DummyPasswordHashFactory.Value;

    private readonly NutriTrackDbContext _db;
    private readonly JwtTokenService _jwt;
    private readonly IEmailSender _emailSender;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;
    private readonly RegisterValidator _registerValidator;
    private readonly LoginValidator _loginValidator;
    private readonly RefreshTokenValidator _refreshTokenValidator;
    private readonly RevokeTokenValidator _revokeTokenValidator;
    private readonly ConfirmEmailValidator _confirmEmailValidator;
    private readonly ResendConfirmationValidator _resendConfirmationValidator;

    public AuthService(
        NutriTrackDbContext db,
        JwtTokenService jwt,
        IEmailSender emailSender,
        IConfiguration configuration,
        ILogger<AuthService> logger,
        RegisterValidator registerValidator,
        LoginValidator loginValidator,
        RefreshTokenValidator refreshTokenValidator,
        RevokeTokenValidator revokeTokenValidator,
        ConfirmEmailValidator confirmEmailValidator,
        ResendConfirmationValidator resendConfirmationValidator)
    {
        _db = db;
        _jwt = jwt;
        _emailSender = emailSender;
        _configuration = configuration;
        _logger = logger;
        _registerValidator = registerValidator;
        _loginValidator = loginValidator;
        _refreshTokenValidator = refreshTokenValidator;
        _revokeTokenValidator = revokeTokenValidator;
        _confirmEmailValidator = confirmEmailValidator;
        _resendConfirmationValidator = resendConfirmationValidator;
    }

    public async Task<int> Register(RegisterRequest cmd, CancellationToken ct)
    {
        _registerValidator.ValidateAndThrow(cmd);

        var emailTaken = await _db.Users.AnyAsync(u => u.Email == cmd.Email, ct);
        if (emailTaken)
            throw new FluentValidation.ValidationException("Email is already in use.");

        var userRole = await _db.Roles
            .FirstOrDefaultAsync(r => r.Name == "User", ct)
            ?? throw new NotFoundException("Default role not found.");

        var user = new User
        {
            Email = cmd.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(cmd.Password),
            RoleId = userRole.RoleId,
            EmailConfirmed = false
        };

        _db.Add(user);
        await _db.SaveChangesAsync(ct);

        var rawToken = await IssueConfirmationTokenAsync(user, ct);

        // Delivery is best-effort. The account row is already committed, so letting a
        // mail outage bubble up would leave an unconfirmable account that also cannot
        // be registered again. The token is persisted either way and the address owner
        // can ask for a fresh mail via ResendConfirmationEmail.
        var delivered = await TrySendConfirmationEmailAsync(user, rawToken, ct);

        _logger.LogInformation(
            "User {UserId} registered; confirmation email {DeliveryOutcome}",
            user.UserId, delivered ? "sent" : "not delivered");
        return user.UserId;
    }

    public async Task ResendConfirmationEmail(ResendConfirmationRequest cmd, CancellationToken ct)
    {
        _resendConfirmationValidator.ValidateAndThrow(cmd);

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == cmd.Email, ct);

        // Unknown and already-confirmed addresses are a silent no-op: answering them
        // differently would turn this endpoint into an account-enumeration oracle.
        if (user is null || user.EmailConfirmed)
        {
            _logger.LogInformation("Confirmation resend requested for an address that needs no mail");
            return;
        }

        // Retire outstanding links so only the newest one works.
        var outstanding = await _db.EmailConfirmationTokens
            .Where(t => t.UserId == user.UserId && t.ConsumedAt == null)
            .ToListAsync(ct);
        foreach (var token in outstanding)
            token.ConsumedAt = DateTime.UtcNow;

        var rawToken = await IssueConfirmationTokenAsync(user, ct);

        // This is itself the retry path, so a delivery failure is reported rather than
        // swallowed — the caller needs to know the mail did not go out.
        await SendConfirmationEmailAsync(user, rawToken, ct);

        _logger.LogInformation("Confirmation email resent for user {UserId}", user.UserId);
    }

    public async Task ConfirmEmail(ConfirmEmailRequest cmd, CancellationToken ct)
    {
        _confirmEmailValidator.ValidateAndThrow(cmd);

        var suppliedHash = TokenHasher.Hash(cmd.Token);

        var token = await _db.EmailConfirmationTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Token == suppliedHash, ct)
            ?? throw new NotFoundException("Invalid confirmation link.");

        if (!token.IsActive)
            throw new ForbiddenException("Confirmation link has expired or already been used.");

        token.User.EmailConfirmed = true;
        token.ConsumedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Email confirmed for user {UserId}", token.UserId);
    }

    /// <summary>Persists a fresh confirmation token and returns its raw value.</summary>
    private async Task<string> IssueConfirmationTokenAsync(User user, CancellationToken ct)
    {
        // Hex is URL-safe, so the raw token drops straight into the query string.
        var rawToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

        // Only the hash is stored: the raw token exists in the emailed link and nowhere else,
        // so reading this table gives no way to confirm somebody else's address.
        _db.Add(new EmailConfirmationToken
        {
            UserId = user.UserId,
            Token = TokenHasher.Hash(rawToken),
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(EmailConfirmationLifetimeHours)
        });
        await _db.SaveChangesAsync(ct);

        return rawToken;
    }

    /// <summary>Sends the confirmation mail, reporting failure instead of throwing.</summary>
    private async Task<bool> TrySendConfirmationEmailAsync(User user, string rawToken, CancellationToken ct)
    {
        try
        {
            await SendConfirmationEmailAsync(user, rawToken, ct);
            return true;
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            _logger.LogError(ex,
                "Could not deliver the confirmation email for user {UserId}; " +
                "they can request a new one via resend-confirmation", user.UserId);
            return false;
        }
    }

    private async Task SendConfirmationEmailAsync(User user, string rawToken, CancellationToken ct)
    {
        var clientBaseUrl = (_configuration["App:ClientBaseUrl"] ?? string.Empty).TrimEnd('/');
        var link = $"{clientBaseUrl}/confirm-email?token={rawToken}";

        var body =
            $"""
            <p>Welcome to NutriTrack!</p>
            <p>Please confirm your email address by clicking the link below:</p>
            <p><a href="{link}">Confirm my email</a></p>
            <p>This link expires in {EmailConfirmationLifetimeHours} hours. If you didn't create an account, you can ignore this email.</p>
            """;

        await _emailSender.SendAsync(user.Email, "Confirm your NutriTrack account", body, ct);
    }

    public async Task<Result<AuthTokensDto>> Login(LoginRequest cmd, CancellationToken ct)
    {
        _loginValidator.ValidateAndThrow(cmd);

        var user = await _db.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Email == cmd.Email, ct);

        // Always run a verify, even with no user, so the response time does not reveal whether
        // the email is registered. Short-circuiting here would leak that in ~100ms of BCrypt.
        var passwordValid = BCrypt.Net.BCrypt.Verify(
            cmd.Password, user?.PasswordHash ?? DummyPasswordHash);

        if (user is null)
        {
            _logger.LogWarning("Failed login attempt for {Email} (no such user)", cmd.Email);
            return AuthErrors.InvalidCredentials;
        }

        if (!passwordValid)
        {
            _logger.LogWarning("Failed login attempt for user {UserId} (bad password)", user.UserId);
            return AuthErrors.InvalidCredentials;
        }

        if (!user.EmailConfirmed)
            return AuthErrors.EmailNotConfirmed;

        var accessToken = _jwt.GenerateAccessToken(user.UserId, user.Role.Name);
        var refreshToken = _jwt.GenerateRefreshToken();

        var refreshExpiresAt = DateTime.UtcNow.AddDays(RefreshTokenLifetimeDays);

        // The raw token goes to the caller; only its hash is persisted, so a reader of this
        // table cannot use what they find to refresh anybody's session.
        _db.Add(new RefreshToken
        {
            UserId = user.UserId,
            Token = TokenHasher.Hash(refreshToken),
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = refreshExpiresAt
        });

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("User {UserId} logged in", user.UserId);
        return new AuthTokensDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            RefreshTokenExpiresAtUtc = refreshExpiresAt
        };
    }

    public async Task<Result<AuthTokensDto>> RefreshToken(RefreshTokenRequest cmd, CancellationToken ct)
    {
        _refreshTokenValidator.ValidateAndThrow(cmd);

        var suppliedHash = TokenHasher.Hash(cmd.RefreshToken);

        var existing = await _db.RefreshTokens
            .Include(r => r.User)
            .ThenInclude(u => u.Role)
            .FirstOrDefaultAsync(r => r.Token == suppliedHash, ct);

        if (existing is null)
        {
            _logger.LogWarning(
                "Refresh attempted with unknown token {Token}",
                LogMasking.Mask(cmd.RefreshToken));
            return AuthErrors.RefreshTokenInvalid;
        }

        // A revoked token has already been exchanged, so presenting it again means two parties
        // hold it — the legitimate client and whoever copied it out of localStorage. Rotation
        // guarantees only one of them can keep the chain alive, so this replay is the moment the
        // theft becomes visible: retire the lineage descended from it and make both sides
        // re-authenticate. Expiry is not suspicious and is handled below.
        if (existing.IsRevoked)
        {
            var revokedCount = await RevokeDescendantsAsync(existing, ct);

            _logger.LogWarning(
                "Refresh token replay detected for user {UserId} with token {Token}; " +
                "revoked {RevokedCount} descendant token(s)",
                existing.UserId, LogMasking.Mask(cmd.RefreshToken), revokedCount);

            return AuthErrors.RefreshTokenInvalid;
        }

        if (existing.IsExpired)
        {
            // Masks the supplied token rather than the stored hash: both branches then log a
            // fragment of the same string, so the two lines still correlate to one token.
            _logger.LogWarning(
                "Refresh attempted with expired token {Token} for user {UserId}",
                LogMasking.Mask(cmd.RefreshToken), existing.UserId);
            return AuthErrors.RefreshTokenInvalid;
        }

        var newAccessToken = _jwt.GenerateAccessToken(
            existing.User.UserId, existing.User.Role.Name);
        var newRefreshToken = _jwt.GenerateRefreshToken();

        existing.RevokedAt = DateTime.UtcNow;

        // Hashed as well: this column records the rotation chain, and storing the raw
        // replacement here would hand back exactly what hashing Token was meant to withhold.
        existing.ReplacedByToken = TokenHasher.Hash(newRefreshToken);

        var refreshExpiresAt = DateTime.UtcNow.AddDays(RefreshTokenLifetimeDays);

        _db.Add(new RefreshToken
        {
            UserId = existing.UserId,
            Token = TokenHasher.Hash(newRefreshToken),
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = refreshExpiresAt
        });

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Access token refreshed for user {UserId}", existing.UserId);
        return new AuthTokensDto
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken,
            RefreshTokenExpiresAtUtc = refreshExpiresAt
        };
    }

    /// <summary>
    /// Revokes every token descended from <paramref name="replayed"/> by following the rotation
    /// chain recorded in <see cref="RefreshToken.ReplacedByToken"/>, and returns how many were
    /// still active. Only the compromised lineage is retired, so the user's other devices keep
    /// their sessions.
    /// </summary>
    private async Task<int> RevokeDescendantsAsync(RefreshToken replayed, CancellationToken ct)
    {
        // The chain is data, and data can be wrong: a corrupted or looping ReplacedByToken must
        // not spin here, so every hash is visited at most once.
        var visited = new HashSet<string> { replayed.Token };
        var now = DateTime.UtcNow;
        var revoked = 0;

        var nextHash = replayed.ReplacedByToken;

        while (!string.IsNullOrEmpty(nextHash) && visited.Add(nextHash))
        {
            var descendant = await _db.RefreshTokens
                .FirstOrDefaultAsync(r => r.Token == nextHash, ct);

            if (descendant is null)
                break;

            if (descendant.RevokedAt is null)
            {
                descendant.RevokedAt = now;
                revoked++;
            }

            nextHash = descendant.ReplacedByToken;
        }

        if (revoked > 0)
            await _db.SaveChangesAsync(ct);

        return revoked;
    }

    public async Task<Result> RevokeToken(RevokeTokenRequest cmd, CancellationToken ct)
    {
        _revokeTokenValidator.ValidateAndThrow(cmd);

        var suppliedHash = TokenHasher.Hash(cmd.RefreshToken);

        var token = await _db.RefreshTokens
            .FirstOrDefaultAsync(r => r.Token == suppliedHash, ct);

        if (token is null || !token.IsActive)
        {
            _logger.LogWarning(
                "Revoke attempted with unknown or inactive token {Token}",
                LogMasking.Mask(cmd.RefreshToken));
            return AuthErrors.RefreshTokenInvalid;
        }

        token.RevokedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Refresh token revoked for user {UserId}", token.UserId);
        return Result.Success();
    }
}
