// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Diagnostics.ResourceMonitoring.Linux;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Linux;

#if NET7_0_OR_GREATER
[SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "Nah")]
[ExcludeFromCodeCoverage(Justification = "This is just a call to a native method. We access it by the interface at runtime. It is not testable.")]
internal static partial class NativeMethods
{
    [LibraryImport("libc", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    public static partial long sysconf(int name);
}
#endif

[ExcludeFromCodeCoverage] // Justification: This is just a call to a native method. We access it by the interface at runtime. It is not testable.
internal sealed class UserHz : IUserHz
{
    private const int SystemConfigurationUserHz = 2;

    public UserHz()
    {
        Value = NativeMethods.sysconf(SystemConfigurationUserHz);
    }

    public long Value { get; }

#if !NET7_0_OR_GREATER
    private static class NativeMethods
    {
        [DllImport("libc", SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        public static extern long sysconf(int name);
    }
#endif
}
