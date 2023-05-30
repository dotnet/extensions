﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Gen.Logging.Model;
using Microsoft.Gen.Shared;

namespace Microsoft.Gen.Logging.Parsing;

internal sealed class Parser
{
    private readonly CancellationToken _cancellationToken;
    private readonly Compilation _compilation;
    private readonly Action<Diagnostic> _reportDiagnostic;

    public Parser(Compilation compilation, Action<Diagnostic> reportDiagnostic, CancellationToken cancellationToken)
    {
        _compilation = compilation;
        _cancellationToken = cancellationToken;
        _reportDiagnostic = reportDiagnostic;
    }

    /// <summary>
    /// Gets the set of logging types containing methods to output.
    /// </summary>
    [SuppressMessage("Maintainability", "CA1505:Avoid unmaintainable code", Justification = "Fix this in a follow-up")]
    public IReadOnlyList<LoggingType> GetLogTypes(IEnumerable<TypeDeclarationSyntax> types)
    {
        Action<DiagnosticDescriptor, Location?, object?[]?> diagReport = Diag; // Keeping one instance of the delegate
        var symbols = SymbolLoader.LoadSymbols(_compilation, diagReport);
        if (symbols == null)
        {
            // nothing to do if required symbols aren't available
            return Array.Empty<LoggingType>();
        }

        var ids = new HashSet<int>();
        var eventNames = new HashSet<string>();
        var results = new List<LoggingType>();
        var parameterSymbols = new Dictionary<LoggingMethodParameter, IParameterSymbol>();

        // we enumerate by syntax tree, to minimize the need to instantiate semantic models (since they're expensive)
        foreach (var group in types.GroupBy(x => x.SyntaxTree))
        {
            SemanticModel? sm = null;
            foreach (var typeDec in group)
            {
                // stop if we're asked to
                _cancellationToken.ThrowIfCancellationRequested();

                LoggingType? lt = null;
                string nspace = string.Empty;
                string? loggerField = null;
                string? redactorProviderField = null;
                bool loggerFieldNullable = false;
                bool redactorProviderFieldNullable = false;
                IFieldSymbol? secondLoggerField = null;
                IFieldSymbol? secondRedactorProviderField = null;

                ids.Clear();
                foreach (var method in typeDec.Members.Where(m => m.IsKind(SyntaxKind.MethodDeclaration)).Cast<MethodDeclarationSyntax>())
                {
                    sm ??= _compilation.GetSemanticModel(typeDec.SyntaxTree);

                    var attrLoc = GetLogMethodAttribute(method, sm, symbols);
                    if (attrLoc == null)
                    {
                        // doesn't have the magic attribute we like, so ignore
                        continue;
                    }

                    var methodSymbol = sm.GetDeclaredSymbol(method, _cancellationToken);
                    if (methodSymbol == null)
                    {
                        // we only care about methods
                        continue;
                    }

                    var (lm, keepMethod) = ProcessMethod(method, methodSymbol, attrLoc);

                    parameterSymbols.Clear();
                    MethodParsingState parsingState = default;

                    foreach (var paramSymbol in methodSymbol.Parameters)
                    {
                        var lp = ProcessParameter(paramSymbol, symbols, ref parsingState);
                        if (lp == null)
                        {
                            keepMethod = false;
                            continue;
                        }

                        parameterSymbols[lp] = paramSymbol;

                        // Check if the parameter is annotated with an attribute to enable logging of its properties:
                        var logPropertiesAttribute = ParserUtilities.GetSymbolAttributeAnnotationOrDefault(symbols.LogPropertiesAttribute, paramSymbol);
                        if (logPropertiesAttribute is not null)
                        {
                            var processingResult = LogParserUtilities.ProcessLogPropertiesForParameter(
                                logPropertiesAttribute,
                                lm,
                                lp,
                                paramSymbol,
                                symbols,
                                diagReport,
                                _compilation,
                                _cancellationToken);

                            if (processingResult == LogPropertiesProcessingResult.Fail)
                            {
                                keepMethod = false;
                            }
                            else
                            {
                                parsingState.FoundCustomLogPropertiesProvider |= lp.HasPropsProvider;
                                if (processingResult == LogPropertiesProcessingResult.SucceededWithRedaction)
                                {
                                    parsingState.FoundDataClassificationAnnotation = true;
                                }
                            }
                        }

                        bool forceAsTemplateParam = false;
                        if (lp.IsLogger && lm.TemplateMap.ContainsKey(lp.Name))
                        {
                            Diag(DiagDescriptors.ShouldntMentionLoggerInMessage, attrLoc, lp.Name);
                            forceAsTemplateParam = true;
                        }
                        else if (lp.IsException && lm.TemplateMap.ContainsKey(lp.Name))
                        {
                            Diag(DiagDescriptors.ShouldntMentionExceptionInMessage, attrLoc, lp.Name);
                            forceAsTemplateParam = true;
                        }
                        else if (lp.IsLogLevel && lm.TemplateMap.ContainsKey(lp.Name))
                        {
                            Diag(DiagDescriptors.ShouldntMentionLogLevelInMessage, attrLoc, lp.Name);
                            forceAsTemplateParam = true;
                        }
                        else if (lp.IsNormalParameter && !lm.TemplateMap.ContainsKey(lp.Name) &&
                                 logPropertiesAttribute == null && !string.IsNullOrEmpty(lm.Message))
                        {
                            Diag(DiagDescriptors.ArgumentHasNoCorrespondingTemplate, paramSymbol.GetLocation(), lp.Name);
                        }

                        // "Name" property doesn't contain '@' even if there was one in the source.
                        if (lp.Name[0] == '_')
                        {
                            // can't have logging method parameter names that start with _ since that can lead to conflicting symbol names
                            // because all generated symbols start with _
                            Diag(DiagDescriptors.InvalidLoggingMethodParameterName, paramSymbol.GetLocation());
                        }

                        lm.AllParameters.Add(lp);
                        if (lp.IsNormalParameter || forceAsTemplateParam)
                        {
                            if (lm.TemplateMap.ContainsKey(lp.Name))
                            {
                                lm.TemplateParameters.Add(lp);
                            }
                        }
                    }

                    if (keepMethod)
                    {
                        if (lm.IsStatic)
                        {
                            if (!parsingState.FoundLogger)
                            {
                                Diag(DiagDescriptors.MissingLoggerArgument, method.ParameterList.GetLocation(), lm.Name);
                                keepMethod = false;
                            }
                            else if (parsingState.FoundRedactorProvider && !parsingState.FoundDataClassificationAnnotation)
                            {
                                Diag(DiagDescriptors.MissingDataClassificationArgument, method.ParameterList.GetLocation(), lm.Name);
                            }
                            else if (parsingState.FoundDataClassificationAnnotation && !parsingState.FoundRedactorProvider)
                            {
                                Diag(DiagDescriptors.MissingRedactorProviderArgument, method.ParameterList.GetLocation());
                                keepMethod = false;
                            }
                            else if (parsingState.FoundRedactorProvider && parsingState.FoundCustomLogPropertiesProvider)
                            {
                                foreach (var lp in lm.AllParameters)
                                {
                                    if (lp.HasPropsProvider)
                                    {
                                        Diag(DiagDescriptors.LogPropertiesProviderWithRedaction, parameterSymbols[lp].GetLocation(), lp.Name);
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (!parsingState.FoundLogger)
                            {
                                if (loggerField == null)
                                {
                                    (loggerField, secondLoggerField, loggerFieldNullable) = FindField(sm, typeDec, symbols.ILoggerSymbol);
                                }

                                if (secondLoggerField != null)
                                {
                                    Diag(DiagDescriptors.MultipleLoggerFields, secondLoggerField.GetLocation(), typeDec.Identifier.Text);
                                    keepMethod = false;
                                }
                                else if (loggerField == null)
                                {
                                    Diag(DiagDescriptors.MissingLoggerField, method.Identifier.GetLocation(), typeDec.Identifier.Text);
                                    keepMethod = false;
                                }
                                else
                                {
                                    lm.LoggerField = loggerField;
                                    lm.LoggerFieldNullable = loggerFieldNullable;
                                }
                            }

                            if (!parsingState.FoundRedactorProvider && parsingState.FoundDataClassificationAnnotation)
                            {
                                if (symbols.RedactorProviderSymbol == null)
                                {
                                    // nothing to do if this type isn't available
                                    Diag(DiagDescriptors.MissingRequiredType, method.GetLocation(), SymbolLoader.IRedactorProviderType);
                                    return Array.Empty<LoggingType>();
                                }

                                if (redactorProviderField == null)
                                {
                                    (redactorProviderField, secondRedactorProviderField, redactorProviderFieldNullable) = FindField(sm, typeDec, symbols.RedactorProviderSymbol);
                                }

                                if (secondRedactorProviderField != null)
                                {
                                    Diag(DiagDescriptors.MultipleRedactorProviderFields, secondRedactorProviderField.GetLocation(), typeDec.Identifier.Text);
                                    keepMethod = false;
                                }
                                else if (redactorProviderField == null)
                                {
                                    Diag(DiagDescriptors.MissingRedactorProviderField, method.GetLocation(), typeDec.Identifier.Text);
                                    keepMethod = false;
                                }
                                else
                                {
                                    lm.RedactorProviderField = redactorProviderField;
                                    lm.RedactorProviderFieldNullable = redactorProviderFieldNullable;
                                }
                            }
                            else if (parsingState.FoundRedactorProvider && !parsingState.FoundDataClassificationAnnotation)
                            {
                                Diag(DiagDescriptors.MissingDataClassificationArgument, method.GetLocation(), lm.Name);
                            }

                            // Show this warning only if other checks passed
                            if (keepMethod &&
                                parsingState.FoundLogger &&
                                (!parsingState.FoundDataClassificationAnnotation || parsingState.FoundRedactorProvider))
                            {
                                Diag(DiagDescriptors.LoggingMethodShouldBeStatic, method.Identifier.GetLocation());
                            }
                        }

                        if (lm.Level == null && !parsingState.FoundLogLevel)
                        {
                            Diag(DiagDescriptors.MissingLogLevel, method.GetLocation());
                            keepMethod = false;
                        }

                        if (keepMethod &&
                            string.IsNullOrWhiteSpace(lm.Message) &&
                            !lm.EventId.HasValue &&
                            lm.AllParameters.All(x => x.IsLogger || x.IsRedactorProvider || x.IsLogLevel))
                        {
                            Diag(DiagDescriptors.EmptyLoggingMethod, method.Identifier.GetLocation(), methodSymbol.Name);
                        }

                        foreach (var t in lm.TemplateMap)
                        {
                            bool found = false;
                            foreach (var p in lm.AllParameters)
                            {
                                if (t.Key.Equals(p.Name, StringComparison.OrdinalIgnoreCase))
                                {
                                    found = true;
                                    break;
                                }
                            }

                            if (!found)
                            {
                                Diag(DiagDescriptors.TemplateHasNoCorrespondingArgument, attrLoc, t.Key);
                            }
                        }

                        LogParserUtilities.CheckMethodParametersAreUnique(lm, diagReport, parameterSymbols);
                    }

                    if (lt == null)
                    {
                        // determine the namespace the class is declared in, if any
                        SyntaxNode? potentialNamespaceParent = typeDec.Parent;
                        while (potentialNamespaceParent != null &&
#if ROSLYN_4_0_OR_GREATER
                            potentialNamespaceParent is not NamespaceDeclarationSyntax &&
                            potentialNamespaceParent is not FileScopedNamespaceDeclarationSyntax)
#else
                            potentialNamespaceParent is not NamespaceDeclarationSyntax)
#endif
                        {
                            potentialNamespaceParent = potentialNamespaceParent.Parent;
                        }

#if ROSLYN_4_0_OR_GREATER
                        BaseNamespaceDeclarationSyntax? namespaceParent = potentialNamespaceParent as BaseNamespaceDeclarationSyntax;
#else
                        NamespaceDeclarationSyntax? namespaceParent = potentialNamespaceParent as NamespaceDeclarationSyntax;
#endif
                        if (namespaceParent != null)
                        {
                            nspace = namespaceParent.Name.ToString();
                            while (true)
                            {
                                namespaceParent = namespaceParent.Parent as NamespaceDeclarationSyntax;
                                if (namespaceParent == null)
                                {
                                    break;
                                }

                                nspace = $"{namespaceParent.Name}.{nspace}";
                            }
                        }
                    }

                    if (keepMethod)
                    {
                        if (lt == null)
                        {
                            lt = new LoggingType
                            {
                                Keyword = typeDec.Keyword.ValueText,
                                Namespace = nspace,
                                Name = typeDec.Identifier.ToString() + typeDec.TypeParameterList,
                                Parent = null,
                            };

                            LoggingType currentLoggerClass = lt;
                            var parentLoggerClass = typeDec.Parent as TypeDeclarationSyntax;
                            var parentType = methodSymbol.ContainingType.ContainingType;

                            static bool IsAllowedKind(SyntaxKind kind) =>
                                kind == SyntaxKind.ClassDeclaration ||
                                kind == SyntaxKind.StructDeclaration ||
                                kind == SyntaxKind.RecordDeclaration;

                            while (parentLoggerClass != null && IsAllowedKind(parentLoggerClass.Kind()))
                            {
                                currentLoggerClass.Parent = new LoggingType
                                {
                                    Keyword = parentLoggerClass.Keyword.ValueText,
                                    Namespace = nspace,
                                    Name = parentLoggerClass.Identifier.ToString() + parentLoggerClass.TypeParameterList,
                                    Parent = null,
                                };

                                currentLoggerClass = currentLoggerClass.Parent;
                                parentLoggerClass = parentLoggerClass.Parent as TypeDeclarationSyntax;
                                parentType = parentType.ContainingType;
                            }
                        }

                        lt.Methods.Add(lm);
                    }
                }

                if (lt != null)
                {
                    results.Add(lt);
                }
            }
        }

        return results;

        (LoggingMethod lm, bool keepMethod) ProcessMethod(MethodDeclarationSyntax method, IMethodSymbol methodSymbol, Location attrLoc)
        {
            var attr = ParserUtilities.GetSymbolAttributeAnnotationOrDefault(symbols.LogMethodAttribute, methodSymbol)!;

            var (eventId, level, message, eventName, skipEnabledCheck) = AttributeProcessors.ExtractLogMethodAttributeValues(attr, symbols);

            var lm = new LoggingMethod
            {
                Name = methodSymbol.Name,
                Level = level,
                Message = message,
                EventId = eventId,
                EventName = eventName,
                SkipEnabledCheck = skipEnabledCheck,
                IsExtensionMethod = methodSymbol.IsExtensionMethod,
                IsStatic = methodSymbol.IsStatic,
                Modifiers = method.Modifiers.ToString(),
                HasXmlDocumentation = HasXmlDocumentation(method),
            };

            TemplateExtractor.ExtractTemplates(message, lm.TemplateMap, out var templatesWithAtSymbol);

            var keepMethod = true;
            if (lm.Name[0] == '_')
            {
                // can't have logging method names that start with _ since that can lead to conflicting symbol names
                // because the generated symbols start with _
                Diag(DiagDescriptors.InvalidLoggingMethodName, method.Identifier.GetLocation());
            }

            if (!methodSymbol.ReturnsVoid)
            {
                // logging methods must return void
                Diag(DiagDescriptors.LoggingMethodMustReturnVoid, method.ReturnType.GetLocation());
                keepMethod = false;
            }

            if (templatesWithAtSymbol.Count > 0)
            {
                // there is/are template(s) that start with @, which is not allowed
                Diag(DiagDescriptors.TemplateStartsWithAtSymbol, attrLoc, method.Identifier.Text, string.Join("; ", templatesWithAtSymbol));
                keepMethod = false;
            }

            if (method.Arity > 0)
            {
                // we don't currently support generic methods
                Diag(DiagDescriptors.LoggingMethodIsGeneric, method.TypeParameterList!.GetLocation());
                keepMethod = false;
            }

#if ROSLYN_4_0_OR_GREATER
            bool isPartial = methodSymbol.IsPartialDefinition;
#else
            bool isPartial = true;  // don't check for this condition on older versions of Roslyn since IsPartialDefinition doesn't exist
#endif

            if (method.Body != null)
            {
                Diag(DiagDescriptors.LoggingMethodHasBody, method.Body.GetLocation());
                keepMethod = false;
            }
            else if (!isPartial)
            {
                Diag(DiagDescriptors.LoggingMethodMustBePartial, method.Identifier.GetLocation());
                keepMethod = false;
            }

            // ensure there are no duplicate ids.
            if (lm.EventId != null)
            {
                if (!ids.Add(lm.EventId.Value))
                {
                    Diag(DiagDescriptors.ShouldntReuseEventIds, attrLoc, lm.EventId.Value, methodSymbol.ContainingType.Name);
                }
            }

            // ensure there are no duplicate event names.
            if (lm.EventName != null)
            {
                if (!eventNames.Add(lm.EventName))
                {
                    Diag(DiagDescriptors.ShouldntReuseEventNames, attrLoc, lm.EventName, methodSymbol.ContainingType.Name);
                }
            }

            var msg = lm.Message;
#pragma warning disable S1067 // Expressions should not be too complex
            if (msg.StartsWith("INFORMATION:", StringComparison.OrdinalIgnoreCase)
                || msg.StartsWith("INFO:", StringComparison.OrdinalIgnoreCase)
                || msg.StartsWith("WARNING:", StringComparison.OrdinalIgnoreCase)
                || msg.StartsWith("WARN:", StringComparison.OrdinalIgnoreCase)
                || msg.StartsWith("ERROR:", StringComparison.OrdinalIgnoreCase)
                || msg.StartsWith("ERR:", StringComparison.OrdinalIgnoreCase))
#pragma warning restore S1067 // Expressions should not be too complex
            {
                Diag(DiagDescriptors.RedundantQualifierInMessage, attrLoc, methodSymbol.Name);
            }

            return (lm, keepMethod);
        }
    }

    private static bool HasXmlDocumentation(MethodDeclarationSyntax method)
    {
        var triviaList = method.GetLeadingTrivia();
        foreach (var trivia in triviaList)
        {
            if (trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia))
            {
                return true;
            }
        }

        return false;
    }

    private LoggingMethodParameter? ProcessParameter(
        IParameterSymbol paramSymbol,
        SymbolHolder symbols,
        ref MethodParsingState parsingState)
    {
        var paramName = paramSymbol.Name;

        var needsAtSign = false;
        if (!paramSymbol.DeclaringSyntaxReferences.IsDefaultOrEmpty)
        {
            var paramSyntax = paramSymbol.DeclaringSyntaxReferences[0].GetSyntax(_cancellationToken) as ParameterSyntax;
            if (!string.IsNullOrEmpty(paramSyntax!.Identifier.Text))
            {
                needsAtSign = paramSyntax.Identifier.Text[0] == '@';
            }
        }

        if (string.IsNullOrWhiteSpace(paramName))
        {
            // semantic problem, just bail quietly
            return null;
        }

        var paramTypeSymbol = paramSymbol.Type;
        if (paramTypeSymbol is IErrorTypeSymbol)
        {
            // semantic problem, just bail quietly
            return null;
        }

        string? qualifier = null;
        if (paramSymbol.RefKind == RefKind.In)
        {
            qualifier = "in";
        }
        else if (paramSymbol.RefKind != RefKind.None)
        {
            // Parameter has "ref", "out" modifier, no can do
            Diag(DiagDescriptors.LoggingMethodParameterRefKind, paramSymbol.GetLocation(), paramSymbol.ContainingSymbol.Name, paramName);
            return null;
        }

        string typeName = paramTypeSymbol.ToDisplayString(
            SymbolDisplayFormat.FullyQualifiedFormat.WithMiscellaneousOptions(
                SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier | SymbolDisplayMiscellaneousOptions.UseSpecialTypes));

        INamedTypeSymbol? classificationAttributeType = null;

        var paramDataClassAttributes = paramSymbol.GetDataClassificationAttributes(symbols);
        if (paramDataClassAttributes.Count > 1)
        {
            Diag(DiagDescriptors.MultipleDataClassificationAttributes, paramSymbol.GetLocation());
        }
        else
        {
            bool isAnnotatedWithDataClassAttr = paramDataClassAttributes.Count == 1;
            if (isAnnotatedWithDataClassAttr)
            {
                classificationAttributeType = paramDataClassAttributes[0];
                parsingState.FoundDataClassificationAnnotation = true;
            }
        }

        var extractedType = paramTypeSymbol;
        if (paramTypeSymbol.IsNullableOfT())
        {
            // extract the T from a Nullable<T>
            extractedType = ((INamedTypeSymbol)paramTypeSymbol).TypeArguments[0];
        }

        var lp = new LoggingMethodParameter
        {
            Name = paramName,
            Type = typeName,
            Qualifier = qualifier,
            NeedsAtSign = needsAtSign,
            ClassificationAttributeType = classificationAttributeType?.ToDisplayString(),
            IsNullable = paramTypeSymbol.NullableAnnotation == NullableAnnotation.Annotated,
            IsReference = paramTypeSymbol.IsReferenceType,
            IsLogger = !parsingState.FoundLogger && ParserUtilities.IsBaseOrIdentity(paramTypeSymbol, symbols.ILoggerSymbol, _compilation),
            IsException = !parsingState.FoundException && ParserUtilities.IsBaseOrIdentity(paramTypeSymbol, symbols.ExceptionSymbol, _compilation),
            IsLogLevel = !parsingState.FoundLogLevel && SymbolEqualityComparer.Default.Equals(paramTypeSymbol, symbols.LogLevelSymbol),
            IsEnumerable = LogParserUtilities.IsEnumerable(extractedType, symbols),
            ImplementsIConvertible = LogParserUtilities.ImplementsIConvertible(paramTypeSymbol, symbols),
            ImplementsIFormatable = LogParserUtilities.ImplementsIFormatable(paramTypeSymbol, symbols),

            IsRedactorProvider = !parsingState.FoundRedactorProvider &&
                symbols.RedactorProviderSymbol is not null &&
                ParserUtilities.IsBaseOrIdentity(paramTypeSymbol, symbols.RedactorProviderSymbol!, _compilation)
        };

        parsingState.FoundLogger |= lp.IsLogger;
        parsingState.FoundRedactorProvider |= lp.IsRedactorProvider;
        parsingState.FoundException |= lp.IsException;
        parsingState.FoundLogLevel |= lp.IsLogLevel;

        return lp;
    }

    private Location? GetLogMethodAttribute(MethodDeclarationSyntax methodSyntax, SemanticModel sm, SymbolHolder symbols)
    {
        foreach (var mal in methodSyntax.AttributeLists)
        {
            foreach (var methodAttr in mal.Attributes)
            {
                var attrCtor = sm.GetSymbolInfo(methodAttr, _cancellationToken).Symbol;
                if (attrCtor != null && SymbolEqualityComparer.Default.Equals(attrCtor.ContainingType, symbols.LogMethodAttribute))
                {
                    return methodAttr.GetLocation();
                }
            }
        }

        return null;
    }

    private (string? field, IFieldSymbol? secondLoggerField, bool isNullable) FindField(SemanticModel sm, TypeDeclarationSyntax classDec, ITypeSymbol symbol)
    {
        string? field = null;
        bool isNullable = false;

        foreach (var m in classDec.Members)
        {
            if (m is FieldDeclarationSyntax fds)
            {
                foreach (var v in fds.Declaration.Variables)
                {
                    var fs = sm.GetDeclaredSymbol(v, _cancellationToken) as IFieldSymbol;
                    if (fs != null && ParserUtilities.IsBaseOrIdentity(fs.Type, symbol, _compilation))
                    {
                        if (field == null)
                        {
                            field = v.Identifier.Text;
                            isNullable = fs.Type.NullableAnnotation == NullableAnnotation.Annotated;
                        }
                        else
                        {
                            return (null, fs, isNullable);
                        }
                    }
                }
            }
        }

        return (field, null, isNullable);
    }

    private void Diag(DiagnosticDescriptor desc, Location? location, params object?[]? messageArgs)
    {
        _reportDiagnostic(Diagnostic.Create(desc, location, messageArgs));
    }

    private record struct MethodParsingState(
        bool FoundLogger,
        bool FoundRedactorProvider,
        bool FoundException,
        bool FoundLogLevel,
        bool FoundDataClassificationAnnotation,
        bool FoundCustomLogPropertiesProvider);
}
