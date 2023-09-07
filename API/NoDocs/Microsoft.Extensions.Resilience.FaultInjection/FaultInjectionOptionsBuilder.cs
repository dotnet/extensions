// Assembly 'Microsoft.Extensions.Resilience'

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Resilience.FaultInjection;

public class FaultInjectionOptionsBuilder
{
    public FaultInjectionOptionsBuilder(IServiceCollection services);
    public FaultInjectionOptionsBuilder Configure();
    public FaultInjectionOptionsBuilder Configure(IConfiguration section);
    public FaultInjectionOptionsBuilder Configure(Action<FaultInjectionOptions> configureOptions);
    public FaultInjectionOptionsBuilder AddException(string key, Exception exception);
    [Experimental("EXTEXP0001", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
    public FaultInjectionOptionsBuilder AddCustomResult(string key, object customResult);
}
