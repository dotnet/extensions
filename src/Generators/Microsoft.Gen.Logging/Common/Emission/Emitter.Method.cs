// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.Gen.Logging.Model;
using Microsoft.Gen.Shared;

namespace Microsoft.Gen.Logging.Emission;

// Stryker disable all

internal sealed partial class Emitter : EmitterBase
{
    [SuppressMessage("Major Code Smell", "S103:Lines should not be too long", Justification = "Long strings are easier to read in this function")]
    private void GenLogMethod(LoggingMethod lm)
    {
        var logPropsDataClasses = GetLogPropertiesAttributes(lm);
        string level = GetLoggerMethodLogLevel(lm);
        string extension = lm.IsExtensionMethod ? "this " : string.Empty;
        string eventName = string.IsNullOrWhiteSpace(lm.EventName) ? $"nameof({lm.Name})" : $"\"{lm.EventName}\"";
        string exceptionArg = GetException(lm);
        (string logger, bool isNullableLogger) = GetLogger(lm);

        OutLn();

        if (!lm.HasXmlDocumentation)
        {
            var l = GetLoggerMethodLogLevelForXmlDocumentation(lm);
            var lvl = string.Empty;
            if (l != null)
            {
                lvl = $" at \"{l}\" level";
            }

            OutLn($"/// <summary>");

            if (!string.IsNullOrEmpty(lm.Message))
            {
                OutLn($"/// Logs \"{EscapeMessageStringForXmlDocumentation(lm.Message)}\" {lvl}.");
            }
            else
            {
                OutLn($"/// Emits a structured log entry{lvl}.");
            }

            OutLn($"/// </summary>");
        }

        OutGeneratedCodeAttribute();

        OutIndent();
        Out($"{lm.Modifiers} void {lm.Name}({extension}");
        GenParameters(lm);
        Out(")\n");

        OutOpenBrace();

        if (isNullableLogger)
        {
            OutLn($"if ({logger} == null)");
            OutOpenBrace();
            OutLn("return;");
            OutCloseBrace();
            OutLn();
        }

        if (!lm.SkipEnabledCheck)
        {
            OutLn($"if (!{logger}.IsEnabled({level}))");
            OutOpenBrace();
            OutLn("return;");
            OutCloseBrace();
            OutLn();
        }

        var parametersToRedact = lm.AllParameters.Where(lp => lp.ClassificationAttributeType != null).ToArray();

        if (parametersToRedact.Length > 0 || logPropsDataClasses.Count > 0)
        {
            (string redactorProvider, bool isNullableRedactorProvider) = GetRedactorProvider(lm);

            var classifications = parametersToRedact
                .Select(static p => p.ClassificationAttributeType)
                .Concat(logPropsDataClasses)
                .Distinct()
                .Where(static p => p != null)
                .Select(static p => p!);

            GenRedactorsFetchingCode(_isRedactorProviderInTheInstance, classifications, redactorProvider, isNullableRedactorProvider);
            OutLn();
        }

        Dictionary<LoggingMethodParameter, int> holderMap = new();

        OutLn($"var _helper = {LogMethodHelperType}.GetHelper();");

        foreach (var p in lm.AllParameters)
        {
            if (!p.HasPropsProvider && !p.HasProperties && p.IsNormalParameter)
            {
                GenHelperAdd(lm, holderMap, p);
            }
        }

        foreach (var p in lm.TemplateParameters)
        {
            if (!holderMap.ContainsKey(p))
            {
                GenHelperAdd(lm, holderMap, p);
            }
        }

        if (!string.IsNullOrEmpty(lm.Message))
        {
            OutLn($"_helper.Add(\"{{OriginalFormat}}\", \"{EscapeMessageString(lm.Message)}\");");
        }

        foreach (var p in lm.AllParameters)
        {
            if (p.HasPropsProvider)
            {
                if (p.OmitParameterName)
                {
                    OutLn($"_helper.ParameterName = string.Empty;");
                }
                else
                {
                    OutLn($"_helper.ParameterName = nameof({p.NameWithAt});");
                }

                OutLn($"{p.LogPropertiesProvider!.ContainingType}.{p.LogPropertiesProvider.MethodName}(_helper, {p.NameWithAt});");
            }
            else if (p.HasProperties)
            {
                OutLn($"_helper.ParameterName = string.Empty;");

#pragma warning disable S1067 // Expressions should not be too complex
                p.TraverseParameterPropertiesTransitively((propertyChain, member) =>
                {
                    var propName = PropertyChainToString(propertyChain, member, "_", omitParameterName: p.OmitParameterName);
                    var accessExpression = PropertyChainToString(propertyChain, member, "?.", nonNullSeparator: ".");

                    var skipNull = p.SkipNullProperties && member.PotentiallyNull
                        ? $"if ({accessExpression} != null) "
                        : string.Empty;

                    if (member.ClassificationAttributeType != null)
                    {
                        var value = $"_{EncodeTypeName(member.ClassificationAttributeType)}Redactor?.Redact(global::System.MemoryExtensions.AsSpan({ConvertPropertyToString(member, accessExpression)}))";

                        if (member.PotentiallyNull)
                        {
                            if (p.SkipNullProperties || accessExpression == value)
                            {
                                OutLn($"{skipNull}_helper.Add(\"{propName}\", {value});");
                            }
                            else
                            {
                                OutLn($"_helper.Add(\"{propName}\", {accessExpression} != null ? {value} : null);");
                            }
                        }
                        else
                        {
                            OutLn($"_helper.Add(\"{propName}\", {value});");
                        }
                    }
                    else
                    {
                        var ts = ShouldStringify(member.Type) ? ConvertPropertyToString(member, accessExpression) : accessExpression;

                        var value = member.IsEnumerable
                            ? $"global::Microsoft.Extensions.Telemetry.Logging.LogMethodHelper.Stringify({accessExpression})"
                            : ts;

                        OutLn($"{skipNull}_helper.Add(\"{propName}\", {value});");
                    }
                });
#pragma warning restore S1067 // Expressions should not be too complex
            }
        }

        OutLn();

        OutLn($"{logger}.Log(");

        Indent();
        OutLn($"{level},");

        if (lm.EventId != null)
        {
            OutLn($"new({lm.EventId}, {eventName}),");
        }
        else
        {
            OutLn($"new(0, {eventName}),");
        }

        OutLn($"_helper,");
        OutLn($"{exceptionArg},");
        OutLn($"__FUNC_{_memberCounter}_{lm.Name});");
        Unindent();

        OutLn();
        OutLn($"{LogMethodHelperType}.ReturnHelper(_helper);");

        OutCloseBrace();

        OutLn();
        OutGeneratedCodeAttribute();
        OutLn($"private static string __FMT_{_memberCounter}_{lm.Name}(global::Microsoft.Extensions.Telemetry.Logging.LogMethodHelper _h, global::System.Exception? _)");
        OutOpenBrace();

        if (GenVariableAssignments(lm, holderMap))
        {
            OutLn($@"return global::System.FormattableString.Invariant($""{EscapeMessageString(lm.Message)}"");");
        }
        else if (string.IsNullOrEmpty(lm.Message))
        {
            OutLn($@"return string.Empty;");
        }
        else
        {
            OutLn($@"return ""{EscapeMessageString(lm.Message)}"";");
        }

        OutCloseBrace();

        OutLn();
        OutGeneratedCodeAttribute();
        OutLn($"private static readonly global::System.Func<global::Microsoft.Extensions.Telemetry.Logging.LogMethodHelper, global::System.Exception?, string>" +
            $" __FUNC_{_memberCounter}_{lm.Name} = new(__FMT_{_memberCounter}_{lm.Name});");

        static bool ShouldStringify(string typeName)
        {
            // well-known system types should not be stringified, since the logger may have special encodings for these
            if (typeName.Contains("."))
            {
                return !typeName.StartsWith("global::System", StringComparison.Ordinal);
            }

            // a primitive type...
            return false;
        }

        static string ConvertToString(LoggingMethodParameter lp, string arg)
        {
            var question = lp.PotentiallyNull ? "?" : string.Empty;
            if (lp.ImplementsIConvertible)
            {
                return $"{arg}{question}.ToString(global::System.Globalization.CultureInfo.InvariantCulture)";
            }
            else if (lp.ImplementsIFormatable)
            {
                return $"{arg}{question}.ToString(null, global::System.Globalization.CultureInfo.InvariantCulture)";
            }

            return $"{arg}{question}.ToString()";
        }

        static string ConvertPropertyToString(LoggingProperty lp, string arg)
        {
            var question = lp.PotentiallyNull ? "?" : string.Empty;
            if (lp.ImplementsIConvertible)
            {
                return $"{arg}{question}.ToString(global::System.Globalization.CultureInfo.InvariantCulture)";
            }
            else if (lp.ImplementsIFormatable)
            {
                return $"{arg}{question}.ToString(null, global::System.Globalization.CultureInfo.InvariantCulture)";
            }

            return $"{arg}{question}.ToString()";
        }

        static string GetException(LoggingMethod lm)
        {
            string exceptionArg = "null";
            foreach (var p in lm.AllParameters)
            {
                if (p.IsException)
                {
                    exceptionArg = p.Name;
                    break;
                }
            }

            return exceptionArg;
        }

        bool GenVariableAssignments(LoggingMethod lm, Dictionary<LoggingMethodParameter, int> holderMap)
        {
            var result = false;

            foreach (var t in lm.TemplateMap)
            {
                int index = 0;
                foreach (var p in lm.TemplateParameters)
                {
                    if (t.Key.Equals(p.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        break;
                    }

                    index++;
                }

                // check for an index that's too big, this can happen in some cases of malformed input
                if (index < lm.TemplateParameters.Count)
                {
                    var parameter = lm.TemplateParameters[index];
                    var atSign = parameter.NeedsAtSign ? "@" : string.Empty;
                    if (parameter.PotentiallyNull)
                    {
                        const string Null = "\"(null)\"";
                        OutLn($"var {atSign}{t.Key} = _h[{holderMap[parameter]}].Value ?? {Null};");
                        result = true;
                    }
                    else
                    {
                        OutLn($"var {atSign}{t.Key} = _h[{holderMap[parameter]}].Value;");
                        result = true;
                    }
                }
            }

            return result;
        }

        static (string name, bool isNullable) GetLogger(LoggingMethod lm)
        {
            string logger = lm.LoggerField;
            bool isNullable = lm.LoggerFieldNullable;

            foreach (var p in lm.AllParameters)
            {
                if (p.IsLogger)
                {
                    logger = p.Name;
                    isNullable = p.IsNullable;
                    break;
                }
            }

            return (logger, isNullable);
        }

        static (string name, bool isNullable) GetRedactorProvider(LoggingMethod lm)
        {
            string redactorProvider = lm.RedactorProviderField;
            bool isNullable = lm.RedactorProviderFieldNullable;

            foreach (var p in lm.AllParameters)
            {
                if (p.IsRedactorProvider)
                {
                    redactorProvider = p.Name;
                    isNullable = p.IsNullable;
                    break;
                }
            }

            return (redactorProvider, isNullable);
        }

        void GenHelperAdd(LoggingMethod lm, Dictionary<LoggingMethodParameter, int> holderMap, LoggingMethodParameter p)
        {
            string key = $"\"{lm.GetParameterNameInTemplate(p)}\"";

            if (p.ClassificationAttributeType != null)
            {
                var dataClassVariableName = EncodeTypeName(p.ClassificationAttributeType);

                OutOpenBrace();
                OutLn($"var _v = {ConvertToString(p, p.NameWithAt)};");
                var value = $"_v != null ? _{dataClassVariableName}Redactor?.Redact(global::System.MemoryExtensions.AsSpan(_v)) : null";
                OutLn($"_helper.Add({key}, {value});");
                OutCloseBrace();
            }
            else
            {
                if (p.IsEnumerable)
                {
                    var value = p.PotentiallyNull
                        ? $"{p.NameWithAt} != null ? global::Microsoft.Extensions.Telemetry.Logging.LogMethodHelper.Stringify({p.NameWithAt}) : null"
                        : $"global::Microsoft.Extensions.Telemetry.Logging.LogMethodHelper.Stringify({p.NameWithAt})";

                    OutLn($"_helper.Add({key}, {value});");
                }
                else
                {
                    var value = ShouldStringify(p.Type)
                        ? ConvertToString(p, p.NameWithAt)
                        : p.NameWithAt;

                    OutLn($"_helper.Add({key}, {value});");
                }
            }

            holderMap.Add(p, holderMap.Count);
        }
    }

    private void GenParameters(LoggingMethod lm)
    {
        OutEnumeration(lm.AllParameters.Select(static p =>
        {
            if (p.Qualifier != null)
            {
                return $"{p.Qualifier} {p.Type} {p.NameWithAt}";
            }

            return $"{p.Type} {p.NameWithAt}";
        }));
    }

    private string PropertyChainToString(
        IEnumerable<LoggingProperty> propertyChain,
        LoggingProperty leafProperty,
        string separator,
        string? nonNullSeparator = null,
        bool omitParameterName = false)
    {
        bool needAts = nonNullSeparator == ".";
        var adjustedNonNullSeparator = nonNullSeparator ?? separator;
        var localStringBuilder = _sbPool.GetStringBuilder();
        try
        {
            int count = 0;
            foreach (var property in propertyChain)
            {
                count++;
                if (omitParameterName && count == 1)
                {
                    continue;
                }

                _ = localStringBuilder
                    .Append(needAts ? property.NameWithAt : property.Name)
                    .Append(property.PotentiallyNull ? separator : adjustedNonNullSeparator);
            }

            // Last item:
            _ = localStringBuilder.Append(needAts ? leafProperty.NameWithAt : leafProperty.Name);

            return localStringBuilder.ToString();
        }
        finally
        {
            _sbPool.ReturnStringBuilder(localStringBuilder);
        }
    }

    private void GenRedactorsFetchingCode(
        bool isRedactorProviderInTheInstance,
        IEnumerable<string> classificationAttributeTypes,
        string redactorProvider,
        bool isNullableRedactorProvider)
    {
        if (isRedactorProviderInTheInstance)
        {
            foreach (var classificationAttributeType in classificationAttributeTypes)
            {
                var dataClassVariableName = EncodeTypeName(classificationAttributeType);

                OutLn($"var _{dataClassVariableName}Redactor = __{dataClassVariableName}Redactor;");
            }
        }
        else
        {
            foreach (var classificationAttributeType in classificationAttributeTypes)
            {
                var classificationVariableName = EncodeTypeName(classificationAttributeType);
                var attrClassificationFieldName = GetAttributeClassification(classificationAttributeType);

                if (isNullableRedactorProvider)
                {
                    OutLn($"var _{classificationVariableName}Redactor = {redactorProvider}?.GetRedactor({attrClassificationFieldName});");
                }
                else
                {
                    OutLn($"var _{classificationVariableName}Redactor = {redactorProvider}.GetRedactor({attrClassificationFieldName});");
                }
            }
        }
    }
}
