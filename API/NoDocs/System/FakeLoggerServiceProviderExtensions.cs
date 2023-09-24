// Assembly 'Microsoft.Extensions.Diagnostics.Testing'

using Microsoft.Extensions.Logging.Testing;

namespace System;

public static class FakeLoggerServiceProviderExtensions
{
    public static FakeLogCollector GetFakeLogCollector(this IServiceProvider services);
}
