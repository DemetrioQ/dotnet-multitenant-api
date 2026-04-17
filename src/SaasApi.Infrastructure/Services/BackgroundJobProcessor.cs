using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SaasApi.Application.Common.Interfaces;

namespace SaasApi.Infrastructure.Services;

public class BackgroundJobProcessor(
    IBackgroundJobQueue queue,
    IServiceScopeFactory scopeFactory,
    ILogger<BackgroundJobProcessor> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var workItem = await queue.DequeueAsync(stoppingToken);

            try
            {
                using var scope = scopeFactory.CreateScope();
                await workItem(scope.ServiceProvider, stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing background job");
            }
        }
    }
}
