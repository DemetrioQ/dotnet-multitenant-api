using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SaasApi.Application.Features.Users.Commands.RefreshTokens
{
    public record RefreshTokenCommand(string RefreshToken) : IRequest<RefreshTokenResult>;

    public record RefreshTokenResult(string JwtToken, string RefreshToken, DateTime ExpiresAt);
}
