// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Text;
using System.Net.Sockets;

namespace Microsoft.Extensions.Hosting.SystemdServices
{
    public class SystemdNotifier : ISystemdNotifier
    {
        const string NOTIFY_SOCKET = "NOTIFY_SOCKET";

        private readonly string _socketPath;

        public SystemdNotifier() :
            this(GetNotifySocketPath())
        { }

        // For testing
        internal SystemdNotifier(string socketPath)
        {
            _socketPath = socketPath;
        }

        /// <inheritdoc />
        public bool IsEnabled => _socketPath != null;

        /// <inheritdoc />
        public void Notify(ServiceState state, ServiceState[] states)
        {
            if (!IsEnabled)
            {
                return;
            }

            byte[] data;
            if (states.Length == 0)
            {
                data = Encoding.UTF8.GetBytes(state.ToString());
            }
            else
            {
                var ms = new MemoryStream();
                AppendState(ms, state);
                for (int i = 0; i < states.Length; i++)
                {
                    ms.WriteByte((byte)'\n');
                    AppendState(ms, states[i]);
                }
                data = ms.ToArray();
            }

            using (var socket = new Socket(AddressFamily.Unix, SocketType.Dgram, ProtocolType.Unspecified))
            {
                var endPoint = new UnixDomainSocketEndPoint(_socketPath);
                socket.Connect(endPoint);

                int rv = socket.Send(data);
            }
        }

        private static string GetNotifySocketPath()
        {
            string socketPath = Environment.GetEnvironmentVariable(NOTIFY_SOCKET);

            if (string.IsNullOrEmpty(socketPath))
            {
                return null;
            }

            // Support abstract socket paths.
            if (socketPath[0] == '@')
            {
                socketPath = "\0" + socketPath.Substring(1);
            }

            return socketPath;
        }

        private static void AppendState(MemoryStream stream, ServiceState state)
        {
            var buffer = Encoding.UTF8.GetBytes(state.ToString());
            stream.Write(buffer, 0, buffer.Length);
        }
    }
}