// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Razor.LanguageServer.Common
{
    public static class LanguageServerConstants
    {
        public const string ProjectConfigurationFile = "project.razor.json";

        // Semantic "Legacy" endpoints refer to an old LSP spec version, needed for now until VS reacts.
        public const string LegacyRazorSemanticTokensEndpoint = "textDocument/semanticTokens";

        public const string LegacyRazorSemanticTokensEditEndpoint = "textDocument/semanticTokens/edits";

        public const string LegacyRazorSemanticTokensRangeEndpoint = "textDocument/semanticTokens/range";

        public const string RazorSemanticTokensLegendEndpoint = "_ms_/textDocument/semanticTokensLegend";

        public const string RazorSemanticTokensEditEndpoint = "textDocument/semanticTokens/full/delta";

        public const string RazorSemanticTokensEndpoint = "textDocument/semanticTokens/full";

        public const string SemanticTokensProviderName = "semanticTokensProvider";

        public const string RazorLanguageQueryEndpoint = "razor/languageQuery";

        public const string RazorMonitorProjectConfigurationFilePathEndpoint = "razor/monitorProjectConfigurationFilePath";

        public const string RazorMapToDocumentRangesEndpoint = "razor/mapToDocumentRanges";

        public const string RazorMapToDocumentEditsEndpoint = "razor/mapToDocumentEdits";

        public const string RazorCodeActionRunnerCommand = "razor/runCodeAction";

        public const string RazorCodeActionResolveEndpoint = "textDocument/codeActionResolve";

        // RZLS Custom Message Targets
        public const string RazorUpdateCSharpBufferEndpoint = "razor/updateCSharpBuffer";

        public const string RazorUpdateHtmlBufferEndpoint = "razor/updateHtmlBuffer";

        public const string RazorRangeFormattingEndpoint = "razor/rangeFormatting";

        public const string RazorProvideCodeActionsEndpoint = "razor/provideCodeActions";

        public const string RazorResolveCodeActionsEndpoint = "razor/resolveCodeActions";

        public const string RazorProvideSemanticTokensEndpoint = "razor/provideSemanticTokens";

        public static class CodeActions
        {
            public const string ExtractToCodeBehindAction = "ExtractToCodeBehind";

            public const string CreateComponentFromTag = "CreateComponentFromTag";

            public const string AddUsing = "AddUsing";

            public const string Default = "Default";

            public static class Languages
            {
                public const string CSharp = "CSharp";

                public const string Razor = "Razor";
            }
        }
    }
}
