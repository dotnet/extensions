// Assembly 'Microsoft.Extensions.Diagnostics.ExceptionSummarization'

namespace Microsoft.Extensions.Diagnostics.ExceptionSummarization;

/// <summary>
/// Controls exception summarization.
/// </summary>
public static class ExceptionSummarizationBuilderExtensions
{
    /// <summary>
    /// Registers a summary provider that handles <see cref="T:System.OperationCanceledException" />, <see cref="T:System.Net.WebException" />, and <see cref="T:System.Net.Sockets.SocketException" /> .
    /// </summary>
    /// <param name="builder">The builder to attach the provider to.</param>
    /// <returns>The value of <paramref name="builder" />.</returns>
    /// <exception cref="T:System.ArgumentNullException"><paramref name="builder" /> is <see langword="null" />.</exception>
    public static IExceptionSummarizationBuilder AddHttpProvider(this IExceptionSummarizationBuilder builder);
}
