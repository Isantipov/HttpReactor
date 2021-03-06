﻿using System;
using System.Net;
using System.Net.Sockets;

namespace HttpReactor.Transport
{
    internal sealed class HttpSocket : IHttpSocket
    {
        private const int WSAEWOULDBLOCK = 10035;
        private readonly Socket _socket;

        public HttpSocket()
        {
            _socket = new Socket(AddressFamily.InterNetwork,
                SocketType.Stream, ProtocolType.Tcp)
            {
                Blocking = false,
                NoDelay = true
            };
        }

        public void Connect(EndPoint endPoint, int timeoutMicros)
        {
            ConnectDontBlock(endPoint);
            ConnectPoll(timeoutMicros);
        }

        public void ConnectDontBlock(EndPoint endPoint)
        {
            try
            {
                _socket.Connect(endPoint);
            }
            catch (SocketException exception)
            {
                if (exception.ErrorCode != WSAEWOULDBLOCK)
                {
                    throw;
                }
            }
        }

        public void ConnectPoll(int timeoutMicros)
        {
            PollOrTimeout("connect", SelectMode.SelectWrite, timeoutMicros);            
        }

        public int Send(byte[] buffer, int offset, int count, int timeoutMicros)
        {
            PollOrTimeout("send", SelectMode.SelectWrite, timeoutMicros);
            return _socket.Send(buffer, offset, count, SocketFlags.None);
        }

        public int Receive(byte[] buffer, int offset, int count, int timeoutMicros)
        {
            PollOrTimeout("receive", SelectMode.SelectRead, timeoutMicros);
            return _socket.Receive(buffer, offset, count, SocketFlags.None);
        }

        public void Close()
        {
            _socket.Shutdown(SocketShutdown.Both);
            _socket.Close();
        }

        public void Dispose()
        {
            _socket.Dispose();
        }

        public int SendBufferSize
        {
            get { return _socket.SendBufferSize; }
        }

        public int ReceiveBufferSize
        {
            get { return _socket.ReceiveBufferSize; }
        }

        private void PollOrTimeout(string socketOperation,
            SelectMode mode, int timeoutMicros)
        {
            if (timeoutMicros < 0 || !_socket.Poll(timeoutMicros, mode))
            {
                ThrowTimeoutException(socketOperation, timeoutMicros);
            }
        }

        private static void ThrowTimeoutException(string socketOperation,
            int timeoutMicros)
        {
            var timeoutMillis = (double)timeoutMicros / 1000;
            throw new TimeoutException(String.Format("{0} timeout {1}",
                socketOperation, TimeSpan.FromMilliseconds(timeoutMillis)));
        }
    }
}
