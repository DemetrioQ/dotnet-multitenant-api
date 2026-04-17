using System.Threading.Channels;
using SaasApi.Application.Common.Interfaces;

namespace SaasApi.Infrastructure.Services;

public class BackgroundJobQueue : IBackgroundJobQueue
{
    private readonly Channel<Func<IServiceProvider, CancellationToken, Task>> _queue =
        Channel.CreateBounded<Func<IServiceProvider, CancellationToken, Task>>(100);

    public async ValueTask EnqueueAsync(Func<IServiceProvider, CancellationToken, Task> workItem, CancellationToken ct = default)
    {
        await _queue.Writer.WriteAsync(workItem, ct);
    }

    public async ValueTask<Func<IServiceProvider, CancellationToken, Task>> DequeueAsync(CancellationToken ct)
    {
        return await _queue.Reader.ReadAsync(ct);
    }
}
