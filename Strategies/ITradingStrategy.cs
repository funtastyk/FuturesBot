namespace FuturesBot.Strategies
{
    public interface ITradingStrategy
    {
        Task EvaluateAsync();
    }
}