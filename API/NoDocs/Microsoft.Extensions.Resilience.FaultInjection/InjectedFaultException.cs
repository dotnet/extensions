// Assembly 'Microsoft.Extensions.Resilience'

using System;

namespace Microsoft.Extensions.Resilience.FaultInjection;

public class InjectedFaultException : Exception
{
    public InjectedFaultException();
    public InjectedFaultException(string message);
    public InjectedFaultException(string message, Exception innerException);
}
