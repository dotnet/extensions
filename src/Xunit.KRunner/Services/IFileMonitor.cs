using System;

namespace Microsoft.Net.Runtime
{
    [AssemblyNeutral]
    public interface IFileMonitor
    {
        event Action<string> OnChanged;
    }
}
