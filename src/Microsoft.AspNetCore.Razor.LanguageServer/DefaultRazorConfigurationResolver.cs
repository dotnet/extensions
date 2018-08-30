// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;

namespace Microsoft.AspNetCore.Razor.LanguageServer.StrongNamed
{
    internal class DefaultRazorConfigurationResolver : RazorConfigurationResolver
    {
        private static readonly IReadOnlyDictionary<string, RazorConfiguration> SupportedConfigurations = new Dictionary<string, RazorConfiguration>(StringComparer.OrdinalIgnoreCase)
        {
            [FallbackRazorConfiguration.MVC_1_0.ConfigurationName] = FallbackRazorConfiguration.MVC_1_0,
            [FallbackRazorConfiguration.MVC_1_1.ConfigurationName] = FallbackRazorConfiguration.MVC_1_1,
            [FallbackRazorConfiguration.MVC_2_0.ConfigurationName] = FallbackRazorConfiguration.MVC_2_0,
            [FallbackRazorConfiguration.MVC_2_1.ConfigurationName] = FallbackRazorConfiguration.MVC_2_1,
        };

        public override RazorConfiguration Default => FallbackRazorConfiguration.MVC_2_1;

        public override bool TryResolve(string configurationName, out RazorConfiguration configuration)
        {
            if (configurationName == null)
            {
                configuration = Default;
                return true;
            }

            if (SupportedConfigurations.TryGetValue(configurationName, out configuration))
            {
                return true;
            }

            return false;
        }
    }
}
