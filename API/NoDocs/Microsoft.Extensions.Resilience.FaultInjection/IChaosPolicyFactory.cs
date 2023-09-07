// Assembly 'Microsoft.Extensions.Resilience'

using System.Diagnostics.CodeAnalysis;
using Polly.Contrib.Simmy.Latency;
using Polly.Contrib.Simmy.Outcomes;

namespace Microsoft.Extensions.Resilience.FaultInjection;

public interface IChaosPolicyFactory
{
    AsyncInjectLatencyPolicy<TResult> CreateLatencyPolicy<TResult>();
    AsyncInjectOutcomePolicy CreateExceptionPolicy();
    [Experimental("EXTEXP0001", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
    AsyncInjectOutcomePolicy<TResult> CreateCustomResultPolicy<TResult>();
}
