using System;
using System.Threading;
using System.Threading.Tasks;
using FuturesBot.Strategies;

namespace FuturesBot.Services
{
    public class StrategyController
    {
        private readonly ITradingStrategy _strategy;
        private CancellationTokenSource? _cts;

        public bool IsRunning => _cts != null && !_cts.IsCancellationRequested;

        public StrategyController(ITradingStrategy strategy)
        {
            _strategy = strategy;
        }

        public void Start()
        {
            if (IsRunning) return;

            _cts = new CancellationTokenSource();
            Task.Run(() => RunLoopAsync(_cts.Token));
        }

        public void Stop()
        {
            _cts?.Cancel();
        }

        private async Task RunLoopAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    await _strategy.EvaluateAsync();
                }
                catch (Exception ex)
                {
                    // Логировать ошибку или вывести в GUI
                    Console.WriteLine($"Ошибка в стратегии: {ex.Message}");
                }

                await Task.Delay(TimeSpan.FromSeconds(15), ct);
            }
        }
    }
}