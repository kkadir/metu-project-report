using System;

namespace Metu.MarketData.Kdb
{
    /// <summary>
    ///     Exception representing q error.
    /// </summary>
    public class QException : Exception
    {
        public QException(string message)
            : base(message)
        {
        }
    }
}