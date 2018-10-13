// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

// This class is a copy from the Razor repo. All of this data is just metadata related.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.AspNetCore.Razor.OmniSharpPlugin
{
    public class FallbackRazorConfiguration : RazorConfiguration
    {
        public static readonly RazorConfiguration MVC_1_0 = new FallbackRazorConfiguration(
            RazorLanguageVersion.Version_1_0,
            "MVC-1.0",
            new[] { new FallbackRazorExtension("MVC-1.0"), });

        public static readonly RazorConfiguration MVC_1_1 = new FallbackRazorConfiguration(
            RazorLanguageVersion.Version_1_1,
            "MVC-1.1",
            new[] { new FallbackRazorExtension("MVC-1.1"), });

        public static readonly RazorConfiguration MVC_2_0 = new FallbackRazorConfiguration(
             RazorLanguageVersion.Version_2_0,
             "MVC-2.0",
             new[] { new FallbackRazorExtension("MVC-2.0"), });

        public static readonly RazorConfiguration MVC_2_1 = new FallbackRazorConfiguration(
             RazorLanguageVersion.Version_2_1,
             "MVC-2.1",
             new[] { new FallbackRazorExtension("MVC-2.1"), });


        public static RazorConfiguration SelectConfiguration(Version version)
        {
            if (version.Major == 1 && version.Minor == 0)
            {
                return MVC_1_0;
            }
            else if (version.Major == 1 && version.Minor == 1)
            {
                return MVC_1_1;
            }
            else if (version.Major == 2 && version.Minor == 0)
            {
                return MVC_2_0;
            }
            else if (version.Major == 2 && version.Minor == 1)
            {
                return MVC_2_1;
            }
            else
            {
                return MVC_2_1;
            }
        }

        public FallbackRazorConfiguration(
            RazorLanguageVersion languageVersion,
            string configurationName,
            RazorExtension[] extensions)
        {
            if (languageVersion == null)
            {
                throw new ArgumentNullException(nameof(languageVersion));
            }

            if (configurationName == null)
            {
                throw new ArgumentNullException(nameof(configurationName));
            }

            if (extensions == null)
            {
                throw new ArgumentNullException(nameof(extensions));
            }

            LanguageVersion = languageVersion;
            ConfigurationName = configurationName;
            Extensions = extensions;
        }

        public override string ConfigurationName { get; }

        public override IReadOnlyList<RazorExtension> Extensions { get; }

        public override RazorLanguageVersion LanguageVersion { get; }

        private class FallbackRazorExtension : RazorExtension
        {
            public FallbackRazorExtension(string extensionName)
            {
                if (extensionName == null)
                {
                    throw new ArgumentNullException(nameof(extensionName));
                }

                ExtensionName = extensionName;
            }

            public override string ExtensionName { get; }
        }
    }
}
