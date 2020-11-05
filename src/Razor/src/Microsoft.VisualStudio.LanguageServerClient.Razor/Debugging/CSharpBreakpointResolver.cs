// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor.Debugging
{
    // This type is temporary and will be replaced by an ExternalAccess.Razor type once available.

    internal abstract class CSharpBreakpointResolver
    {
        public abstract bool TryGetBreakpointSpan(SyntaxTree tree, int position, CancellationToken cancellationToken, out TextSpan breakpointSpan);
    }
}
