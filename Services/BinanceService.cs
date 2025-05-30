using Binance.Net;
using Binance.Net.Clients;
using Binance.Net.Enums;
using CryptoExchange.Net.Authentication;
using Binance.Net.Interfaces;

namespace FuturesBot.Services
{
    public class BinanceService
    {
        private readonly BinanceRestClient _restClient;
        private readonly BinanceSocketClient _socketClient;
        private readonly BinanceEnvironment _environment;

        public BinanceService(string apiKey, string secretKey, BinanceEnvironment environment)
        {
            _environment = environment;

            var credentials = new ApiCredentials(apiKey, secretKey);

            _restClient = new BinanceRestClient(options =>
            {
                options.ApiCredentials = credentials;
                options.Environment = environment;
                options.AutoTimestamp = true;
                options.ReceiveWindow = TimeSpan.FromSeconds(5);
                options.AllowAppendingClientOrderId = false;

                options.UsdFuturesOptions.TradeRulesBehaviour = TradeRulesBehaviour.AutoComply;
                options.UsdFuturesOptions.TradeRulesUpdateInterval = TimeSpan.FromMinutes(60);
            });

            _socketClient = new BinanceSocketClient(options =>
            {
                options.ApiCredentials = credentials;
                options.Environment = environment;
                options.AllowAppendingClientOrderId = false;
                options.SocketSubscriptionsCombineTarget = 10;

                options.UsdFuturesOptions.TradeRulesBehaviour = TradeRulesBehaviour.AutoComply;
                options.UsdFuturesOptions.TradeRulesUpdateInterval = TimeSpan.FromMinutes(60);
            });
        }

        // Проверка подключения через получение информации об аккаунте
        public async Task<(bool Success, string? ErrorMessage)> CheckConnectionAsync()
        {
            var accountInfoResult = await _restClient.UsdFuturesApi.Account.GetAccountInfoV2Async();

            if (!accountInfoResult.Success)
            {
                string errorMsg = accountInfoResult.Error?.Message ?? "Неизвестная ошибка";
                return (false, errorMsg);
            }

            return (true, null);
        }

        // Получение свечей (кадлов)
        public async Task<List<IBinanceKline>?> GetCandlesAsync(string symbol, KlineInterval interval, int limit)
        {
            TimeSpan klineSpan = interval switch
            {
                KlineInterval.OneMinute => TimeSpan.FromMinutes(1),
                KlineInterval.ThreeMinutes => TimeSpan.FromMinutes(3),
                KlineInterval.FiveMinutes => TimeSpan.FromMinutes(5),
                KlineInterval.FifteenMinutes => TimeSpan.FromMinutes(15),
                KlineInterval.ThirtyMinutes => TimeSpan.FromMinutes(30),
                KlineInterval.OneHour => TimeSpan.FromHours(1),
                KlineInterval.TwoHour => TimeSpan.FromHours(2),
                KlineInterval.FourHour => TimeSpan.FromHours(4),
                KlineInterval.SixHour => TimeSpan.FromHours(6),
                KlineInterval.EightHour => TimeSpan.FromHours(8),
                KlineInterval.TwelveHour => TimeSpan.FromHours(12),
                KlineInterval.OneDay => TimeSpan.FromDays(1),
                KlineInterval.ThreeDay => TimeSpan.FromDays(3),
                KlineInterval.OneWeek => TimeSpan.FromDays(7),
                KlineInterval.OneMonth => TimeSpan.FromDays(30),
                _ => throw new ArgumentException("Неподдерживаемый интервал")
            };

            var startTime = DateTime.UtcNow - klineSpan * limit;

            var result = await _restClient.UsdFuturesApi.ExchangeData.GetKlinesAsync(
                symbol,
                interval,
                startTime: startTime,
                limit: limit);

            if (!result.Success)
                return null;

            return result.Data.ToList();
        }

        // Установка плеча для символа
        public async Task<bool> SetLeverageAsync(string symbol, int leverage)
        {
            if (leverage < 1 || leverage > 125) // Binance обычно поддерживает от 1 до 125
                throw new ArgumentOutOfRangeException(nameof(leverage), "Плечо должно быть от 1 до 125.");

            var result = await _restClient.UsdFuturesApi.Account.ChangeInitialLeverageAsync(symbol, leverage);
            return result.Success;
        }

        // Открытие рыночной позиции
        // quantity - количество базового актива (например, 0.01 BTC)
        public async Task<(bool Success, string? ErrorMessage)> OpenMarketPosition(string symbol, decimal usdAmount, bool isLong)
        {
            var side = isLong ? Binance.Net.Enums.OrderSide.Buy : Binance.Net.Enums.OrderSide.Sell;

            // Получаем текущую цену актива
            var priceResult = await _restClient.UsdFuturesApi.ExchangeData.GetPriceAsync(symbol);
            if (!priceResult.Success || priceResult.Data.Price <= 0)
                return (false, "Не удалось получить цену актива.");

            decimal price = priceResult.Data.Price;

            // Считаем количество контрактов
            decimal quantity = usdAmount / price;

            // Округляем количество до нужной точности (например, 3 знака после запятой)
            quantity = Math.Round(quantity, 3, MidpointRounding.ToZero);

            var orderResult = await _restClient.UsdFuturesApi.Trading.PlaceOrderAsync(
                symbol,
                side,
                Binance.Net.Enums.FuturesOrderType.Market,
                quantity
            );

            if (!orderResult.Success)
                return (false, orderResult.Error?.Message);

            return (true, null);
        }

        // Закрытие всех позиций по символу (путём выставления рыночного ордера в противоположную сторону)
        public async Task<bool> CloseAllPositions(string symbol)
        {
            var positionsResult = await _restClient.UsdFuturesApi.Account.GetPositionInformationAsync();

            if (!positionsResult.Success)
                return false;

            var position = positionsResult.Data.FirstOrDefault(p => p.Symbol == symbol && p.Quantity != 0);

            if (position == null)
                return true; // Позиция уже закрыта

            var side = position.Quantity > 0
                ? Binance.Net.Enums.OrderSide.Sell
                : Binance.Net.Enums.OrderSide.Buy;

            var quantityToClose = Math.Abs(position.Quantity);

            var closeOrderResult = await _restClient.UsdFuturesApi.Trading.PlaceOrderAsync(
                symbol,
                side,
                Binance.Net.Enums.FuturesOrderType.Market,
                quantityToClose);

            return closeOrderResult.Success;
        }

        public BinanceEnvironment GetEnvironment() => _environment;
    }
}
