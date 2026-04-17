using MediatR;
using SaasApi.Application.Common.Models;
using SaasApi.Application.Features.AuditLog.Queries;
using SaasApi.Domain.Entities;
using SaasApi.Domain.Interfaces;

namespace SaasApi.Application.Features.Admin.Queries.GetAllAuditLogs;

public class GetAllAuditLogsHandler(IRepository<AuditLogEntry> auditRepo)
    : IRequestHandler<GetAllAuditLogsQuery, PagedResult<AuditLogEntryDto>>
{
    public async Task<PagedResult<AuditLogEntryDto>> Handle(GetAllAuditLogsQuery request, CancellationToken ct)
    {
        var all = await auditRepo.FindGlobalAsync(_ => true, ct);
        var totalCount = all.Count;

        var paged = all
            .OrderByDescending(a => a.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        var dtos = paged.Select(AuditLogEntryDto.FromEntity).ToList();

        return new PagedResult<AuditLogEntryDto>(dtos, totalCount, request.Page, request.PageSize);
    }
}
