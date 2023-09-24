// Assembly 'Microsoft.Extensions.Diagnostics.ExceptionSummarization'

using System;
using Microsoft.Extensions.Diagnostics.ExceptionSummarization;

namespace Microsoft.Extensions.DependencyInjection;

public static class ExceptionSummarizationServiceCollectionExtensions
{
    public static IServiceCollection AddExceptionSummarizer(this IServiceCollection services);
    public static IServiceCollection AddExceptionSummarizer(this IServiceCollection services, Action<IExceptionSummarizationBuilder> configure);
}
