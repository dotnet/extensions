// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Framework.Runtime;
using Microsoft.Framework.Runtime.Common.CommandLine;
using Microsoft.Framework.Runtime.Common.DependencyInjection;
using Microsoft.Framework.TestAdapter;

namespace Microsoft.AspNet.TestHost
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
            var application = new CommandLineApplication();
            application.HelpOption("-?|-h|--help");

            var env = (IApplicationEnvironment)_services.GetService(typeof(IApplicationEnvironment));

            var portOption = application.Option("--port", "Port number to listen for a connection.", CommandOptionType.SingleValue);
            var projectOption = application.Option("--project", "Path to a project file.", CommandOptionType.SingleValue);

            var projectPath = projectOption.Value() ?? env.ApplicationBasePath;

            application.Command("list", command =>
            {
                command.Name = "list";
                command.Description = "Lists all available tests.";

                command.OnExecute(async () =>
                {
                    await DiscoverTests(int.Parse(portOption.Value()), projectPath);
                    return 0;
                });
            });

            application.Command("run", command =>
            {
                command.Name = "run";
                command.Description = "Runs specified tests.";

                var tests = command.Option("--test <test>", "test to run", CommandOptionType.MultipleValue);

                command.OnExecute(async () =>
                {
                    await ExecuteTests(int.Parse(portOption.Value()), projectPath, tests.Values);
                    return 0;
                });

            });


            var result = application.Execute(args);

            if (!application.IsShowingInformation && !application.RemainingArguments.Any())
            {
                application.ShowHelp();
            }

            return result;
        }

        private async Task ExecuteTests(int port, string projectPath, IList<string> tests)
        {
            Console.WriteLine("Listening on port {0}", port);
            using (var channel = await ReportingChannel.ListenOn(port))
            {
                Console.WriteLine("Client accepted {0}", channel.Socket.LocalEndPoint);

                string testCommand = null;
                Project project = null;
                if (Project.TryGetProject(projectPath, out project))
                {
                    project.Commands.TryGetValue("test", out testCommand);
                }

                if (testCommand == null)
                {
                    // No test command means no tests.
                    Trace.TraceInformation("[ReportingChannel]: OnTransmit(ExecuteTests)");
                    channel.Send(new Message()
                    {
                        MessageType = "TestExecution.Response",
                    });

                    return;
                }

                var testServices = new ServiceProvider(_services);
                testServices.Add(typeof(ITestExecutionSink), new TestExecutionSink(channel));

                var args = new List<string>()
                {
                    "test",
                    "--designtime"
                };

                if (tests != null)
                {
                    foreach (var test in tests)
                    {
                        args.Add("--test");
                        args.Add(test);
                    }
                }

                try
                {
                    await ProjectCommand.Execute(testServices, project, args.ToArray());
                }
                catch
                {
                    // For now we're not doing anything with these exceptions, we might want to report them
                    // to VS.   
                }

                Trace.TraceInformation("[ReportingChannel]: OnTransmit(ExecuteTests)");
                channel.Send(new Message()
                {
                    MessageType = "TestExecution.Response",
                });
            }
        }

        private async Task DiscoverTests(int port, string projectPath)
        {
            Console.WriteLine("Listening on port {0}", port);
            using (var channel = await ReportingChannel.ListenOn(port))
            {
                Console.WriteLine("Client accepted {0}", channel.Socket.LocalEndPoint);

                string testCommand = null;
                Project project = null;
                if (Project.TryGetProject(projectPath, out project))
                {
                    project.Commands.TryGetValue("test", out testCommand);
                }

                if (testCommand == null)
                {
                    // No test command means no tests.
                    Trace.TraceInformation("[ReportingChannel]: OnTransmit(DiscoverTests)");
                    channel.Send(new Message()
                    {
                        MessageType = "TestDiscovery.Response",
                    });

                    return;
                }

                var testServices = new ServiceProvider(_services);
                testServices.Add(typeof(ITestDiscoverySink), new TestDiscoverySink(channel));

                var args = new string[] { "test", "--list", "--designtime" };

                try
                {
                    await ProjectCommand.Execute(testServices, project, args);
                }
                catch
                {
                    // For now we're not doing anything with these exceptions, we might want to report them
                    // to VS.   
                }

                Trace.TraceInformation("[ReportingChannel]: OnTransmit(DiscoverTests)");
                channel.Send(new Message()
                {
                    MessageType = "TestDiscovery.Response",
                });
            }
        }
    }
}