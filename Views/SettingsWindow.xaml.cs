using System.Windows;
using FuturesBot.Helpers;
using FuturesBot.Models;

namespace FuturesBot.Views
{
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();

            var settings = SettingsManager.Load();
            ApiKeyBox.Text = settings.ApiKey;
            SecretKeyBox.Text = settings.SecretKey;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            var settings = new AppSettings
            {
                ApiKey = ApiKeyBox.Text.Trim(),
                SecretKey = SecretKeyBox.Text.Trim()
            };

            SettingsManager.Save(settings);
            MessageBox.Show("Настройки сохранены", "Успешно", MessageBoxButton.OK, MessageBoxImage.Information);
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}