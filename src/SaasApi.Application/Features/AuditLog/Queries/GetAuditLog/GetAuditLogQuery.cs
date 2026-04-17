using MediatR;
using SaasApi.Application.Common.Models;

namespace SaasApi.Application.Features.AuditLog.Queries.GetAuditLog;

public record GetAuditLogQuery(int Page = 1, int PageSize = 20) : IRequest<PagedResult<AuditLogEntryDto>>;
