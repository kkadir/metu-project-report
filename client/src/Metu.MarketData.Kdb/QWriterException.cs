namespace Metu.MarketData.Kdb
{
    /// <summary>
    ///     Exception representing q writer error.
    /// </summary>
    public class QWriterException : QException
    {
        public QWriterException(string message)
            : base(message)
        {
        }
    }
}