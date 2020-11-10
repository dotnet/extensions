// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Razor.Workspaces;

namespace Microsoft.AspNetCore.Razor.LanguageServer
{
    internal class DefaultLanguageServerFeatureOptions : LanguageServerFeatureOptions
    {
        public override bool SupportsFileManipulation { get; } = true;
    }
}
