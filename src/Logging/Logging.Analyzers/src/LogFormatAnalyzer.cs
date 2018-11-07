// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.Extensions.Logging.Internal;

namespace Microsoft.Extensions.Logging.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class LogFormatAnalyzer : DiagnosticAnalyzer
    {
        public LogFormatAnalyzer()
        {
            SupportedDiagnostics = ImmutableArray.Create(new[]
            {
                Descriptors.MEL0001NumericsInFormatString,
                Descriptors.MEL0002ConcatenationInFormatString,
                Descriptors.MEL0003FormatParameterCountMismatch,
                Descriptors.MEL0004UseCompiledLogMessages,
                Descriptors.MEL0005UsePascalCasedLogMessageTokens,
            });
        }

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();

            context.RegisterCompilationStartAction(analysisContext =>
            {
                var loggerExtensionsType = analysisContext.Compilation.GetTypeByMetadataName("Microsoft.Extensions.Logging.LoggerExtensions");
                var loggerType = analysisContext.Compilation.GetTypeByMetadataName("Microsoft.Extensions.Logging.ILogger");
                var loggerMessageType = analysisContext.Compilation.GetTypeByMetadataName("Microsoft.Extensions.Logging.LoggerMessage");
                if (loggerExtensionsType == null || loggerType == null || loggerMessageType == null)
                {
                    return;
                }

                analysisContext.RegisterSyntaxNodeAction(syntaxContext => AnalyzeInvocation(syntaxContext, loggerType, loggerExtensionsType, loggerMessageType), SyntaxKind.InvocationExpression);
            });
        }

        private void AnalyzeInvocation(SyntaxNodeAnalysisContext syntaxContext, INamedTypeSymbol loggerType, INamedTypeSymbol loggerExtensionsType, INamedTypeSymbol loggerMessageType)
        {
            var invocation = (InvocationExpressionSyntax)syntaxContext.Node;

            var symbolInfo = ModelExtensions.GetSymbolInfo(syntaxContext.SemanticModel, invocation, syntaxContext.CancellationToken);
            if (symbolInfo.Symbol?.Kind != SymbolKind.Method)
            {
                return;
            }

            var methodSymbol = (IMethodSymbol)symbolInfo.Symbol;

            if (methodSymbol.ContainingType == loggerExtensionsType)
            {
                syntaxContext.ReportDiagnostic(Diagnostic.Create(Descriptors.MEL0004UseCompiledLogMessages, invocation.GetLocation(), methodSymbol.Name));
            }
            else if (methodSymbol.ContainingType != loggerType && methodSymbol.ContainingType != loggerMessageType)
            {
                return;
            }

            if (FindLogParameters(methodSymbol, out var messageArgument, out var paramsArgument))
            {
                int paramsCount = 0;
                ExpressionSyntax formatExpression = null;
                bool argsIsArray = false;

                if (methodSymbol.ContainingType == loggerMessageType)
                {
                    // For LoggerMessage.Define, count type parameters on the invocation instead of arguments
                    paramsCount = methodSymbol.TypeParameters.Length;
                    var arg = invocation.ArgumentList.Arguments.FirstOrDefault(argument =>
                    {
                        var parameter = DetermineParameter(argument, syntaxContext.SemanticModel, syntaxContext.CancellationToken);
                        return Equals(parameter, messageArgument);
                    });
                    formatExpression = arg.Expression;
                }
                else
                {
                    foreach (var argument in invocation.ArgumentList.Arguments)
                    {
                        var parameter = DetermineParameter(argument, syntaxContext.SemanticModel, syntaxContext.CancellationToken);
                        if (Equals(parameter, messageArgument))
                        {
                            formatExpression = argument.Expression;
                        }
                        else if (Equals(parameter, paramsArgument))
                        {
                            var parameterType = syntaxContext.SemanticModel.GetTypeInfo(argument.Expression).ConvertedType;
                            if (parameterType == null)
                            {
                                return;
                            }

                            //Detect if current argument can be passed directly to args
                            argsIsArray = parameterType.TypeKind == TypeKind.Array && ((IArrayTypeSymbol)parameterType).ElementType.SpecialType == SpecialType.System_Object;

                            paramsCount++;
                        }
                    }
                }

                AnalyzeFormatArgument(syntaxContext, formatExpression, paramsCount, argsIsArray);
            }
        }

        private void AnalyzeFormatArgument(SyntaxNodeAnalysisContext syntaxContext, ExpressionSyntax formatExpression, int paramsCount, bool argsIsArray)
        {
            var text = TryGetFormatText(formatExpression, syntaxContext.SemanticModel);
            if (text == null)
            {
                syntaxContext.ReportDiagnostic(Diagnostic.Create(Descriptors.MEL0002ConcatenationInFormatString, formatExpression.GetLocation()));
                return;
            }

            LogValuesFormatter formatter;
            try
            {
                formatter = new LogValuesFormatter(text);
            }
            catch (Exception)
            {
                return;
            }

            foreach (var valueName in formatter.ValueNames)
            {
                if (int.TryParse(valueName, out _))
                {
                    syntaxContext.ReportDiagnostic(Diagnostic.Create(Descriptors.MEL0001NumericsInFormatString, formatExpression.GetLocation()));
                }
                else if (char.IsLower(valueName[0]))
                {
                    syntaxContext.ReportDiagnostic(Diagnostic.Create(Descriptors.MEL0005UsePascalCasedLogMessageTokens, formatExpression.GetLocation()));
                }
            }

            var argsPassedDirectly = argsIsArray && paramsCount == 1;
            if (!argsPassedDirectly && paramsCount != formatter.ValueNames.Count)
            {
                syntaxContext.ReportDiagnostic(Diagnostic.Create(Descriptors.MEL0003FormatParameterCountMismatch, formatExpression.GetLocation()));
            }
        }

        private string TryGetFormatText(ExpressionSyntax argumentExpression, SemanticModel semanticModel)
        {
            switch (argumentExpression)
            {
                case LiteralExpressionSyntax literal when literal.Token.IsKind(SyntaxKind.StringLiteralToken):
                    return literal.Token.ValueText;
                case InterpolatedStringExpressionSyntax interpolated:
                    var text = "";
                    foreach (var interpolatedStringContentSyntax in interpolated.Contents)
                    {
                        if (interpolatedStringContentSyntax is InterpolatedStringTextSyntax textSyntax)
                        {
                            text += textSyntax.TextToken.ValueText;
                        }
                        else
                        {
                            return null;
                        }
                    }
                    return text;
                case InvocationExpressionSyntax invocation when IsNameOfInvocation(invocation):
                    // return placeholder from here because actual value is not required for analysis and is hard to get
                    return "NAMEOF";
                case ParenthesizedExpressionSyntax parenthesized:
                    return TryGetFormatText(parenthesized.Expression, semanticModel);
                case BinaryExpressionSyntax binary when binary.OperatorToken.IsKind(SyntaxKind.PlusToken):
                    var leftText = TryGetFormatText(binary.Left, semanticModel);
                    var rightText = TryGetFormatText(binary.Right, semanticModel);

                    if (leftText != null && rightText != null)
                    {
                        return leftText + rightText;
                    }

                    return null;
                default:
                    var constant = semanticModel.GetConstantValue(argumentExpression);
                    if (constant.HasValue && constant.Value is string constantString)
                    {
                        return constantString;
                    }
                    return null;
            }
        }

        private bool FindLogParameters(IMethodSymbol methodSymbol, out IParameterSymbol message, out IParameterSymbol arguments)
        {
            message = null;
            arguments = null;
            foreach (var parameter in methodSymbol.Parameters)
            {
                if (parameter.Type.SpecialType == SpecialType.System_String &&
                    string.Equals(parameter.Name, "message", StringComparison.Ordinal) ||
                    string.Equals(parameter.Name, "messageFormat", StringComparison.Ordinal) ||
                    string.Equals(parameter.Name, "formatString", StringComparison.Ordinal))
                {
                    message = parameter;
                }

                // When calling logger.BeginScope("{Param}") generic overload would be selected
                if (parameter.Type.SpecialType == SpecialType.System_String &&
                    methodSymbol.Name.Equals("BeginScope") &&
                    string.Equals(parameter.Name, "state", StringComparison.Ordinal))
                {
                    message = parameter;
                }

                if (parameter.IsParams &&
                    string.Equals(parameter.Name, "args", StringComparison.Ordinal))
                {
                    arguments = parameter;
                }
            }
            return message != null;
        }

        private static bool IsNameOfInvocation(InvocationExpressionSyntax invocation)
        {
            return invocation.Expression is IdentifierNameSyntax identifierName &&
                   (identifierName.Identifier.IsKind(SyntaxKind.NameOfKeyword) ||
                   identifierName.Identifier.ToString() == SyntaxFacts.GetText(SyntaxKind.NameOfKeyword));
        }

        private static IParameterSymbol DetermineParameter(
            ArgumentSyntax argument,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            if (!(argument.Parent is BaseArgumentListSyntax argumentList))
            {
                return null;
            }

            if (!(argumentList.Parent is ExpressionSyntax invocableExpression))
            {
                return null;
            }

            if (!(semanticModel.GetSymbolInfo(invocableExpression, cancellationToken).Symbol is IMethodSymbol symbol))
            {
                return null;
            }

            var parameters = symbol.Parameters;

            // Handle named argument
            if (argument.NameColon != null && !argument.NameColon.IsMissing)
            {
                var name = argument.NameColon.Name.Identifier.ValueText;
                return parameters.FirstOrDefault(p => p.Name == name);
            }

            // Handle positional argument
            var index = argumentList.Arguments.IndexOf(argument);
            if (index < 0)
            {
                return null;
            }

            if (index < parameters.Length)
            {
                return parameters[index];
            }

            var lastParameter = parameters.LastOrDefault();
            if (lastParameter == null)
            {
                return null;
            }

            if (lastParameter.IsParams)
            {
                return lastParameter;
            }

            return null;
        }
    }
}
