// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Microsoft.Framework.TestHost
{
    public class TestHostWrapper
    {
        private readonly IServiceProvider _services;

        public TestHostWrapper(IServiceProvider services)
        {
            _services = services;
        }

        public List<Message> Output { get; } = new List<Message>();

        public async Task<int> RunListAsync(string project)
        {
            var port = FindFreePort();

            var arguments = new List<string>();

            arguments.Add("--port");
            arguments.Add(port.ToString());

            arguments.Add("--project");
            arguments.Add(project);

            arguments.Add("list");

            // This will block until the test host opens the port
            var listener = Task.Run(() => GetMessage(port));

            var program = new Program(_services);
            var result = program.Main(arguments.ToArray());

            await listener;

            return result;
        }

        public async Task<int> RunTestsAsync(string project, params string[] tests)
        {
            var port = FindFreePort();

            var arguments = new List<string>();

            arguments.Add("--port");
            arguments.Add(port.ToString());

            arguments.Add("--project");
            arguments.Add(project);

            arguments.Add("run");

            foreach (var test in tests)
            {
                arguments.Add("--test");
                arguments.Add(test);
            }

            // This will block until the test host opens the port
            var listener = Task.Run(() => GetMessage(port));

            var program = new Program(_services);
            var result = program.Main(arguments.ToArray());

            await listener;

            return result;
        }

        private void GetMessage(int port)
        {
            try
            {
                using (var client = new TcpClient())
                {
                    for (var i = 0; i < 10; i++)
                    {
                        try
                        {
                            client.Connect(new IPEndPoint(IPAddress.Loopback, port));
                            break;
                        }
                        catch (SocketException)
                        {
                            Thread.Sleep(100);
                        }
                    }

                    using (var reader = new BinaryReader(client.GetStream()))
                    {
                        while (true)
                        {
                            var message =JsonConvert.DeserializeObject<Message>(reader.ReadString());
                            Output.Add(message);
                        }
                    }
                }
            }
            catch (ObjectDisposedException)
            {
                // Thrown when the socket is closed by the test process.
            }
            catch (EndOfStreamException)
            {
                // Thrown if nothing was written by the test process.
            }
        }

        private int FindFreePort()
        {
            using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                socket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                return ((IPEndPoint)socket.LocalEndPoint).Port;
            }
        }
    }
}