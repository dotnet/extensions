// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.LanguageServer.StrongNamed;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Test
{
    public class TestDocumentSnapshot : DocumentSnapshotShim
    {
        private readonly SourceText _sourceText;
        private readonly TextAndVersion _textVersion;

        public TestDocumentSnapshot(string filePath) : this(filePath, string.Empty)
        {
        }

        public TestDocumentSnapshot(string filePath, string text)
        {
            HostDocument = HostDocumentShim.Create(filePath, filePath);

            _sourceText = SourceText.From(text);
            _textVersion = TextAndVersion.Create(_sourceText, VersionStamp.Default, FilePath);
        }

        public override HostDocumentShim HostDocument { get; }

        public override string FilePath => HostDocument.FilePath;

        public override string TargetPath => HostDocument.TargetPath;

        public override Task<RazorCodeDocument> GetGeneratedOutputAsync()
        {
            throw new NotImplementedException();
        }

        public override IReadOnlyList<DocumentSnapshotShim> GetImports()
        {
            throw new NotImplementedException();
        }

        public override Task<SourceText> GetTextAsync() => Task.FromResult(_sourceText);

        public override Task<VersionStamp> GetTextVersionAsync() => Task.FromResult(_textVersion.Version);

        public override bool TryGetGeneratedOutput(out RazorCodeDocument result)
        {
            throw new NotImplementedException();
        }

        public override bool TryGetText(out SourceText result)
        {
            result = _sourceText;
            return true;
        }

        public override bool TryGetTextVersion(out VersionStamp result)
        {
            result = _textVersion.Version;
            return true;
        }
    }
}
