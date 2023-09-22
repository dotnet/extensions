// Assembly 'Microsoft.Extensions.Diagnostics.ExceptionSummarization'

using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.Diagnostics.ExceptionSummarization;

public interface IExceptionSummaryProvider
{
    IEnumerable<Type> SupportedExceptionTypes { get; }
    IReadOnlyList<string> Descriptions { get; }
    int Describe(Exception exception, out string? additionalDetails);
}
