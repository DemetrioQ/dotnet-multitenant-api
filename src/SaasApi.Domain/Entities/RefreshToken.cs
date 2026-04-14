using SaasApi.Domain.Common;
using System.Security.Cryptography;

namespace SaasApi.Domain.Entities
{
    public class RefreshToken : BaseEntity, ITenantEntity
    {
        public string Token { get; private set; }
        public Guid UserId { get; private set; }
        public Guid TenantId { get; private set; } // needed for the global query filter
        public DateTime ExpiresAt { get; private set; }
        public DateTime? RevokedAt { get; private set; } // null means still valid

        public bool IsExpired => ExpiresAt < DateTime.UtcNow;
        public bool IsValid => RevokedAt == null && !IsExpired;

        private RefreshToken() { } // EF Core

        public static RefreshToken Create(Guid tenantId, Guid userId)
        {
            return new RefreshToken { TenantId = tenantId, UserId = userId, Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)), ExpiresAt = DateTime.UtcNow.AddDays(7) };
        }

        public  void Revoke()
        {
            this.RevokedAt = DateTime.UtcNow;
        }
    }
}
