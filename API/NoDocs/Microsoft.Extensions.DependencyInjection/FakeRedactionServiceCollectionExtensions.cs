// Assembly 'Microsoft.Extensions.Compliance.Testing'

using System;
using Microsoft.Extensions.Compliance.Testing;

namespace Microsoft.Extensions.DependencyInjection;

public static class FakeRedactionServiceCollectionExtensions
{
    public static IServiceCollection AddFakeRedaction(this IServiceCollection services);
    public static IServiceCollection AddFakeRedaction(this IServiceCollection services, Action<FakeRedactorOptions> configure);
}
