﻿using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Metu.MarketData.Kdb
{
    /// <summary>
    ///     Connector class for interfacing with the kdb+ service.
    ///     Provides methods for synchronous and asynchronous interaction.
    ///     Methods of QBasicConnection are not thread safe.
    /// </summary>
    public class QBasicConnection : IDisposable, QConnection
    {
        public const int DefaultMaxReadingChunk = 65536;
        private TcpClient _connection;
        private Stream _stream;

        /// <summary>
        ///     Initializes a new QBasicConnection instance.
        /// </summary>
        /// <param name="host">Host of remote q service</param>
        /// <param name="port">Port of remote q service</param>
        /// <param name="username">Username for remote authorization</param>
        /// <param name="password">Password for remote authorization</param>
        /// <param name="encoding">Encoding used for serialization/deserialization of string objects. Default: Encoding.ASCII</param>
        /// <param name="maxReadingChunk">Maximum number of bytes read in a single chunk from stream</param>
        public QBasicConnection(string host = "localhost", int port = 0, string username = null, string password = null,
            Encoding encoding = null, int maxReadingChunk = DefaultMaxReadingChunk)
        {
            Encoding = encoding ?? Encoding.ASCII;
            Host = host;
            Port = port;
            Username = username;
            Password = password;
            MaxReadingChunk = maxReadingChunk;
        }

        private int MaxReadingChunk { get; set; }
        private QReader Reader { get; set; }
        private QWriter Writer { get; set; }

        public void Dispose()
        {
            Close();
        }

        public string Host { get; private set; }
        public int Port { get; private set; }
        public string Username { get; private set; }
        public string Password { get; private set; }
        public Encoding Encoding { get; private set; }
        public int ProtocolVersion { get; private set; }

        public Socket ReadSocket => _connection.Client;

        /// <summary>
        ///     Initializes connection with the remote q service.
        /// </summary>
        public virtual void Open()
        {
            if (IsConnected()) return;
            if (Host != null)
            {
                InitSocket();
                Initialize();

                Reader = new QReader(_stream, Encoding, MaxReadingChunk);
                Writer = new QWriter(_stream, Encoding, ProtocolVersion);
            }
            else
            {
                throw new QConnectionException("Host cannot be null");
            }
        }

        /// <summary>
        ///     Closes connection with the remote q service.
        /// </summary>
        public virtual void Close()
        {
            if (!IsConnected()) return;
            _connection.Close();
            _connection = null;
        }

        /// <summary>
        ///     Reinitializes connection with the remote q service.
        /// </summary>
        public virtual void Reset()
        {
            _connection?.Close();
            _connection = null;
            Open();
        }

        /// <summary>
        ///     Check whether connection with the remote q host has been established and is active.
        /// </summary>
        /// <returns>true if connection with remote host is established and is active, false otherwise</returns>
        public bool IsConnected()
        {
            return _connection is {Connected: true} &&
                   _connection.Client.Connected &&
                   !(_connection.Client.Poll(1000, SelectMode.SelectRead) & (_connection.Client.Available == 0));
        }

        /// <summary>
        ///     Executes a synchronous query against the remote q service.
        /// </summary>
        /// <param name="query">Query to be executed</param>
        /// <param name="parameters">Additional parameters</param>
        /// <returns>deserialized response from the remote q service</returns>
        public virtual object Sync(string query, params object[] parameters)
        {
            Query(MessageType.Sync, query, parameters);
            var response = Reader.Read();

            if (response.MessageType == MessageType.Response)
            {
                return response.Data;
            }
            Writer.Write(new QException("nyi: Metu.MarketData.Kdb expected response message"),
                response.MessageType == MessageType.Async ? MessageType.Async : MessageType.Response);
            throw new QReaderException("Received an " + response.MessageType + " message where response where expected");
        }

        /// <summary>
        ///     Executes an asynchronous query against the remote q service.
        /// </summary>
        /// <param name="query">Query to be executed</param>
        /// <param name="parameters">Additional parameters</param>
        public virtual void Async(string query, params object[] parameters)
        {
            Query(MessageType.Async, query, parameters);
        }

        /// <summary>
        ///     Executes a query against the remote q service.
        ///     Result of the query has to be retrieved by calling a Receive method.
        /// </summary>
        /// <param name="msgType">Indicates whether message should be synchronous or asynchronous</param>
        /// <param name="query">Query to be executed</param>
        /// <param name="parameters">Additional parameters</param>
        public virtual int Query(MessageType msgType, string query, params object[] parameters)
        {
            if (_stream == null)
            {
                throw new QConnectionException("Connection has not been initalized.");
            }

            if (parameters.Length > 8)
            {
                throw new QWriterException("Too many parameters.");
            }

            if (parameters.Length == 0) // simple string query
            {
                return Writer.Write(query.ToCharArray(), msgType);
            }
            var request = new object[parameters.Length + 1];
            request[0] = query.ToCharArray();

            var i = 1;
            foreach (var param in parameters)
            {
                request[i++] = param;
            }

            return Writer.Write(request, msgType);
        }

        /// <summary>
        ///     Reads next message from the remote q service.
        /// </summary>
        /// <param name="dataOnly">
        ///     if true returns only data part of the message, if false retuns data and message meta-information
        ///     encapsulated in QMessage
        /// </param>
        /// <param name="raw">indicates whether message should be parsed to C# object or returned as an array of bytes</param>
        /// <returns>deserialized response from the remote q service</returns>
        public virtual object Receive(bool dataOnly = true, bool raw = false)
        {
            return dataOnly ? Reader.Read(raw).Data : Reader.Read(raw);
        }

        private void InitSocket()
        {
            _connection = new TcpClient(Host, Port);
            _stream = _connection.GetStream();
        }

        private void Initialize()
        {
            var credentials = Password != null ? string.Format("{0}:{1}", Username, Password) : Username;
            var request = Encoding.GetBytes(credentials + "\x3\x0");
            var response = new byte[2];

            _stream.Write(request, 0, request.Length);
            if (_stream.Read(response, 0, 1) != 1)
            {
                Close();
                InitSocket();

                request = Encoding.GetBytes(credentials + "\x0");
                _stream.Write(request, 0, request.Length);
                if (_stream.Read(response, 0, 1) != 1)
                {
                    throw new QConnectionException("Connection denied.");
                }
            }

            ProtocolVersion = Math.Min(response[0], (byte) 3);
        }

        /// <summary>
        ///     Returns a System.String that represents the current QConnection.
        /// </summary>
        /// <returns>A System.String that represents the current QConnection</returns>
        public override string ToString()
        {
            return $":{Host}:{Port}";
        }
    }
}