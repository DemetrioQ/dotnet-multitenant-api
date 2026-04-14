using SaasApi.Domain.Entities;

namespace SaasApi.Application.Common.Interfaces;

public interface IJwtTokenService
{
    string GenerateToken(User user);
}
