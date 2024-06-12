using System.Text;

namespace Metu.MarketData.Kdb
{
    /// <summary>
    ///     Represents q message type.
    /// </summary>
    public enum MessageType : byte
    {
        Async,
        Sync,
        Response
    }
}