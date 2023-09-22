// Assembly 'Microsoft.Extensions.Diagnostics.ExceptionSummarization'

using System;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Diagnostics.ExceptionSummarization;

public static class ExceptionSummarizationExtensions
{
    public static IServiceCollection AddExceptionSummarizer(this IServiceCollection services);
    public static IServiceCollection AddExceptionSummarizer(this IServiceCollection services, Action<IExceptionSummarizationBuilder> configure);
    public static IExceptionSummarizationBuilder AddHttpProvider(this IExceptionSummarizationBuilder builder);
}
