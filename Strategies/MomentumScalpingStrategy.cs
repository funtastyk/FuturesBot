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

        // Новые параметры
        public decimal UsdAmount { get; set; } = 10m;
        public int Leverage { get; set; } = 1;

        public MomentumScalpingStrategy(BinanceService binanceService)
        {
            _binanceService = binanceService;
        }

        public async Task EvaluateAsync()
        {
            var symbol = "BTCUSDT";

            var candles = await _binanceService.GetCandlesAsync(symbol, KlineInterval.ThreeMinutes, 50);
            if (candles == null || candles.Count < 20)
                return;

            var last = candles[^1];
            var prev = candles[^2];

            // Получаем текущую цену закрытия
            decimal currentPrice = last.ClosePrice;

            // Рассчитываем размер позиции в BTC:
            // Размер позиции = (UsdAmount * Leverage) / Цена
            decimal positionSize = Math.Round((UsdAmount * Leverage) / currentPrice, 6); // округляем до 6 знаков

            if (last.ClosePrice > prev.ClosePrice * 1.002m)
            {
                await _binanceService.OpenMarketPosition(symbol, positionSize, isLong: true);
            }
            else if (last.ClosePrice < prev.ClosePrice * 0.998m)
            {
                await _binanceService.OpenMarketPosition(symbol, positionSize, isLong: false);
            }
        }
    }
}