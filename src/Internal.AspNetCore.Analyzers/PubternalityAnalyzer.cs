// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Internal.AspNetCore.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class PubternalityAnalyzer : DiagnosticAnalyzer
    {
        public PubternalityAnalyzer()
        {
            SupportedDiagnostics = ImmutableArray.Create(new[]
            {
                PubturnalityDescriptors.PUB0001,
                PubturnalityDescriptors.PUB0002
            });
        }

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();

            context.RegisterCompilationStartAction(analysisContext =>
            {
                analysisContext.RegisterSymbolAction(symbolAnalysisContext => AnalyzeTypeUsage(symbolAnalysisContext), SymbolKind.Namespace);
                analysisContext.RegisterSyntaxNodeAction(syntaxContext => AnalyzeTypeUsage(syntaxContext), SyntaxKind.IdentifierName);
            });
        }

        private void AnalyzeTypeUsage(SymbolAnalysisContext context)
        {
            var ns = (INamespaceSymbol)context.Symbol;
            if (IsInternal(ns))
            {
                return;
            }

            foreach (var namespaceOrTypeSymbol in ns.GetMembers())
            {
                if (namespaceOrTypeSymbol.IsType)
                {
                    CheckType((ITypeSymbol)namespaceOrTypeSymbol, context);
                }
            }
        }

        private void CheckType(ITypeSymbol typeSymbol, SymbolAnalysisContext context)
        {
            if (IsPrivate(typeSymbol) || IsPrivate(typeSymbol.ContainingType))
            {
                return;
            }

            CheckAttributes(context, typeSymbol.GetAttributes());

            if (typeSymbol.BaseType != null)
            {
                CheckType(context, typeSymbol.BaseType, typeSymbol.DeclaringSyntaxReferences);
            }

            foreach (var interfaceImpl in typeSymbol.AllInterfaces)
            {
                CheckType(context, interfaceImpl, typeSymbol.DeclaringSyntaxReferences);
            }

            foreach (var member in typeSymbol.GetMembers())
            {
                CheckMember(context, member);
            }

            foreach (var innerType in typeSymbol.GetTypeMembers())
            {
                CheckType(innerType, context);
            }

            if (typeSymbol is INamedTypeSymbol namedTypeSymbol)
            {
                // Check delegate signatures
                if (namedTypeSymbol.DelegateInvokeMethod != null)
                {
                    CheckMethod(context, namedTypeSymbol.DelegateInvokeMethod);
                }
            }
        }

        private void CheckMember(SymbolAnalysisContext context, ISymbol symbol)
        {
            if (IsPrivate(symbol))
            {
                return;
            }


            switch (symbol)
            {
                case IFieldSymbol fieldSymbol:
                {
                    CheckAttributes(context, fieldSymbol.GetAttributes());
                    CheckType(context, fieldSymbol.Type, fieldSymbol.DeclaringSyntaxReferences);
                    break;
                }
                case IPropertySymbol propertySymbol:
                {
                    CheckAttributes(context, propertySymbol.GetAttributes());
                    CheckType(context, propertySymbol.Type, propertySymbol.DeclaringSyntaxReferences);
                    break;
                }
                case IMethodSymbol methodSymbol:
                {
                    // Skip compiler generated members that we already explicitly check
                    switch (methodSymbol.MethodKind)
                    {
                        case MethodKind.EventAdd:
                        case MethodKind.EventRaise:
                        case MethodKind.EventRemove:
                        case MethodKind.PropertyGet:
                        case MethodKind.PropertySet:
                        case MethodKind.DelegateInvoke:
                        case MethodKind.Ordinary when methodSymbol.ContainingType.TypeKind == TypeKind.Delegate:
                            return;
                    }

                    CheckMethod(context, methodSymbol);
                    break;
                }
                case IEventSymbol eventSymbol:
                    CheckType(context, eventSymbol.Type, eventSymbol.DeclaringSyntaxReferences);
                    break;
            }
        }

        private void CheckMethod(SymbolAnalysisContext context, IMethodSymbol methodSymbol)
        {
            if (IsPrivate(methodSymbol))
            {
                return;
            }

            CheckAttributes(context, methodSymbol.GetAttributes());
            CheckAttributes(context, methodSymbol.GetReturnTypeAttributes());
            foreach (var parameter in methodSymbol.Parameters)
            {
                CheckAttributes(context, parameter.GetAttributes());
                CheckType(context, parameter.Type, parameter.DeclaringSyntaxReferences);
            }

            CheckType(context, methodSymbol.ReturnType, methodSymbol.DeclaringSyntaxReferences);
        }

        private static bool IsPrivate(ISymbol symbol)
        {
            return symbol != null &&
                   (symbol.DeclaredAccessibility == Accessibility.Private ||
                   symbol.DeclaredAccessibility == Accessibility.Internal ||
                   IsInternal(symbol.ContainingNamespace));
        }

        private void CheckAttributes(SymbolAnalysisContext context, ImmutableArray<AttributeData> attributes)
        {
            foreach (var attributeData in attributes)
            {
                CheckType(context, attributeData.AttributeClass, attributeData.ApplicationSyntaxReference);
            }
        }

        private void CheckType(SymbolAnalysisContext context, ITypeSymbol symbol, SyntaxReference syntax)
        {
            if (ContainsPubternalType(symbol))
            {
                context.ReportDiagnostic(Diagnostic.Create(PubturnalityDescriptors.PUB0001, syntax.GetSyntax().GetLocation()));
            }
        }

        private void CheckType(SymbolAnalysisContext context, ITypeSymbol symbol, ImmutableArray<SyntaxReference> syntaxReferences)
        {
            if (ContainsPubternalType(symbol))
            {
                foreach (var syntaxReference in syntaxReferences)
                {
                    context.ReportDiagnostic(Diagnostic.Create(PubturnalityDescriptors.PUB0001, syntaxReference.GetSyntax().GetLocation()));
                }
            }
        }

        private bool ContainsPubternalType(ITypeSymbol symbol)
        {
            if (IsInternal(symbol.ContainingNamespace))
            {
                return true;
            }
            else
            {
                if (symbol is INamedTypeSymbol namedTypeSymbol && namedTypeSymbol.IsGenericType)
                {
                    foreach (var argument in namedTypeSymbol.TypeArguments)
                    {
                        if (ContainsPubternalType(argument))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private void AnalyzeTypeUsage(SyntaxNodeAnalysisContext syntaxContext)
        {
            var identifier = (IdentifierNameSyntax)syntaxContext.Node;

            var symbolInfo = ModelExtensions.GetTypeInfo(syntaxContext.SemanticModel, identifier, syntaxContext.CancellationToken);
            if (symbolInfo.Type == null)
            {
                return;
            }

            var type = symbolInfo.Type;
            if (!IsInternal(type.ContainingNamespace))
            {
                // don't care about non-pubternal type references
                return;
            }

            if (!syntaxContext.ContainingSymbol.ContainingAssembly.Equals(type.ContainingAssembly))
            {
                syntaxContext.ReportDiagnostic(Diagnostic.Create(PubturnalityDescriptors.PUB0002, identifier.GetLocation()));
            }
        }

        private static bool IsInternal(INamespaceSymbol ns)
        {
            while (ns != null)
            {
                if (ns.Name == "Internal")
                {
                    return true;
                }

                ns = ns.ContainingNamespace;
            }

            return false;
        }
    }
}
