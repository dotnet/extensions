// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis;

namespace Microsoft.VisualStudio.LiveShare.Razor.Test
{
    internal static class TestProjectSnapshotHandleProxy
    {
        public static ProjectSnapshotHandleProxy Create(Uri filePath) => new ProjectSnapshotHandleProxy(filePath, Array.Empty<TagHelperDescriptor>(), RazorConfiguration.Default);
    }
}
