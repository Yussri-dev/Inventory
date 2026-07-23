using Inventory.Dto.Auth.Results;
using Inventory.LocalDB.Context;
using Inventory.LocalDB.Models;
using Inventory.LocalDB.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Inventory.LocalDB.Services
{
    public class LocalUserSessionService : ILocalUserSessionService
    {
        private readonly PosLocalDbContext _db;

        public LocalUserSessionService(PosLocalDbContext db)
        {
            _db = db;
        }

        public async Task SaveFromAuthResultAsync(
            AuthResult authResult,
            string plainPassword,
            CancellationToken cancellationToken = default)
        {
            if (authResult.UserId == Guid.Empty)
                throw new InvalidOperationException("Invalid user id in auth result.");

            if (string.IsNullOrWhiteSpace(authResult.Email))
                throw new InvalidOperationException("Invalid email in auth result.");

            if (string.IsNullOrWhiteSpace(authResult.Role))
                throw new InvalidOperationException("Invalid role in auth result.");

            if (string.IsNullOrWhiteSpace(plainPassword))
                throw new InvalidOperationException("Password is required for offline login setup.");

            var passwordHash = LocalPasswordHasher.HashPassword(plainPassword);

            var existingSessions = await _db.UserSessions
                .ToListAsync(cancellationToken);

            foreach (var session in existingSessions)
            {
                session.IsActive = false;
            }

            var normalizedEmail = authResult.Email.Trim().ToLower();

            var existing = existingSessions
                .FirstOrDefault(x => x.Email.ToLower() == normalizedEmail);

            if (existing == null)
            {
                existing = new LocalUserSession
                {
                    Id = Guid.NewGuid(),
                    UserId = authResult.UserId,
                    TenantId = authResult.TenantId,
                    Email = authResult.Email.Trim(),
                    FullName = authResult.FullName,
                    Role = authResult.Role,
                    TokenExpiresAtUtc = authResult.ExpiresAt,
                    TrialEndDateUtc = authResult.TrialEndDate,
                    IsTrialActive = authResult.IsTrialActive,
                    LastOnlineLoginAtUtc = DateTime.UtcNow,
                    LastOfflineAccessAtUtc = null,
                    IsActive = true,
                    OfflinePasswordHash = passwordHash.Hash,
                    OfflinePasswordSalt = passwordHash.Salt,
                    OfflinePasswordIterations = passwordHash.Iterations
                };

                _db.UserSessions.Add(existing);
            }
            else
            {
                existing.UserId = authResult.UserId;
                existing.TenantId = authResult.TenantId;
                existing.Email = authResult.Email.Trim();
                existing.FullName = authResult.FullName;
                existing.Role = authResult.Role;
                existing.TokenExpiresAtUtc = authResult.ExpiresAt;
                existing.TrialEndDateUtc = authResult.TrialEndDate;
                existing.IsTrialActive = authResult.IsTrialActive;
                existing.LastOnlineLoginAtUtc = DateTime.UtcNow;
                existing.LastOfflineAccessAtUtc = null;
                existing.IsActive = true;
                existing.OfflinePasswordHash = passwordHash.Hash;
                existing.OfflinePasswordSalt = passwordHash.Salt;
                existing.OfflinePasswordIterations = passwordHash.Iterations;
            }

            await _db.SaveChangesAsync(cancellationToken);
        }

        public async Task<LocalUserSession?> GetCurrentAsync(
            CancellationToken cancellationToken = default)
        {
            return await _db.UserSessions
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.IsActive, cancellationToken);
        }

        public async Task<LocalUserSession?> GetByEmailAsync(
            string email,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(email))
                return null;

            var normalizedEmail = email.Trim().ToLower();

            return await _db.UserSessions
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.Email.ToLower() == normalizedEmail,
                    cancellationToken);
        }

        public async Task<LocalUserSession?> ValidateOfflineLoginAsync(
            string email,
            string password,
            CancellationToken cancellationToken = default)
        {
            var user = await GetByEmailAsync(email, cancellationToken);

            if (user == null)
                return null;

            if (string.IsNullOrWhiteSpace(user.OfflinePasswordHash) ||
                string.IsNullOrWhiteSpace(user.OfflinePasswordSalt))
            {
                return null;
            }

            var isValid = LocalPasswordHasher.VerifyPassword(
                password,
                user.OfflinePasswordHash,
                user.OfflinePasswordSalt,
                user.OfflinePasswordIterations);

            if (!isValid)
                return null;

            var trackedUser = await _db.UserSessions
                .FirstAsync(x => x.Id == user.Id, cancellationToken);

            var allUsers = await _db.UserSessions
                .ToListAsync(cancellationToken);

            foreach (var session in allUsers)
            {
                session.IsActive = false;
            }

            trackedUser.IsActive = true;
            trackedUser.LastOfflineAccessAtUtc = DateTime.UtcNow;

            await _db.SaveChangesAsync(cancellationToken);

            return trackedUser;
        }

        public async Task ClearAsync(CancellationToken cancellationToken = default)
        {
            var sessions = await _db.UserSessions
                .Where(x => x.IsActive)
                .ToListAsync(cancellationToken);

            foreach (var session in sessions)
            {
                session.IsActive = false;
                session.LastOfflineAccessAtUtc = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}