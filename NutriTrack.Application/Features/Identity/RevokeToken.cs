using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NutriTrack.Application.Common;
using NutriTrack.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace NutriTrack.Application.Features.Identity
{
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
        private readonly IAppDbContext _db;

        public RevokeTokenHandler(IAppDbContext db) => _db = db;

        public async Task Handle(RevokeTokenCommand cmd, CancellationToken ct)
        {
            var token = await _db.RefreshTokens
                .FirstOrDefaultAsync(r => r.Token == cmd.RefreshToken, ct)
                ?? throw new NotFoundException("Refresh token not found.");

            if (!token.IsActive)
                throw new ForbiddenException("Refresh token is already inactive.");

            token.RevokedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);
        }
    }
}
