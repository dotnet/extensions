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

namespace Microsoft.Framework.TestHost.Client
{
    public class TestHostWrapper
    {
        public DataReceivedEventHandler ConsoleOutputReceived;

        public EventHandler<Message> MessageReceived;

        public TestHostWrapper()
            : this(Client.DNX.FindDnx())
        {

        }

        public TestHostWrapper(string dnx)
            : this(dnx, debug: false)
        {
            
        }

        public TestHostWrapper(string dnx, bool debug)
        {
            DNX = dnx;
            Debug = debug;

            Output = new List<Message>();
        }

        private bool Debug { get; }

        private string DNX { get; }

        public IList<Message> Output { get; }

        public async Task<int> RunListAsync(string project)
        {
            var port = FindFreePort();

            var arguments = new List<string>();

            arguments.Add("--port");
            arguments.Add(port.ToString());

            arguments.Add("--project");
            arguments.Add(project);

            if (Debug)
            {
                arguments.Add("--debug");
            }

            arguments.Add("list");

            // This will block until the test host opens the port
            var listener = Task.Run(() => GetMessage(port, "TestDiscovery.Response"));

            var process = RunDNX(project, arguments);
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

            if (Debug)
            {
                arguments.Add("--debug");
            }

            arguments.Add("run");

            foreach (var test in tests)
            {
                arguments.Add("--test");
                arguments.Add(test);
            }

            // This will block until the test host opens the port
            var listener = Task.Run(() => GetMessage(port, "TestExecution.Response"));

            var process = RunDNX(project, arguments);
            process.WaitForExit();

            await listener;

            return process.ExitCode;
        }

        private Process RunDNX(string project, IEnumerable<string> args)
        {
            if (project.EndsWith("project.json", StringComparison.OrdinalIgnoreCase))
            {
                project = Path.GetDirectoryName(project);
            }

            var allArgs = ". Microsoft.Framework.TestHost " + string.Join(" ", args.Select(Quote));
            var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                Arguments = allArgs,
                CreateNoWindow = true,
                FileName = DNX,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                WorkingDirectory = project,
            };

            process.OutputDataReceived += Process_OutputDataReceived;
            process.ErrorDataReceived += Process_OutputDataReceived;

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            return process;
        }

        private void OnMessageReceived(object sender, Message e)
        {
            Output.Add(e);

            var handler = MessageReceived;
            if (handler != null)
            {
                handler(sender, e);
            }
        }

        private void Process_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            var handler = ConsoleOutputReceived;
            if (handler != null && e.Data != null)
            {
                handler(sender, e);
            }
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

                    if (!client.Connected)
                    {
                        throw new Exception("Unable to connect.");
                    }

                    var stream = client.GetStream();
                    using (var reader = new BinaryReader(stream))
                    {
                        while (true)
                        {
                            var message = JsonConvert.DeserializeObject<Message>(reader.ReadString());
                            OnMessageReceived(this, message);

                            if (string.Equals(message.MessageType, terminalMessageType) ||
                                string.Equals(message.MessageType, "Error"))
                            {
                                var writer = new BinaryWriter(stream);
                                writer.Write(JsonConvert.SerializeObject(new Message
                                {
                                    MessageType = "TestHost.Acknowledge"
                                }));

                                break;
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
