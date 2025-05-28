using System;
using System.Windows;
using FuturesBot.Services;
using FuturesBot.Helpers;
using FuturesBot.Models;
using FuturesBot.Views;

namespace FuturesBot
{
    public partial class MainWindow : Window
    {
        private BinanceService _binanceService;

        public MainWindow()
        {
            InitializeComponent();

            // Загрузка ключей из JSON
            var settings = SettingsManager.Load();
            string apiKey = settings.ApiKey;
            string secretKey = settings.SecretKey;

            if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(secretKey))
            {
                Log("⚠️ API ключи не заданы. Откройте настройки.");
                return;
            }

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

        // Открытие окна настроек
        private void OpenSettings_Click(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new SettingsWindow();
            settingsWindow.Owner = this;
            if (settingsWindow.ShowDialog() == true)
            {
                // Перезапуск MainWindow с обновлёнными ключами
                MessageBox.Show("Настройки применены. Перезапустите приложение для их применения.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}
