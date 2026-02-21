using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace FiberHelp.Services
{
    // Simple background scheduler to periodically sync Cloud -> Local cache.
    public class SyncScheduler
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly TimeSpan _interval;
        private CancellationTokenSource? _cts;

        public SyncScheduler(IServiceScopeFactory scopeFactory, TimeSpan? interval = null)
        {
            _scopeFactory = scopeFactory;
            _interval = interval ?? TimeSpan.FromMinutes(1); // default 1 minute
        }

        public void Start()
        {
            _cts = new CancellationTokenSource();
            _ = Task.Run(() => RunAsync(_cts.Token));
        }

        public void Stop()
        {
            _cts?.Cancel();
        }

        private async Task RunAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var sync = scope.ServiceProvider.GetRequiredService<DataSyncService>();
                    await sync.SyncCloudToLocalAsync();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"SyncScheduler error: {ex.Message}");
                }

                try { await Task.Delay(_interval, token); } catch { }
            }
        }
    }
}
