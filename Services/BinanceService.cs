using Binance.Net;
using Binance.Net.Clients;
using Binance.Net.Enums;
using Binance.Net.Objects.Options;
using CryptoExchange.Net.Authentication;
using System;
using System.Threading.Tasks;

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

        public async Task SetLeverage(string symbol, int leverage)
        {
            var result = await _restClient.UsdFuturesApi.Account.ChangeInitialLeverageAsync(symbol, leverage);
            if (!result.Success)
                throw new Exception($"Ошибка установки плеча: {result.Error?.Message ?? result.Error?.ToString()}");
        }

        // Проверка подключения через получение информации об аккаунте (актуальный метод)
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

        // Пример методов для открытия и закрытия позиций (пока заглушки)
        public async Task OpenMarketPosition(string symbol, decimal quantity, bool isLong)
        {
            // Логика открытия позиции
        }

        public async Task CloseAllPositions(string symbol)
        {
            // Логика закрытия позиции
        }

        public BinanceEnvironment GetEnvironment() => _environment;
    }
}
