namespace Metu.MarketData.Kdb
{
    /// <summary>
    ///     Exception representing q connection error.
    /// </summary>
    public class QConnectionException : QException
    {
        public QConnectionException(string message)
            : base(message)
        {
        }
    }
}