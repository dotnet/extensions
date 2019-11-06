// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Extensions.Hosting
{
    public static partial class WindowsServiceLifetimeHostBuilderExtensions
    {
        public static Microsoft.Extensions.Hosting.IHostBuilder UseWindowsService(this Microsoft.Extensions.Hosting.IHostBuilder hostBuilder) { throw null; }
    }
}
namespace Microsoft.Extensions.Hosting.WindowsServices
{
    public static partial class WindowsServiceHelpers
    {
        public static bool IsWindowsService() { throw null; }
    }
    public partial class WindowsServiceLifetime : System.ServiceProcess.ServiceBase, Microsoft.Extensions.Hosting.IHostLifetime
    {
        public WindowsServiceLifetime(Microsoft.Extensions.Hosting.IHostEnvironment environment, Microsoft.Extensions.Hosting.IHostApplicationLifetime applicationLifetime, Microsoft.Extensions.Logging.ILoggerFactory loggerFactory, Microsoft.Extensions.Options.IOptions<Microsoft.Extensions.Hosting.HostOptions> optionsAccessor) { }
        protected override void Dispose(bool disposing) { }
        protected override void OnStart(string[] args) { }
        protected override void OnStop() { }
        public System.Threading.Tasks.Task StopAsync(System.Threading.CancellationToken cancellationToken) { throw null; }
        public System.Threading.Tasks.Task WaitForStartAsync(System.Threading.CancellationToken cancellationToken) { throw null; }
    }
}
