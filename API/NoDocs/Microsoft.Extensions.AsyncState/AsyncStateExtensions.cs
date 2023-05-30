// Assembly 'Microsoft.Extensions.AsyncState'

using System;
using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.AsyncState;

public static class AsyncStateExtensions
{
    public static IServiceCollection AddAsyncStateCore(this IServiceCollection services);
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static IServiceCollection TryRemoveAsyncStateCore(this IServiceCollection services);
}
