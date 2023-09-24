// Assembly 'Microsoft.Extensions.Diagnostics.Testing'

using Microsoft.Extensions.Logging.Testing;

namespace System;

/// <summary>
/// Extensions for configuring fake logging, used in unit tests.
/// </summary>
public static class FakeLoggerServiceProviderExtensions
{
    /// <summary>
    /// Gets the object that collects log records sent to the fake logger.
    /// </summary>
    /// <param name="services">The service provider containing the logger.</param>
    /// <exception cref="T:System.InvalidOperationException">No collector exists in the provider.</exception>
    /// <returns>The collector which tracks records logged to fake loggers.</returns>
    public static FakeLogCollector GetFakeLogCollector(this IServiceProvider services);
}
