using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NutriTrack.Application.Common;
using NutriTrack.Application.Interfaces;
using NutriTrack.Domain.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace NutriTrack.Application.Features.Identity
{
    public record RegisterCommand(string Email, string Password) : IRequest<int>;

    public class RegisterValidator : AbstractValidator<RegisterCommand>
    {
        public RegisterValidator()
        {
            RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(255);
            RuleFor(x => x.Password).NotEmpty().MinimumLength(6).MaximumLength(100);
        }
    }

    public class RegisterHandler : IRequestHandler<RegisterCommand, int>
    {
        private readonly IAppDbContext _db;

        public RegisterHandler(IAppDbContext db) => _db = db;

        public async Task<int> Handle(RegisterCommand cmd, CancellationToken ct)
        {
            var emailTaken = await _db.Users
                .AnyAsync(u => u.Email == cmd.Email, ct);

            if (emailTaken)
                throw new FluentValidation.ValidationException("Email is already in use.");

            var userRole = await _db.Roles
                .FirstOrDefaultAsync(r => r.Name == "User", ct)
                ?? throw new NotFoundException("Default role not found.");

            var user = new User
            {
                Email = cmd.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(cmd.Password),
                RoleId = userRole.RoleId
            };

            _db.Add(user);
            await _db.SaveChangesAsync(ct);

            return user.UserId;
        }
    }
}
