// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    internal static class UnsupportedRazorConfiguration
    {
        public static readonly RazorConfiguration Instance = RazorConfiguration.Create(
            RazorLanguageVersion.Version_1_0,
            "UnsupportedRazor",
            new[] { new UnsupportedRazorExtension("UnsupportedRazorExtension"), });

        private class UnsupportedRazorExtension : RazorExtension
        {
            public UnsupportedRazorExtension(string extensionName)
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
