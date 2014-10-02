// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.AspNet.TestHost
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

        private ReportingChannel(Socket socket)
        {
            Socket = socket;

            var stream = new NetworkStream(Socket);
            _writer = new BinaryWriter(stream);
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

        public void Dispose()
        {
            Socket.Dispose();
        }

        private static Task<Socket> AcceptAsync(Socket socket)
        {
            return Task.Factory.FromAsync((cb, state) => socket.BeginAccept(cb, state), ar => socket.EndAccept(ar), null);
        }
    }
}