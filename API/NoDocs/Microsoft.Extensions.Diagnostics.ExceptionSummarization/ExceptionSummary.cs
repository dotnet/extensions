// Assembly 'Microsoft.Extensions.Diagnostics.ExceptionSummarization'

using System;
using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Diagnostics.ExceptionSummarization;

public readonly struct ExceptionSummary : IEquatable<ExceptionSummary>
{
    public string ExceptionType { get; }
    public string Description { get; }
    public string AdditionalDetails { get; }
    public ExceptionSummary(string exceptionType, string description, string additionalDetails);
    public override int GetHashCode();
    public override string ToString();
    public override bool Equals(object? obj);
    public bool Equals(ExceptionSummary other);
    public static bool operator ==(ExceptionSummary left, ExceptionSummary right);
    public static bool operator !=(ExceptionSummary left, ExceptionSummary right);
}
