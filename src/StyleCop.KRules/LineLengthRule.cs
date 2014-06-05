using StyleCop.CSharp;

namespace StyleCop.KRules
{
    [SourceAnalyzer(typeof(CsParser))]
    public class LineLengthRule : SourceAnalyzer
    {
        private const string LineLengthProperty = "LineLength";
        private const string RuleName = "LineMustNotBeTooLong";

        public override void AnalyzeDocument(CodeDocument document)
        {
            var csharpDocument = (CsDocument)document;
            if (csharpDocument.RootElement != null && !csharpDocument.RootElement.Generated)
            {
                if (IsRuleEnabled(csharpDocument, RuleName))
                {
                    CheckLineLength(csharpDocument);
                }
            }
        }

        private void CheckLineLength(CsDocument csharpDocument)
        {
            var lengthProperty = GetSetting(csharpDocument.Settings, LineLengthProperty) as IntProperty;
            if (lengthProperty == null)
            {
                return;
            }

            using (var reader = csharpDocument.SourceCode.Read())
            {
                string line;
                var lineNumber = 0;
                while ((line = reader.ReadLine()) != null)
                {
                    lineNumber++;
                    if (line.Length > lengthProperty.Value)
                    {
                        AddViolation(csharpDocument.RootElement, lineNumber, RuleName);
                    }
                }
            }
        }
    }
}