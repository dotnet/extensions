// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Extensions.Logging
{
    public static partial class AzureEventHubFactoryExtensions
    {
        public static Microsoft.Extensions.Logging.ILoggingBuilder AddAzureEventHubs(this Microsoft.Extensions.Logging.ILoggingBuilder builder) { throw null; }
        public static Microsoft.Extensions.Logging.ILoggingBuilder AddAzureEventHubs(this Microsoft.Extensions.Logging.ILoggingBuilder builder, System.Action<Microsoft.Extensions.Logging.AzureEventHubs.AzureEventHubsLoggerOptions> configure) { throw null; }
    }
}
namespace Microsoft.Extensions.Logging.AzureEventHubs
{
    public partial class AzureEventHubsLoggerOptions
    {
        public AzureEventHubsLoggerOptions() { }
        public string ConnectionString { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public bool IncludeScopes { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string Instance { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public string Namespace { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    [Microsoft.Extensions.Logging.ProviderAliasAttribute("AzureEventHubs")]
    public partial class AzureEventHubsLoggerProvider : Microsoft.Extensions.Logging.ILoggerProvider, System.IDisposable
    {
        public AzureEventHubsLoggerProvider(Microsoft.Extensions.Options.IOptionsMonitor<Microsoft.Extensions.Logging.AzureEventHubs.AzureEventHubsLoggerOptions> options, Microsoft.Extensions.Logging.AzureEventHubs.IAzureEventHubsLoggerFormatter formatter, Microsoft.Extensions.Logging.AzureEventHubs.IAzureEventHubsLoggerProcessor processor) { }
        public Microsoft.Extensions.Logging.ILogger CreateLogger(string name) { throw null; }
        public void Dispose() { }
        public void SetScopeProvider(Microsoft.Extensions.Logging.IExternalScopeProvider scopeProvider) { }
    }
    public partial interface IAzureEventHubsLoggerFormatter
    {
        Microsoft.Azure.EventHubs.EventData Format<TState>(Microsoft.Extensions.Logging.LogLevel logLevel, string name, Microsoft.Extensions.Logging.EventId eventId, TState state, System.Exception exception, System.Func<TState, System.Exception, string> formatter, Microsoft.Extensions.Logging.IExternalScopeProvider scopeProvider);
    }
    public partial interface IAzureEventHubsLoggerProcessor : System.IDisposable
    {
        void Process(Microsoft.Azure.EventHubs.EventData eventData);
    }
}
