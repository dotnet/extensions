// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Experiment;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.AspNetCore.Razor.LanguageServer.StrongNamed
{
    public sealed class GeneratedCodeContainerShim
    {
        internal GeneratedCodeContainerShim(GeneratedCodeContainer generatedCodeContainer)
        {
            InnerGeneratedCodeContainer = generatedCodeContainer;
        }

        public SourceText Source { get; private set; }

        public VersionStamp SourceVersion { get; private set; }

        public RazorCSharpDocument Output { get; private set; }

        public SourceTextContainer SourceTextContainer => InnerGeneratedCodeContainer.SourceTextContainer;

        internal GeneratedCodeContainer InnerGeneratedCodeContainer { get; }

        public TService GetService<TService>() => InnerGeneratedCodeContainer.GetService<TService>();

        public void SetOutput(SourceText source, RazorCodeDocument codeDocument) => InnerGeneratedCodeContainer.SetOutput(source, codeDocument);

        public Task<ImmutableArray<SpanMapResult>> MapSpansAsync(
                Document document,
                IEnumerable<TextSpan> spans,
                CancellationToken cancellationToken) => InnerGeneratedCodeContainer.MapSpansAsync(document, spans, cancellationToken);
    }
}
