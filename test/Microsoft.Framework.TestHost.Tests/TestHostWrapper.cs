// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Microsoft.Framework.TestHost
{
    public class TestHostWrapper
    {
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
            var listener = Task.Run(() => GetMessage(port, "TestDiscovery.Response"));

            var process = RunKRE(project, arguments);
            process.WaitForExit();

            await listener;

            return process.ExitCode;
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
            var listener = Task.Run(() => GetMessage(port, "TestExecution.Response"));

            var process = RunKRE(project, arguments);
            process.WaitForExit();

            await listener;

            return process.ExitCode;
        }

        private static Process RunKRE(string projectDirectory, IEnumerable<string> args)
        {
            // TODO: Mono?

            var allArgs = "/c k Microsoft.Framework.TestHost " + string.Join(" ", args.Select(Quote));
            return Process.Start(new ProcessStartInfo
            {
                FileName = "cmd",
                WorkingDirectory = projectDirectory,
                Arguments = allArgs,
                UseShellExecute = false
            });
        }

        private static string Quote(string arg)
        {
            return "\"" + arg + "\"";
        }

        private void GetMessage(int port, string terminalMessageType)
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

                    var stream = client.GetStream();
                    using (var reader = new BinaryReader(stream))
                    {
                        while (true)
                        {
                            var message = JsonConvert.DeserializeObject<Message>(reader.ReadString());
                            Output.Add(message);

                            if (string.Equals(message.MessageType, terminalMessageType))
                            {
                                var writer = new BinaryWriter(stream);
                                writer.Write(JsonConvert.SerializeObject(new Message
                                {
                                    MessageType = "TestHost.Acknowledge"
                                }));
                            }
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