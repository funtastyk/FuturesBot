﻿using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using FuturesBot.Services;
using FuturesBot.Helpers;
using FuturesBot.Models;
using FuturesBot.Views;
using FuturesBot.Strategies;
using Microsoft.Web.WebView2.Core;
using Binance.Net;

namespace FuturesBot
{
    public partial class MainWindow : Window
    {
        private BinanceService _binanceService;
        private BinanceEnvironment _environment;
        private bool _isAutoTradingEnabled = false;

        private CancellationTokenSource? _tradingCts;
        private Task? _tradingTask;
        private MomentumScalpingStrategy? _strategy;

        public MainWindow()
        {
            InitializeComponent();

            var settings = SettingsManager.Load();
            string apiKey = settings.ApiKey;
            string secretKey = settings.SecretKey;
            bool useTestnet = settings.UseTestnet;

            if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(secretKey))
            {
                Log("⚠️ API ключи не заданы. Откройте настройки.");
                return;
            }

            _environment = useTestnet ? BinanceEnvironment.Testnet : BinanceEnvironment.Live;
            _binanceService = new BinanceService(apiKey, secretKey, _environment);

            BrowserView.NavigationCompleted += BrowserView_NavigationCompleted;
            BrowserView.CoreWebView2InitializationCompleted += BrowserView_CoreWebView2InitializationCompleted;
            LeverageTextBox.KeyDown += LeverageTextBox_KeyDown;

            CheckConnectionAndLogAsync();

            UsdAmountTextBox.Text = "10";
            LeverageTextBox.Text = "1";
        }

        private async void CheckConnectionAndLogAsync()
        {
            try
            {
                var (connected, error) = await _binanceService.CheckConnectionAsync();
                string envText = _environment == BinanceEnvironment.Testnet ? "Testnet" : "Live";

                if (connected)
                    Log($"✅ Успешное подключение к Binance ({envText})");
                else
                    Log($"❌ Не удалось подключиться к Binance ({envText}). Ошибка: {error}");
            }
            catch (Exception ex)
            {
                string envText = _environment == BinanceEnvironment.Testnet ? "Testnet" : "Live";
                Log($"❌ Ошибка подключения ({envText}): {ex.Message}");
            }
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
                    url = "https://" + url;

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

        private void OpenSettings_Click(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new SettingsWindow();
            settingsWindow.Owner = this;

            if (settingsWindow.ShowDialog() == true)
            {
                MessageBox.Show("Настройки применены. Для смены сети перезапустите приложение.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void TestnetButton_Click(object sender, RoutedEventArgs e)
        {
            string url = "https://testnet.binancefuture.com/";
            BrowserView.Source = new Uri(url);
            AddressBar.Text = url;
        }

        private void LiveButton_Click(object sender, RoutedEventArgs e)
        {
            string url = "https://www.binance.com/ru/futures/home";
            BrowserView.Source = new Uri(url);
            AddressBar.Text = url;
        }

        private void ToggleAutoTrading_Click(object sender, RoutedEventArgs e)
        {
            _isAutoTradingEnabled = !_isAutoTradingEnabled;
            ToggleAutoTradingButton.Content = _isAutoTradingEnabled ? "🟢 Автоторговля ВКЛ" : "⚪ Автоторговля ВЫКЛ";
            Log($"🔁 Автоторговля: {(_isAutoTradingEnabled ? "включена" : "отключена")}");

            if (_isAutoTradingEnabled)
                StartStrategy();
            else
                StopStrategy();
        }

        private void StartStrategy()
        {
            if (_tradingTask != null)
                return;

            if (!decimal.TryParse(UsdAmountTextBox.Text.Trim(), out decimal usdAmount) || usdAmount <= 0)
            {
                Log("❌ Некорректное значение суммы USD. Используйте положительное число.");
                return;
            }

            if (!int.TryParse(LeverageTextBox.Text.Trim(), out int leverage) || leverage <= 0)
            {
                Log("❌ Некорректное значение плеча. Используйте положительное целое число.");
                return;
            }

            _strategy = new MomentumScalpingStrategy(_binanceService)
            {
                UsdAmount = usdAmount,
                Leverage = leverage
            };

            _tradingCts = new CancellationTokenSource();
            var token = _tradingCts.Token;

            _tradingTask = Task.Run(async () =>
            {
                Log("▶️ Стратегия запущена.");
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        await _strategy.EvaluateAsync();
                    }
                    catch (Exception ex)
                    {
                        Log($"❗ Ошибка в стратегии: {ex.Message}");
                    }

                    await Task.Delay(TimeSpan.FromSeconds(10), token);
                }
                Log("⏹️ Стратегия остановлена.");
            }, token);
        }

        private void StopStrategy()
        {
            if (_tradingCts == null)
                return;

            _tradingCts.Cancel();
            _tradingTask = null;
        }

        private async void LeverageTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (!int.TryParse(LeverageTextBox.Text.Trim(), out int leverage) || leverage <= 0)
                {
                    Log("❌ Некорректное значение плеча. Используйте положительное целое число.");
                    return;
                }

                try
                {
                    bool result = await _binanceService.SetLeverageAsync("BTCUSDT", leverage);

                    if (result)
                        Log($"🔧 Плечо изменено на {leverage}x.");
                    else
                        Log("❌ Не удалось изменить плечо.");
                }
                catch (Exception ex)
                {
                    Log($"❌ Ошибка при изменении плеча: {ex.Message}");
                }
            }
        }

        private async void OpenPositionButton_Click(object sender, RoutedEventArgs e)
        {
            Log("🚀 Попытка открыть позицию...");

            if (!decimal.TryParse(UsdAmountTextBox.Text.Trim(), out decimal usdAmount) || usdAmount <= 0)
            {
                Log("❌ Некорректное значение суммы USD. Используйте положительное число.");
                return;
            }

            try
            {
                var (success, error) = await _binanceService.OpenMarketPosition("BTCUSDT", usdAmount, true);

                if (success)
                    Log("✅ Позиция LONG успешно открыта.");
                else
                    Log($"❌ Не удалось открыть позицию. Ошибка: {error}");
            }
            catch (Exception ex)
            {
                Log($"❌ Ошибка открытия позиции: {ex.Message}");
            }
        }

        private async void ClosePositionButton_Click(object sender, RoutedEventArgs e)
        {
            Log("🛑 Попытка закрыть позицию...");

            try
            {
                var result = await _binanceService.CloseAllPositions("BTCUSDT");

                if (result)
                    Log("✅ Позиция успешно закрыта.");
                else
                    Log("❌ Не удалось закрыть позицию.");
            }
            catch (Exception ex)
            {
                Log($"❌ Ошибка закрытия позиции: {ex.Message}");
            }
        }
    }
}
