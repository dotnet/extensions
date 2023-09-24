// Assembly 'Microsoft.Extensions.Compliance.Testing'

using Microsoft.Extensions.Compliance.Testing;

namespace System;

/// <summary>
/// Extensions that allow registering a fake redactor in the application.
/// </summary>
public static class FakeRedactionServiceProviderExtensions
{
    /// <summary>
    /// Gets the fake redactor collector instance from the dependency injection container.
    /// </summary>
    /// <param name="serviceProvider">The container used to obtain the collector instance.</param>
    /// <returns>The obtained collector.</returns>
    /// <exception cref="T:System.InvalidOperationException">The collector is not in the container.</exception>
    /// <exception cref="T:System.ArgumentNullException"><paramref name="serviceProvider" /> is <see langword="null" />.</exception>
    /// <remarks>
    /// <see cref="T:Microsoft.Extensions.Compliance.Testing.FakeRedactionCollector" /> should be registered and used only with fake redaction implementation.
    /// </remarks>
    public static FakeRedactionCollector GetFakeRedactionCollector(this IServiceProvider serviceProvider);
}
