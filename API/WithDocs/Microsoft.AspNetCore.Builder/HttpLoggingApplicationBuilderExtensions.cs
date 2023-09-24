// Assembly 'Microsoft.AspNetCore.Diagnostics.Middleware'

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Extensions to attach the HTTP logging middleware.
/// </summary>
public static class HttpLoggingApplicationBuilderExtensions
{
    /// <summary>
    /// Registers incoming HTTP request logging middleware into <see cref="T:Microsoft.AspNetCore.Builder.IApplicationBuilder" />.
    /// </summary>
    /// <remarks>
    /// Request logging middleware should be placed after <see cref="M:Microsoft.AspNetCore.Builder.EndpointRoutingApplicationBuilderExtensions.UseRouting(Microsoft.AspNetCore.Builder.IApplicationBuilder)" /> call.
    /// </remarks>
    /// <param name="builder">An application's request pipeline builder.</param>
    /// <returns>The value of <paramref name="builder" />.</returns>
    /// <exception cref="T:System.ArgumentNullException"><paramref name="builder" /> is <see langword="null" />.</exception>
    public static IApplicationBuilder UseHttpLoggingMiddleware(this IApplicationBuilder builder);
}
