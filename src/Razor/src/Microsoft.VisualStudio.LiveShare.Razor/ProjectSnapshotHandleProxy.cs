// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.VisualStudio.LiveShare.Razor
{
    public sealed class ProjectSnapshotHandleProxy
    {
        public ProjectSnapshotHandleProxy(
            Uri filePath,
            IReadOnlyList<TagHelperDescriptor> tagHelpers,
            RazorConfiguration configuration)
        {
            if (filePath == null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            if (tagHelpers == null)
            {
                throw new ArgumentNullException(nameof(tagHelpers));
            }

            FilePath = filePath;
            TagHelpers = tagHelpers;
            Configuration = configuration;
        }

        public RazorConfiguration Configuration { get; }

        public IReadOnlyList<TagHelperDescriptor> TagHelpers { get; }

        public Uri FilePath { get; }
    }
}
