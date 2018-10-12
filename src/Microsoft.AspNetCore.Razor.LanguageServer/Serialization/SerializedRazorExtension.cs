// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

// This class is a copy from the Razor repo.

using System;
using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Serialization
{
    internal class SerializedRazorExtension : RazorExtension
    {
        public SerializedRazorExtension(string extensionName)
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
