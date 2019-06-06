// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Extensions.Hosting
{
    public static partial class SystemdHostBuilderExtensions
    {
        public static Microsoft.Extensions.Hosting.IHostBuilder UseSystemd(this Microsoft.Extensions.Hosting.IHostBuilder hostBuilder) { throw null; }
    }
}
namespace Microsoft.Extensions.Hosting.Systemd
{
    public partial interface ISystemdNotifier
    {
        bool IsEnabled { get; }
        void Notify(Microsoft.Extensions.Hosting.Systemd.ServiceState state, params Microsoft.Extensions.Hosting.Systemd.ServiceState[] states);
    }
    public static partial class ServiceHelpers
    {
        public static bool IsSystemd() { throw null; }
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct ServiceState
    {
        private object _dummy;
        public ServiceState(string state) { throw null; }
        public static Microsoft.Extensions.Hosting.Systemd.ServiceState Ready { get { throw null; } }
        public static Microsoft.Extensions.Hosting.Systemd.ServiceState Reloading { get { throw null; } }
        public static Microsoft.Extensions.Hosting.Systemd.ServiceState Stopping { get { throw null; } }
        public static Microsoft.Extensions.Hosting.Systemd.ServiceState Watchdog { get { throw null; } }
        public static Microsoft.Extensions.Hosting.Systemd.ServiceState BusError(string value) { throw null; }
        public static Microsoft.Extensions.Hosting.Systemd.ServiceState Errno(int value) { throw null; }
        public static Microsoft.Extensions.Hosting.Systemd.ServiceState MainPid(int value) { throw null; }
        public static Microsoft.Extensions.Hosting.Systemd.ServiceState Status(string value) { throw null; }
        public override string ToString() { throw null; }
    }
    public partial class SystemdNotifier : Microsoft.Extensions.Hosting.Systemd.ISystemdNotifier
    {
        public SystemdNotifier() { }
        public bool IsEnabled { get { throw null; } }
        public void Notify(Microsoft.Extensions.Hosting.Systemd.ServiceState state, Microsoft.Extensions.Hosting.Systemd.ServiceState[] states) { }
    }
    public partial class SystemdLifetime : Microsoft.Extensions.Hosting.IHostLifetime, System.IDisposable
    {
        public SystemdLifetime(Microsoft.Extensions.Hosting.IHostEnvironment environment, Microsoft.Extensions.Hosting.IHostApplicationLifetime applicationLifetime, Microsoft.Extensions.Hosting.Systemd.ISystemdNotifier systemdNotifier, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory) { }
        public void Dispose() { }
        public System.Threading.Tasks.Task StopAsync(System.Threading.CancellationToken cancellationToken) { throw null; }
        public System.Threading.Tasks.Task WaitForStartAsync(System.Threading.CancellationToken cancellationToken) { throw null; }
    }
}
