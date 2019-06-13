// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Testing.xunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Hosting.Systemd;
using Xunit;

namespace Microsoft.Extensions.Hosting
{
    public class UseSystemdTests
    {
        [Fact]
        public void DefaultsToOffOutsideOfService()
        {
            var host = new HostBuilder()
                .UseSystemd()
                .Build();

            using (host)
            {
                var lifetime = host.Services.GetRequiredService<IHostLifetime>();
                Assert.IsType<ConsoleLifetime>(lifetime);
            }
        }

        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Windows)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [MemberData(nameof(SystemdNotifierSendsMessageData))]
        public async Task SystemdNotifierSendsMessage(ServiceState state, string expectedMessage)
        {
            string socketPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            try
            {
                using var notifySocket = new Socket(AddressFamily.Unix, SocketType.Dgram, ProtocolType.Unspecified);
                var endPoint = new UnixDomainSocketEndPoint(socketPath);
                notifySocket.Bind(endPoint);

                var systemdNotifier = new SystemdNotifier(socketPath);
                systemdNotifier.Notify(state);

                byte[] buffer = new byte[256];
                using var cts = new CancellationTokenSource();
                cts.CancelAfter(30000);
                int read = await notifySocket.ReceiveAsync(buffer, SocketFlags.None, cts.Token);
                string receivedMessage = Encoding.UTF8.GetString(buffer, 0, read);
                Assert.Equal(expectedMessage, receivedMessage);
            }
            finally
            {
                try
                {
                    File.Delete(socketPath);
                }
                catch
                {}
            }
        }

        public static TheoryData<ServiceState, string> SystemdNotifierSendsMessageData
        {
            get
            {
                return new TheoryData<ServiceState, string>
                {
                    { ServiceState.Ready, "READY=1" },
                    { ServiceState.Stopping, "STOPPING=1" },
                };
            }
        }

        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Windows)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [InlineData("simple")]
        [InlineData("notify")]
        public void SystemdStartStopWorks(string serviceType)
        {
            using var userService = CreateService($"dotnet-startstop-{serviceType}", serviceType);
            userService.Start();
            userService.Stop();

            var log = userService.GetLog();
            // TODO: Assert some things about log.
        }

        private static UserService CreateService(string name, string type)
            => new UserService(name, type, DotnetPath, IntegrationTestApp);

        private static string DotnetPath => $"/proc/{Process.GetCurrentProcess().Id}/exe";

        private static string IntegrationTestApp
        {
            get
            {
                string currentAssemblyPath = typeof(UseSystemdTests).Assembly.Location;
                string tfmPath = Path.GetDirectoryName(currentAssemblyPath);
                string configurationPath = Path.GetDirectoryName(tfmPath);
                string projectPath = Path.GetDirectoryName(configurationPath);
                string binPath = Path.GetDirectoryName(projectPath);
                return Path.Combine(binPath, "IntegrationTestApp", Path.GetFileName(configurationPath), "netcoreapp3.0", "IntegrationTestApp.dll");
            }
        }

        [Fact]
        public void SystemdProcesses()
        {
            Process[] systemdProcesses = Process.GetProcessesByName("systemd");
            StringBuilder sb = new StringBuilder();
            bool userSession = false;
            foreach(var process in systemdProcesses)
            {
                string cmdLine = File.ReadAllText($"/proc/{process.Id}/cmdline");
                userSession = userSession || cmdLine.Contains("--user");
            }
            Assert.NotEmpty(systemdProcesses);
            Assert.True(userSession);
        }
    }
}
