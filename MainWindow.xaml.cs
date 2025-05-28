using System;
using System.Windows;
using System.Windows.Input;
using FuturesBot.Services;
using FuturesBot.Helpers;
using FuturesBot.Models;
using FuturesBot.Views;
using Microsoft.Web.WebView2.Core;

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

            // Подписка на события WebView2
            BrowserView.NavigationCompleted += BrowserView_NavigationCompleted;
            BrowserView.CoreWebView2InitializationCompleted += BrowserView_CoreWebView2InitializationCompleted;
        }

        private void BrowserView_CoreWebView2InitializationCompleted(object sender, CoreWebView2InitializationCompletedEventArgs e)
        {
            if (e.IsSuccess)
            {
                BrowserView.CoreWebView2.SourceChanged += CoreWebView2_SourceChanged;
            }
        }

        private void CoreWebView2_SourceChanged(object sender, CoreWebView2SourceChangedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                AddressBar.Text = BrowserView.Source?.AbsoluteUri ?? "";
            });
        }

        private void BrowserView_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            BackButton.IsEnabled = BrowserView.CanGoBack;
            ForwardButton.IsEnabled = BrowserView.CanGoForward;
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (BrowserView.CanGoBack)
                BrowserView.GoBack();
        }

        private void ForwardButton_Click(object sender, RoutedEventArgs e)
        {
            if (BrowserView.CanGoForward)
                BrowserView.GoForward();
        }

        private void AddressBar_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                string url = AddressBar.Text.Trim();

                if (!url.StartsWith("http://") && !url.StartsWith("https://"))
                {
                    url = "https://" + url;
                }

                try
                {
                    BrowserView.Source = new Uri(url);
                }
                catch (UriFormatException)
                {
                    Log("❌ Некорректный URL");
                }
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
                MessageBox.Show("Настройки применены. Перезапустите приложение для их применения.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        // Кнопка перехода на тестовую сеть Binance Futures
        private void TestnetButton_Click(object sender, RoutedEventArgs e)
        {
            string url = "https://testnet.binancefuture.com/";
            BrowserView.Source = new Uri(url);
            AddressBar.Text = url;
        }

        // Кнопка перехода на Live Binance Futures
        private void LiveButton_Click(object sender, RoutedEventArgs e)
        {
            string url = "https://www.binance.com/ru/futures/home";
            BrowserView.Source = new Uri(url);
            AddressBar.Text = url;
        }
    }
}
