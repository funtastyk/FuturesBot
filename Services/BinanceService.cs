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

        public BinanceService(string apiKey, string secretKey)
        {
            var credentials = new ApiCredentials(apiKey, secretKey);

            _restClient = new BinanceRestClient(options =>
            {
                options.ApiCredentials = credentials;
                options.Environment = BinanceEnvironment.Testnet;
                options.AutoTimestamp = true;
                options.ReceiveWindow = TimeSpan.FromSeconds(5);
                options.AllowAppendingClientOrderId = false;

                options.UsdFuturesOptions.TradeRulesBehaviour = TradeRulesBehaviour.AutoComply;
                options.UsdFuturesOptions.TradeRulesUpdateInterval = TimeSpan.FromMinutes(60);
            });

            _socketClient = new BinanceSocketClient(options =>
            {
                options.ApiCredentials = credentials;
                options.Environment = BinanceEnvironment.Testnet;
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
                throw new Exception($"Ошибка установки плеча: {result.Error}");
        }

        // Пример методов для открытия и закрытия позиций (чтобы компилировалось)
        public async Task OpenMarketPosition(string symbol, decimal quantity, bool isLong)
        {
            // Здесь твоя логика открытия позиции
        }

        public async Task CloseAllPositions(string symbol)
        {
            // Здесь твоя логика закрытия позиции
        }
    }
}
