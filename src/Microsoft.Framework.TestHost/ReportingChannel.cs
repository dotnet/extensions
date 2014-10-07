// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Framework.TestHost
{
    public class ReportingChannel : IDisposable
    {
        public static async Task<ReportingChannel> ListenOn(int port)
        {
            // This fixes the mono incompatibility but ties it to ipv4 connections
            using (var listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                listenSocket.Bind(new IPEndPoint(IPAddress.Loopback, port));
                listenSocket.Listen(10);

                var socket = await AcceptAsync(listenSocket);

                return new ReportingChannel(socket);
            }
        }

        private readonly BinaryWriter _writer;
        private readonly BinaryReader _reader;
        private readonly ManualResetEventSlim _ackWaitHandle;

        private ReportingChannel(Socket socket)
        {
            Socket = socket;

            var stream = new NetworkStream(Socket);
            _writer = new BinaryWriter(stream);
            _reader = new BinaryReader(stream);
            _ackWaitHandle = new ManualResetEventSlim();

            // Waiting for the ack message on a background thread
            new Thread(WaitForAck) { IsBackground = true }.Start();
        }

        public Socket Socket { get; private set; }

        public void Send(Message message)
        {
            lock (_writer)
            {
                try
                {
                    Trace.TraceInformation("[ReportingChannel]: Send({0})", message);
                    _writer.Write(JsonConvert.SerializeObject(message));
                }
                catch (Exception ex)
                {
                    Trace.TraceInformation("[ReportingChannel]: Error sending {0}", ex);
                    throw;
                }
            }
        }

        public void SendError(Exception ex)
        {
            Send(new Message()
            {
                MessageType = "Error",
                Payload = JToken.FromObject(new ErrorMessage()
                {
                    Message = ex.Message,
                }),
            });
        }


        private void WaitForAck()
        {
            try
            {
                var message = JsonConvert.DeserializeObject<Message>(_reader.ReadString());

                if (string.Equals(message.MessageType, "TestHost.Acknowledge"))
                {
                    _ackWaitHandle.Set();
                }
            }
            catch (Exception ex)
            {
                Trace.TraceInformation("[ReportingChannel]: Waiting for ack failed {0}", ex);
            }
        }

        public void Dispose()
        {
            // Wait for a graceful disconnect
            if (_ackWaitHandle.Wait(TimeSpan.FromSeconds(10)))
            {
                Trace.TraceInformation("[ReportingChannel]: Received for ack from test host");
            }
            else
            {
                Trace.TraceInformation("[ReportingChannel]: Timed out waiting for ack from test host");
            }

            Socket.Dispose();
        }

        private static Task<Socket> AcceptAsync(Socket socket)
        {
            return Task.Factory.FromAsync((cb, state) => socket.BeginAccept(cb, state), ar => socket.EndAccept(ar), null);
        }
    }
}