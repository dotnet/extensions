// Assembly 'Microsoft.Extensions.Diagnostics.ExceptionSummarization'

using System;

namespace Microsoft.Extensions.Diagnostics.ExceptionSummarization;

public interface IExceptionSummarizer
{
    ExceptionSummary Summarize(Exception exception);
}
