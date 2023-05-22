// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;

namespace Microsoft.Extensions.ExtraAnalyzers.FixAllProviders;

public interface ISequentialFixer
{
    public SyntaxNode GetFixableSyntaxNodeFromDiagnostic(SyntaxNode documentRoot, Diagnostic diagnostic);
    public SyntaxNode ApplyDiagnosticFixToSyntaxNode(SyntaxNode nodeToFix, Diagnostic diagnostic);
}
