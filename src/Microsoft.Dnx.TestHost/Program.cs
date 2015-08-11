// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Dnx.Runtime;
using Microsoft.Dnx.Runtime.Common.CommandLine;
using Newtonsoft.Json.Linq;

namespace Microsoft.Dnx.TestHost
{
    public class Program
    {
        private readonly IServiceProvider _services;

        public Program(IServiceProvider services)
        {
            _services = services;
        }

        public int Main(string[] args)
        {
            // We want to allow unexpected args, in case future VS needs to pass anything in that we don't current.
            // This will allow us to retain backwards compatibility.
            var application = new CommandLineApplication(throwOnUnexpectedArg: false);
            application.HelpOption("-?|-h|--help");

            var env = (IApplicationEnvironment)_services.GetService(typeof(IApplicationEnvironment));

            var portOption = application.Option("--port", "Port number to listen for a connection.", CommandOptionType.SingleValue);
            var projectOption = application.Option("--project", "Path to a project file.", CommandOptionType.SingleValue);

            var debugOption = application.Option("--debug", "Launch the debugger", CommandOptionType.NoValue);

            var waitOption = application.Option("--wait", "Wait for attach", CommandOptionType.NoValue);

            // If no command was specified at the commandline, then wait for a command via message.
            application.OnExecute(async () =>
            {
                if (debugOption.HasValue())
                {
                    Debugger.Launch();
                }

                if (waitOption.HasValue())
                {
                    Thread.Sleep(10 * 1000);
                }

                var projectPath = projectOption.Value() ?? env.ApplicationBasePath;
                var port = int.Parse(portOption.Value());

                Console.WriteLine("Listening on port {0}", port);
                using (var channel = await ReportingChannel.ListenOn(port))
                {
                    Console.WriteLine("Client accepted {0}", channel.Socket.LocalEndPoint);

                    try
                    {
                        string testCommand = null;
                        Project project = null;
                        if (Project.TryGetProject(projectPath, out project, diagnostics: null))
                        {
                            project.Commands.TryGetValue("test", out testCommand);
                        }

                        if (testCommand == null)
                        {
                            // No test command means no tests.
                            TestHostTracing.Source.TraceEvent(TraceEventType.Error, 0, "Project has no test command.");
                            channel.Send(new Message()
                            {
                                MessageType = "TestExecution.Response",
                            });
                            return -1;
                        }

                        var message = channel.ReadQueue.Take();

                        // The message might be a request to negotiate protocol version. For now we only know
                        // about version 1.
                        if (message.MessageType == "ProtocolVersion")
                        {
                            var version = message.Payload?.ToObject<ProtocolVersionMessage>().Version;
                            var supportedVersion = 1;
                            TestHostTracing.Source.TraceInformation(
                                "[ReportingChannel]: Requested Version: {0} - Using Version: {1}",
                                version,
                                supportedVersion);

                            channel.Send(new Message()
                            {
                                MessageType = "ProtocolVersion",
                                Payload = JToken.FromObject(new ProtocolVersionMessage()
                                {
                                    Version = supportedVersion,
                                }),
                            });

                            // Take the next message, which should be the command to execute.
                            message = channel.ReadQueue.Take();
                        }

                        if (message.MessageType == "TestDiscovery.Start")
                        {
                            TestHostTracing.Source.TraceInformation("Starting Discovery");
                            var commandArgs = new string[]
                            {
                                "--list",
                                "--designtime"
                            };

                            var testServices = TestServices.CreateTestServices(_services, project, channel);
                            await ProjectCommand.Execute(testServices, project, "test", commandArgs);
                            
                            channel.Send(new Message()
                            {
                                MessageType = "TestDiscovery.Response",
                            });

                            TestHostTracing.Source.TraceInformation("Completed Discovery");
                            return 0;
                        }
                        else if (message.MessageType == "TestExecution.Start")
                        {
                            TestHostTracing.Source.TraceInformation("Starting Execution");
                            var commandArgs = new List<string>()
                            {
                                "--designtime"
                            };

                            var tests = message.Payload?.ToObject<RunTestsMessage>().Tests;
                            if (tests != null)
                            {
                                foreach (var test in tests)
                                {
                                    commandArgs.Add("--test");
                                    commandArgs.Add(test);
                                }
                            }

                            var testServices = TestServices.CreateTestServices(_services, project, channel);
                            await ProjectCommand.Execute(testServices, project, "test", commandArgs.ToArray());
                            
                            channel.Send(new Message()
                            {
                                MessageType = "TestExecution.Response",
                            });

                            TestHostTracing.Source.TraceInformation("Completed Execution");
                            return 0;
                        }
                        else
                        {
                            var error = string.Format("Unexpected message type: '{0}'.", message.MessageType);
                            TestHostTracing.Source.TraceEvent(TraceEventType.Error, 0, error);
                            channel.SendError(error);
                            return -1;
                        }
                    }
                    catch (Exception ex)
                    {
                        TestHostTracing.Source.TraceEvent(TraceEventType.Error, 0, ex.ToString());
                        channel.SendError(ex);
                        return -2;
                    }
                }
            });

            return application.Execute(args);
        }
    }
}