using System;
using System.Threading;
using System.Threading.Tasks;

namespace POSSystem.WPF.Helpers
{
    public class Debouncer
    {
        private CancellationTokenSource _cts;
        private readonly int _milliseconds;

        public Debouncer(int milliseconds = 500)
        {
            _milliseconds = milliseconds;
        }

        public async Task RunAsync(Func<Task> action)
        {
            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            try
            {
                await Task.Delay(_milliseconds, token);
                if (!token.IsCancellationRequested)
                {
                    await action();
                }
            }
            catch (TaskCanceledException)
            {
                // Ignored
            }
        }
        
        public async Task RunActionAsync(Action action)
        {
            await RunAsync(() => {
                action();
                return Task.CompletedTask;
            });
        }
    }
}
