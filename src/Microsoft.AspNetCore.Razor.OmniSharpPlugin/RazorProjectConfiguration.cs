// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.AspNetCore.Razor.OmnisharpPlugin
{
    internal class RazorProjectConfiguration
    {
        public string ProjectFilePath { get; set; }

        public IReadOnlyList<TagHelperDescriptor> TagHelpers { get; set; } = Array.Empty<TagHelperDescriptor>();

        public RazorConfiguration Configuration { get; set; }

        // TODO: Include Razor document inputs
    }
}
