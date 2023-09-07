// Assembly 'Microsoft.Extensions.Diagnostics.ResourceMonitoring'

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Microsoft.Shared.Data.Validation;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring;

[Experimental("EXTEXP0008", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
public class WindowsCountersOptions
{
    [Required]
    public ISet<string> InstanceIpAddresses { get; set; }
    [TimeSpan(100, 900000)]
    public TimeSpan CachingInterval { get; set; }
    public WindowsCountersOptions();
}
