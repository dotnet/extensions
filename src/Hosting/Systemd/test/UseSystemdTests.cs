// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
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
                int nrBytesReceived = await notifySocket.ReceiveAsync(buffer, SocketFlags.None, cts.Token);
                string receivedMessage = Encoding.UTF8.GetString(buffer, 0, nrBytesReceived);
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
    }
}
