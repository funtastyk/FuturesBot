using System;
using System.Linq;
using System.Threading.Tasks;
using Binance.Net.Enums;
using FuturesBot.Services;

namespace FuturesBot.Strategies
{
    public class MomentumScalpingStrategy : ITradingStrategy
    {
        private readonly BinanceService _binanceService;

        public MomentumScalpingStrategy(BinanceService binanceService)
        {
            _binanceService = binanceService;
        }

        public async Task EvaluateAsync()
        {
            var candles = await _binanceService.GetCandlesAsync("BTCUSDT", KlineInterval.ThreeMinutes, 50);

            if (candles == null || candles.Count < 20)
                return;

            var last = candles[^1];
            var prev = candles[^2];

            if (last.ClosePrice > prev.ClosePrice * 1.002m)
            {
                await _binanceService.OpenMarketPosition("BTCUSDT", 0.01m, isLong: true);
            }
            else if (last.ClosePrice < prev.ClosePrice * 0.998m)
            {
                await _binanceService.OpenMarketPosition("BTCUSDT", 0.01m, isLong: false);
            }
        }
    }
}