// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if !NET7_0_OR_GREATER

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Telemetry.Metering.Internal;

// Uses custom configuration binder - to bind ISet<T> and IReadOnlySet<T> within options classes properly
// Inspired by: https://source.dot.net/#Microsoft.Extensions.Options.ConfigurationExtensions/NamedConfigureFromConfigurationOptions.cs
// This class can be removed once https://github.com/dotnet/runtime/issues/66141 is resolved
internal sealed class CustomConfigureNamedOptions : ConfigureNamedOptions<EventCountersCollectorOptions>
{
    public CustomConfigureNamedOptions(string name, IConfigurationSection section)
        : base(name, options => BindFromOptions(options, section))
    {
    }

    [UnconditionalSuppressMessage("Trimming",
        "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
        Justification = "Original impl")]
    private static void BindFromOptions(EventCountersCollectorOptions options, IConfiguration section)
        => CustomConfigurationBinder.Bind(section, options);
}
#endif
