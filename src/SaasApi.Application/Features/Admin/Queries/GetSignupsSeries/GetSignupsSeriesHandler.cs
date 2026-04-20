using MediatR;
using Microsoft.EntityFrameworkCore;
using SaasApi.Application.Common.Interfaces;

namespace SaasApi.Application.Features.Admin.Queries.GetSignupsSeries;

public class GetSignupsSeriesHandler(IAppDbContext db)
    : IRequestHandler<GetSignupsSeriesQuery, SignupsSeriesDto>
{
    public async Task<SignupsSeriesDto> Handle(GetSignupsSeriesQuery request, CancellationToken ct)
    {
        var days = request.Days is < 1 or > 365 ? 30 : request.Days;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var sinceDate = today.AddDays(-(days - 1));
        var sinceUtc = sinceDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        // Pull raw CreatedAt timestamps for the window, then bucket client-side.
        // Grouping by CAST(CreatedAt AS DATE) works on SQL Server but not SQLite — the
        // tests run on SQLite, and the window is capped at 365 rows-per-day so the
        // transferred row count stays bounded. Switch to SQL-side bucketing once we
        // run SQL Server everywhere.
        List<DateTime> timestamps = request.Entity switch
        {
            SignupEntity.Tenants => await db.Tenants
                .IgnoreQueryFilters()
                .Where(t => t.CreatedAt >= sinceUtc)
                .Select(t => t.CreatedAt)
                .ToListAsync(ct),
            SignupEntity.Users => await db.Users
                .IgnoreQueryFilters()
                .Where(u => u.CreatedAt >= sinceUtc)
                .Select(u => u.CreatedAt)
                .ToListAsync(ct),
            SignupEntity.Customers => await db.Customers
                .IgnoreQueryFilters()
                .Where(c => c.CreatedAt >= sinceUtc)
                .Select(c => c.CreatedAt)
                .ToListAsync(ct),
            _ => new List<DateTime>()
        };

        var counts = timestamps
            .GroupBy(ts => DateOnly.FromDateTime(ts))
            .ToDictionary(g => g.Key, g => g.Count());

        // Zero-fill missing days so the frontend can plot the array straight.
        var points = new List<SignupPointDto>(days);
        for (var i = 0; i < days; i++)
        {
            var d = sinceDate.AddDays(i);
            points.Add(new SignupPointDto(d, counts.TryGetValue(d, out var n) ? n : 0));
        }

        return new SignupsSeriesDto(request.Entity, days, points);
    }
}
