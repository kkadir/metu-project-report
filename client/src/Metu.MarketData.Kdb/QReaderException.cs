namespace Metu.MarketData.Kdb
{
    /// <summary>
    ///     Exception representing q reader error.
    /// </summary>
    public class QReaderException : QException
    {
        public QReaderException(string message)
            : base(message)
        {
        }
    }
}