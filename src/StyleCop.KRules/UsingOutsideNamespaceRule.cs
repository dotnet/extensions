// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using StyleCop.CSharp;

namespace StyleCop.KRules
{
    [SourceAnalyzer(typeof(CsParser))]
    public class UsingOutsideNamespaceRule : SourceAnalyzer
    {
        private const string RuleName = "UsingDirectivesMustBePlacedOutsideNamespace";

        public override void AnalyzeDocument(CodeDocument document)
        {
            var csharpDocument = (CsDocument)document;
            if (csharpDocument.RootElement != null && !csharpDocument.RootElement.Generated)
            {
                if (IsRuleEnabled(csharpDocument, RuleName))
                {
                    ProcessDocumentRoot(csharpDocument.RootElement);
                }
            }
        }

        private void ProcessDocumentRoot(DocumentRoot documentRoot)
        {
            foreach (var child in documentRoot.ChildElements)
            {
                if (child.ElementType == ElementType.Namespace)
                {
                    ProcessNamespace((Namespace)child);
                }
            }
        }

        private void ProcessNamespace(Namespace ns)
        {
            foreach (var child in ns.ChildElements)
            {
                switch (child.ElementType)
                {
                    case ElementType.Namespace:
                        ProcessNamespace((Namespace)child);
                        break;
                    case ElementType.UsingDirective:
                        AddViolation(child, RuleName);
                        break;
                }
            }
        }
    }
}
