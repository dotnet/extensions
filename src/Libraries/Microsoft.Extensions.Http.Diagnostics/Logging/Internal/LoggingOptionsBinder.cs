// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Http.Logging.Internal;

internal static class LoggingOptionsBinder
{
    /// <summary>
    /// Binds <see cref="LoggingOptions"/> to a configuration section.
    /// </summary>
    /// <param name="builder">The options builder.</param>
    /// <param name="section">The configuration section to bind to.</param>
    /// <returns>The value of <paramref name="builder"/>.</returns>
    /// <remarks>
    /// A dedicated configurator is used because the configuration binding source generator doesn't honor the
    /// type converter used by <see cref="Compliance.Classification.DataClassification"/>.
    /// </remarks>
    public static OptionsBuilder<LoggingOptions> BindConfigurationSection(this OptionsBuilder<LoggingOptions> builder, IConfigurationSection section)
    {
        _ = builder.Services
            .AddSingleton<IConfigureOptions<LoggingOptions>>(
                new LoggingOptionsConfigureOptions(builder.Name, section))
            .AddSingleton<IOptionsChangeTokenSource<LoggingOptions>>(
                new ConfigurationChangeTokenSource<LoggingOptions>(builder.Name, section));

        return builder;
    }
}
