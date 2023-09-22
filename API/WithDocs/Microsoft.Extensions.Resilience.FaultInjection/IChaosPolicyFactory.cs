// Assembly 'Microsoft.Extensions.Resilience'

using System.Diagnostics.CodeAnalysis;
using Polly.Contrib.Simmy.Latency;
using Polly.Contrib.Simmy.Outcomes;

namespace Microsoft.Extensions.Resilience.FaultInjection;

/// <summary>
/// Factory for chaos policy creation.
/// </summary>
public interface IChaosPolicyFactory
{
    /// <summary>
    /// Creates an async latency policy with delegate functions to fetch fault injection
    /// settings from <see cref="T:Polly.Context" />.
    /// </summary>
    /// <typeparam name="TResult">The type of value policies created by this method will inject.</typeparam>
    /// <returns>
    /// A latency policy.
    /// </returns>
    AsyncInjectLatencyPolicy<TResult> CreateLatencyPolicy<TResult>();

    /// <summary>
    /// Creates an async exception policy with delegate functions to fetch
    /// fault injection settings from <see cref="T:Polly.Context" />.
    /// </summary>
    /// <returns>
    /// An exception policy.
    /// </returns>
    AsyncInjectOutcomePolicy CreateExceptionPolicy();

    /// <summary>
    /// Creates an async custom result policy with delegate functions to fetch
    /// fault injection settings from <see cref="T:Polly.Context" />.
    /// </summary>
    /// <typeparam name="TResult">The type of value policies created by this method will inject.</typeparam>
    /// <returns>A custom result policy.</returns>
    [Experimental("EXTEXP0001", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
    AsyncInjectOutcomePolicy<TResult> CreateCustomResultPolicy<TResult>();
}
