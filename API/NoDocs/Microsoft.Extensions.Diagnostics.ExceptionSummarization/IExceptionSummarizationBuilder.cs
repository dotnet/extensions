// Assembly 'Microsoft.Extensions.Diagnostics.ExceptionSummarization'

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Diagnostics.ExceptionSummarization;

public interface IExceptionSummarizationBuilder
{
    IServiceCollection Services { get; }
    IExceptionSummarizationBuilder AddProvider<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>() where T : class, IExceptionSummaryProvider;
}
