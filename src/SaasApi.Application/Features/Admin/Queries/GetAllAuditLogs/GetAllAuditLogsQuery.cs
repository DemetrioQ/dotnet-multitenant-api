using MediatR;
using SaasApi.Application.Common.Models;
using SaasApi.Application.Features.AuditLog.Queries;

namespace SaasApi.Application.Features.Admin.Queries.GetAllAuditLogs;

public record GetAllAuditLogsQuery(int Page = 1, int PageSize = 20) : IRequest<PagedResult<AuditLogEntryDto>>;
