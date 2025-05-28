using System;
using System.Windows;
using FuturesBot.Services;

namespace FuturesBot
{
    public partial class MainWindow : Window
    {
        private readonly BinanceService _binanceService;

        public MainWindow()
        {
            InitializeComponent();

            // Укажи свои тестовые ключи
            string apiKey = "YOUR_TESTNET_API_KEY";
            string secretKey = "YOUR_TESTNET_SECRET";

            // Убираем третий параметр Log, вызываем только с двумя
            _binanceService = new BinanceService(apiKey, secretKey);

            try
            {
                int leverage = int.Parse(LeverageBox.Text);
                _binanceService.SetLeverage("BTCUSDT", leverage);
            }
            catch (Exception ex)
            {
                Log($"Ошибка установки плеча: {ex.Message}");
            }
        }

        private void Log(string message)
        {
            Dispatcher.Invoke(() =>
            {
                LogBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
                LogBox.ScrollToEnd();
            });
        }

        private async void OpenLongButton_Click(object sender, RoutedEventArgs e)
        {
            if (!decimal.TryParse(QuantityBox.Text, out var quantity))
            {
                Log("❌ Некорректное значение Quantity");
                return;
            }

            await _binanceService.OpenMarketPosition("BTCUSDT", quantity, isLong: true);
        }

        private async void OpenShortButton_Click(object sender, RoutedEventArgs e)
        {
            if (!decimal.TryParse(QuantityBox.Text, out var quantity))
            {
                Log("❌ Некорректное значение Quantity");
                return;
            }

            await _binanceService.OpenMarketPosition("BTCUSDT", quantity, isLong: false);
        }

        private async void ClosePositionButton_Click(object sender, RoutedEventArgs e)
        {
            await _binanceService.CloseAllPositions("BTCUSDT");
        }
    }
}
