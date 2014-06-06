// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using StyleCop.CSharp;

namespace StyleCop.KRules
{
    [SourceAnalyzer(typeof(CsParser))]
    public class UseVarRule : SourceAnalyzer
    {
        private const string RuleName = "UseVarWhenPossible";

        public override bool DoAnalysis(CodeDocument document)
        {
            var doc = (CsDocument)document;

            // skipping wrong or auto-generated documents
            if (doc.RootElement == null || doc.RootElement.Generated)
            {
                return true;
            }

            if (IsRuleEnabled(document, RuleName))
            {
                doc.WalkDocument(null, null, VisitExpression);
            }

            return true;
        }

        private bool VisitExpression(
            Expression expression,
            Expression parentExpression,
            Statement parentStatement,
            CsElement parentElement,
            object context)
        {
            if (expression.ExpressionType == ExpressionType.VariableDeclaration)
            {
                var declarationExpression = expression as VariableDeclarationExpression;
                if (declarationExpression == null)
                {
                    this.Log(StyleCopLogLevel.High, "Unknown variable declaration type. " + expression.Location);
                    return true;
                }

                // Fields can't use var.
                if (declarationExpression.Parent != null && declarationExpression.Parent.Parent is Field)
                {
                    return true;
                }

                // Variables declared as const can't use var.
                var declarationStatement = parentStatement as VariableDeclarationStatement;
                if (declarationStatement != null &&
                    declarationStatement.Tokens.Any() &&
                    declarationStatement.Tokens.First().Text == "const")
                {

                    return true;
                }

                // Variables declared without an initializer can't use var.
                //
                // Ex: 
                //      object obj;
                //      if (dictionary.TryGetValue(..., out obj))
                if (declarationExpression.Declarators.Count == 1 &&
                    declarationExpression.Declarators.First().Initializer == null)
                {
                    return true;
                }

                // Variables initialized to null can't use var.
                //
                // Ex: 
                //      string s = null;
                if (declarationExpression.Declarators.Count == 1 &&
                    declarationExpression.Declarators.First().Initializer != null &&
                    declarationExpression.Declarators.First().Initializer.Text == "null")
                {
                    return true;
                }

                // Variables initialized with a lambda can't use var.
                //
                // Ex: 
                //      Func<bool> f = () => true;
                if (declarationExpression.Declarators.Count == 1 &&
                    declarationExpression.Declarators.First().Initializer != null &&
                    declarationExpression.Declarators.First().Initializer.ExpressionType == ExpressionType.Lambda)
                {
                    return true;
                }

                if (declarationExpression.Type.Text == "var")
                {
                    return true;
                }

                AddViolation(parentElement, expression.Location, RuleName, expression.Text);
                return true;
            }

            return true;
        }
    }
}