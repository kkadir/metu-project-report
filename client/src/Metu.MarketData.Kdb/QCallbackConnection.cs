using System;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Metu.MarketData.Kdb
{
    /// <summary>
    ///     Encapsulates a message received from kdb+.
    /// </summary>
    public class QMessageEvent : EventArgs
    {
        /// <summary>
        ///     The wrapped message.
        /// </summary>
        public readonly QMessage Message;

        /// <summary>
        ///     Creates new QMessageEvent object.
        /// </summary>
        /// <param name="message">message received from kdb+</param>
        public QMessageEvent(QMessage message)
        {
            Message = message;
        }
    }

    /// <summary>
    ///     Encapsulates an error encountered during receiving data from kdb+.
    /// </summary>
    public class QErrorEvent : EventArgs
    {
        /// <summary>
        ///     The source exception.
        /// </summary>
        public readonly Exception Cause;

        /// <summary>
        ///     Creates new QErrorEvent object.
        /// </summary>
        /// <param name="cause">original cause</param>
        public QErrorEvent(Exception cause)
        {
            Cause = cause;
        }
    }

    /// <summary>
    ///     The QCallbackConnection, in addition to QBasicConnection, provides an internal thread-based mechanism
    ///     for asynchronous subscription.
    ///     Methods of QCallbackConnection are not thread safe.
    /// </summary>
    public class QCallbackConnection : QBasicConnection
    {
        /// <summary>
        ///     Initializes a new QCallbackConnection instance.
        /// </summary>
        /// <param name="host">Host of remote q service</param>
        /// <param name="port">Port of remote q service</param>
        /// <param name="username">Username for remote authorization</param>
        /// <param name="password">Password for remote authorization</param>
        /// <param name="encoding">Encoding used for serialization/deserialization of string objects</param>
        public QCallbackConnection(string host = "localhost", int port = 0, string username = null,
            string password = null,
            Encoding encoding = null, int connectionId = 0)
            : base(host, port, username, password, encoding)
        {
            ConnectionId = connectionId;
        }

        public int ConnectionId { get; set; }
        public event EventHandler<QMessageEvent> DataReceived;
        public event EventHandler<QErrorEvent> ErrorOccured;

        protected virtual void OnDataReceived(QMessageEvent e)
        {
            try
            {
                DataReceived?.Invoke(this, e);
            }catch{ }
        }

        protected virtual void OnErrorOccured(QErrorEvent e)
        {
            try
            {
                ErrorOccured?.Invoke(this, e);
            }catch {}
        }

        /// <summary>
        ///     Spawns a new task which listens for messages from the remote q host.
        /// </summary>
        public virtual void StartListener(CancellationToken cancellationToken)
        {
            _ = Task.Run(async () => await StartListenerLoop(cancellationToken), cancellationToken);
        }
        
        private const int READ_ITERATIONS = 100;
        
        private async Task StartListenerLoop(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested && IsConnected())
                {
                    var count = 0;
                    while (!token.IsCancellationRequested && count < READ_ITERATIONS && IsConnected())
                    {
                        count++;
                        try
                        {
                            var data = Receive(false);

                            if (data is QMessage {Data: { }} message) OnDataReceived(new QMessageEvent(message));
                        }
                        catch (QException e)
                        {
                            OnErrorOccured(new QErrorEvent(e));
                        }
                        catch (Exception e)
                        {
                            OnErrorOccured(new QErrorEvent(e));
                        }
                    }

                    await Task.Delay(1, token);
                }
            }
            catch (Exception e)
            {
                OnErrorOccured(new QErrorEvent(e));
            }
            finally
            {
                if (IsConnected())
                    Close();
            }
        }
    }
}