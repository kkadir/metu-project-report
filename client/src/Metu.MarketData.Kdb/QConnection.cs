using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Metu.MarketData.Kdb
{

    /// <summary>
    ///     Interface for the q connector.
    ///     Defines methods for synchronous and asynchronous interaction.
    /// </summary>
    public interface QConnection
    {
        string Host { get; }
        int Port { get; }
        string Username { get; }
        string Password { get; }
        Encoding Encoding { get; }
        int ProtocolVersion { get; }

        /// <summary>
        ///     Initializes connection with the remote q service.
        /// </summary>
        void Open();

        /// <summary>
        ///     Reinitializes connection with the remote q service.
        /// </summary>
        void Reset();

        /// <summary>
        ///     Closes connection with the remote q service.
        /// </summary>
        void Close();

        /// <summary>
        ///     Check whether connection with the remote q host has been established. Note that this function doesn't check whether
        ///     the connection is still active.
        /// </summary>
        /// <returns>true if connection with remote host is established, false otherwise</returns>
        bool IsConnected();

        /// <summary>
        ///     Executes a synchronous query against the remote q service.
        /// </summary>
        /// <param name="query">Query to be executed</param>
        /// <param name="parameters">Additional parameters</param>
        /// <returns>deserialized response from the remote q service</returns>
        object Sync(string query, params object[] parameters);

        /// <summary>
        ///     Executes an asynchronous query against the remote q service.
        /// </summary>
        /// <param name="query">Query to be executed</param>
        /// <param name="parameters">Additional parameters</param>
        void Async(string query, params object[] parameters);

        /// <summary>
        ///     Executes a query against the remote q service.
        ///     Result of the query has to be retrieved by calling a Receive method.
        /// </summary>
        /// <param name="msgType">Indicates whether message should be synchronous or asynchronous</param>
        /// <param name="query">Query to be executed</param>
        /// <param name="parameters">Additional parameters</param>
        /// <returns>number of written bytes</returns>
        int Query(MessageType msgType, string query, params object[] parameters);

        /// <summary>
        ///     Reads next message from the remote q service.
        /// </summary>
        /// <param name="dataOnly">
        ///     if true returns only data part of the message, if false retuns data and message meta-information
        ///     encapsulated in QMessage
        /// </param>
        /// <param name="raw">indicates whether message should be parsed to C# object or returned as an array of bytes</param>
        /// <returns>deserialized response from the remote q service</returns>
        object Receive(bool dataOnly = true, bool raw = false);

        /// <summary>
        /// Provide socket access for implementations that do not use streams.
        /// </summary>
        Socket ReadSocket { get; }
    }
}