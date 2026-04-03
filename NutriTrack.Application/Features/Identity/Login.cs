using BCrypt.Net;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NutriTrack.Application.Common;
using NutriTrack.Application.Interfaces;
using NutriTrack.Domain.Identity;
using System;
using System.Collections.Generic;
using System.Text;

namespace NutriTrack.Application.Features.Identity
{
    public record LoginCommand(string Email, string Password) : IRequest<LoginResult>;

    public record LoginResult(string AccessToken, string RefreshToken);

    public class LoginValidator : AbstractValidator<LoginCommand>
    {
        public LoginValidator()
        {
            RuleFor(x => x.Email).NotEmpty().EmailAddress();
            RuleFor(x => x.Password).NotEmpty();
        }
    }

    public class LoginHandler : IRequestHandler<LoginCommand, LoginResult>
    {
        private readonly IAppDbContext _db;
        private readonly IJwtTokenService _jwt;

        public LoginHandler(IAppDbContext db, IJwtTokenService jwt)
        {
            _db = db;
            _jwt = jwt;
        }

        public async Task<LoginResult> Handle(LoginCommand cmd, CancellationToken ct)
        {
            var user = await _db.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email == cmd.Email, ct)
                ?? throw new NotFoundException("Invalid email or password.");

            if (!BCrypt.Net.BCrypt.Verify(cmd.Password, user.PasswordHash))
                throw new NotFoundException("Invalid email or password.");

            var accessToken = _jwt.GenerateAccessToken(user.UserId, user.Role.Name);
            var refreshToken = _jwt.GenerateRefreshToken();

            _db.Add(new RefreshToken
            {
                UserId = user.UserId,
                Token = refreshToken,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            });

            await _db.SaveChangesAsync(ct);

            return new LoginResult(accessToken, refreshToken);
        }
    }
}
