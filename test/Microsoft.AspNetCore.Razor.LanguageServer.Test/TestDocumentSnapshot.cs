// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Test
{
    internal class TestDocumentSnapshot : DefaultDocumentSnapshot
    {
        public static TestDocumentSnapshot Create(string filePath) => Create(filePath, string.Empty);

        public static TestDocumentSnapshot Create(string filePath, string text)
        {
            var testProject = TestProjectSnapshot.Create(filePath + ".csproj");
            var testWorkspace = TestWorkspace.Create();
            var hostDocument = new HostDocument(filePath, filePath);
            var sourceText = SourceText.From(text);
            var version = VersionStamp.Default;
            var documentState = new DocumentState(
                testWorkspace.Services, 
                hostDocument, 
                SourceText.From(text), 
                version, 
                () => Task.FromResult(TextAndVersion.Create(sourceText, version)));
            var testDocument = new TestDocumentSnapshot(testProject, documentState);

            return testDocument;
        }

        private TestDocumentSnapshot(DefaultProjectSnapshot projectSnapshot, DocumentState documentState)
            : base(projectSnapshot, documentState)
        {
        }

        public override Task<RazorCodeDocument> GetGeneratedOutputAsync()
        {
            throw new NotImplementedException();
        }

        public override IReadOnlyList<DocumentSnapshot> GetImports()
        {
            throw new NotImplementedException();
        }

        public override bool TryGetGeneratedOutput(out RazorCodeDocument result)
        {
            throw new NotImplementedException();
        }
    }
}
