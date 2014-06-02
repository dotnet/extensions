using System;
using StyleCop.CSharp;

namespace StyleCop.KRules
{
    [SourceAnalyzer(typeof(CsParser))]
    public class FileHeaderRule : SourceAnalyzer
    {
        private const string FileHeaderTextPropertyName = "FileHeaderText";
        private const string RuleName = "FileMustHaveHeader";

        public override void AnalyzeDocument(CodeDocument document)
        {
            var csharpDocument = (CsDocument)document;
            if (csharpDocument.RootElement != null && !csharpDocument.RootElement.Generated)
            {
                if (IsRuleEnabled(csharpDocument, RuleName))
                {
                    CheckDocumentHeader(csharpDocument);
                }
            }
        }

        private void CheckDocumentHeader(CsDocument csharpDocument)
        {
            var prop = GetSetting(csharpDocument.Settings, FileHeaderTextPropertyName) as StringProperty;
            if (prop != null)
            {
                var expectedText = prop.Value;
                var text = csharpDocument.FileHeader.HeaderText;
                if (!string.Equals(text, expectedText, StringComparison.Ordinal))
                {
                    AddViolation(csharpDocument.RootElement, 1, RuleName);
                }
            }
        }
    }
}