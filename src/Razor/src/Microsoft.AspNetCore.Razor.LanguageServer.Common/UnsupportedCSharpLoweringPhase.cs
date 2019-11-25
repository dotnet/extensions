// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Common
{
    internal class UnsupportedCSharpLoweringPhase : RazorEnginePhaseBase, IRazorCSharpLoweringPhase
    {
        internal const string UnsupportedDisclaimer = "// Razor CSharp output is not supported for this project's version of Razor.";

        protected override void ExecuteCore(RazorCodeDocument codeDocument)
        {
            var documentNode = codeDocument.GetDocumentIntermediateNode();
            ThrowForMissingDocumentDependency(documentNode);

            var cSharpDocument = RazorCSharpDocument.Create(
                UnsupportedDisclaimer,
                documentNode.Options,
                Enumerable.Empty<RazorDiagnostic>());
            codeDocument.SetCSharpDocument(cSharpDocument);
            codeDocument.SetUnsupported();
        }
    }
}
