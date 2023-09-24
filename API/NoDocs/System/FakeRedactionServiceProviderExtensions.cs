// Assembly 'Microsoft.Extensions.Compliance.Testing'

using Microsoft.Extensions.Compliance.Testing;

namespace System;

public static class FakeRedactionServiceProviderExtensions
{
    public static FakeRedactionCollector GetFakeRedactionCollector(this IServiceProvider serviceProvider);
}
