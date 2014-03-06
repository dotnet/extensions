using System;

namespace Microsoft.Net.Runtime
{
    [NotAssemblyNeutral]
    [AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = true)]
    public sealed class NotAssemblyNeutralAttribute : Attribute
    {
    }
}