using MediatR;
using SaasApi.Application.Common.Models;
using SaasApi.Domain.Entities;
using SaasApi.Domain.Interfaces;

namespace SaasApi.Application.Features.AuditLog.Queries.GetAuditLog;

public class GetAuditLogHandler(IRepository<AuditLogEntry> auditRepo)
    : IRequestHandler<GetAuditLogQuery, PagedResult<AuditLogEntryDto>>
{
    public async Task<PagedResult<AuditLogEntryDto>> Handle(GetAuditLogQuery request, CancellationToken ct)
    {
        int skip = (request.Page - 1) * request.PageSize;
        var entries = await auditRepo.GetPagedDescAsync(skip, request.PageSize, ct);
        int totalCount = await auditRepo.CountAsync(ct);

        var dtos = entries.Select(AuditLogEntryDto.FromEntity).ToList();

        return new PagedResult<AuditLogEntryDto>(dtos, totalCount, request.Page, request.PageSize);
    }
}
