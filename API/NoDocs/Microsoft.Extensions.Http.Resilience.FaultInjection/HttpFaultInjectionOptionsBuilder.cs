// Assembly 'Microsoft.Extensions.Http.Resilience'

using System;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Resilience.FaultInjection;

namespace Microsoft.Extensions.Http.Resilience.FaultInjection;

public class HttpFaultInjectionOptionsBuilder
{
    public HttpFaultInjectionOptionsBuilder(IServiceCollection services);
    public HttpFaultInjectionOptionsBuilder Configure();
    public HttpFaultInjectionOptionsBuilder Configure(IConfiguration section);
    public HttpFaultInjectionOptionsBuilder Configure(Action<FaultInjectionOptions> configureOptions);
    public HttpFaultInjectionOptionsBuilder AddException(string key, Exception exception);
    public HttpFaultInjectionOptionsBuilder AddHttpContent(string key, HttpContent content);
}
