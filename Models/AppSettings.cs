namespace FuturesBot.Models
{
    public class AppSettings
    {
        public string ApiKey { get; set; } = "";
        public string SecretKey { get; set; } = "";

        // Новое поле для выбора сети
        public bool UseTestnet { get; set; } = true; // по умолчанию тестнет
    }
}