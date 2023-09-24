// Assembly 'Microsoft.Extensions.Compliance.Redaction'

using System;
using Microsoft.Extensions.Compliance.Redaction;

namespace Microsoft.Extensions.DependencyInjection;

public static class RedactionServiceCollectionExtensions
{
    public static IServiceCollection AddRedaction(this IServiceCollection services);
    public static IServiceCollection AddRedaction(this IServiceCollection services, Action<IRedactionBuilder> configure);
}
