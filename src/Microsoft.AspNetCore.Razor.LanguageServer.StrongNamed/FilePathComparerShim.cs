// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Razor;

namespace Microsoft.AspNetCore.Razor.LanguageServer.StrongNamed
{
    public static class FilePathComparerShim
    {
        public static StringComparer Instance => FilePathComparer.Instance;
    }
}
