// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Extensions.Hosting
{
    public static partial class SystemdServiceHostBuilderExtensions
    {
        public static Microsoft.Extensions.Hosting.IHostBuilder UseSystemdService(this Microsoft.Extensions.Hosting.IHostBuilder hostBuilder) { throw null; }
    }
}
namespace Microsoft.Extensions.Hosting.SystemdServices
{
    public partial interface ISystemdNotifier
    {
        bool IsEnabled { get; }
        void Notify(Microsoft.Extensions.Hosting.SystemdServices.ServiceState state, params Microsoft.Extensions.Hosting.SystemdServices.ServiceState[] states);
    }
    public static partial class ServiceHelpers
    {
        public static bool IsSystemdService() { throw null; }
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct ServiceState
    {
        private object _dummy;
        public ServiceState(string state) { throw null; }
        public static Microsoft.Extensions.Hosting.SystemdServices.ServiceState Ready { get { throw null; } }
        public static Microsoft.Extensions.Hosting.SystemdServices.ServiceState Reloading { get { throw null; } }
        public static Microsoft.Extensions.Hosting.SystemdServices.ServiceState Stopping { get { throw null; } }
        public static Microsoft.Extensions.Hosting.SystemdServices.ServiceState Watchdog { get { throw null; } }
        public static Microsoft.Extensions.Hosting.SystemdServices.ServiceState BusError(string value) { throw null; }
        public static Microsoft.Extensions.Hosting.SystemdServices.ServiceState Errno(int value) { throw null; }
        public static Microsoft.Extensions.Hosting.SystemdServices.ServiceState MainPid(int value) { throw null; }
        public static Microsoft.Extensions.Hosting.SystemdServices.ServiceState Status(string value) { throw null; }
        public override string ToString() { throw null; }
    }
    public partial class SystemdNotifier : Microsoft.Extensions.Hosting.SystemdServices.ISystemdNotifier
    {
        public SystemdNotifier() { }
        public bool IsEnabled { get { throw null; } }
        public void Notify(Microsoft.Extensions.Hosting.SystemdServices.ServiceState state, Microsoft.Extensions.Hosting.SystemdServices.ServiceState[] states) { }
    }
    public partial class SystemdServiceLifetime : Microsoft.Extensions.Hosting.IHostLifetime, System.IDisposable
    {
        public SystemdServiceLifetime(Microsoft.Extensions.Hosting.IHostEnvironment environment, Microsoft.Extensions.Hosting.IHostApplicationLifetime applicationLifetime, Microsoft.Extensions.Hosting.SystemdServices.ISystemdNotifier systemdNotifier, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory) { }
        public void Dispose() { }
        public System.Threading.Tasks.Task StopAsync(System.Threading.CancellationToken cancellationToken) { throw null; }
        public System.Threading.Tasks.Task WaitForStartAsync(System.Threading.CancellationToken cancellationToken) { throw null; }
    }
}
