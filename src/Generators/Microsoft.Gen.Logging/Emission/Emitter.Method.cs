// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Gen.Logging.Model;
using Microsoft.Gen.Shared;

namespace Microsoft.Gen.Logging.Emission;

// Stryker disable all

internal sealed partial class Emitter : EmitterBase
{
    private void GenLogMethod(LoggingMethod lm)
    {
        var logPropsDataClasses = GetLogPropertiesAttributes(lm);
        string level = GetLoggerMethodLogLevel(lm);
        string extension = lm.IsExtensionMethod ? "this " : string.Empty;
        string eventName = string.IsNullOrWhiteSpace(lm.EventName) ? $"nameof({lm.Name})" : $"\"{lm.EventName}\"";
        (string exceptionArg, string exceptionLambdaName) = GetException(lm);
        (string logger, bool isNullableLogger) = GetLogger(lm);

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
                OutLn($"/// Logs \"{EscapeMessageStringForXmlDocumentation(lm.Message)}\"{lvl}.");
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

        var stateName = PickUniqueName("state", lm.Parameters.Select(p => p.Name));

        OutLn($"var {stateName} = {LoggerMessageHelperType}.ThreadLocalState;");
        GenPropertyLoads(lm, stateName, out int numReservedUnclassifiedTags, out int numReservedClassifiedTags);

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

        OutLn($"{stateName},");
        OutLn($"{exceptionArg},");

        var lambdaStateName = PickUniqueName("s", lm.TemplateToParameterName.Select(kvp => kvp.Key));

        OutLn($"static ({lambdaStateName}, {exceptionLambdaName}) =>");
        OutOpenBrace();

        if (GenVariableAssignments(lm, lambdaStateName, numReservedUnclassifiedTags, numReservedClassifiedTags))
        {
            var template = EscapeMessageString(lm.Message);
            template = AddAtSymbolsToTemplates(template, lm.Parameters);
            OutLn($@"return global::System.FormattableString.Invariant($""{template}"");");
        }
        else if (string.IsNullOrEmpty(lm.Message))
        {
            OutLn($@"return string.Empty;");
        }
        else
        {
            OutLn($@"return ""{EscapeMessageString(lm.Message)}"";");
        }

        OutCloseBraceWithExtra(");");
        Unindent();

        OutLn();
        OutLn($"{stateName}.Clear();");
        OutCloseBrace();

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
            else if (lp.ImplementsIFormattable)
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
            else if (lp.ImplementsIFormattable)
            {
                return $"{arg}{question}.ToString(null, global::System.Globalization.CultureInfo.InvariantCulture)";
            }

            return $"{arg}{question}.ToString()";
        }

        static (string exceptionArg, string exceptionLambdaArg) GetException(LoggingMethod lm)
        {
            string exceptionArg = "null";
            string exceptionLambdaArg = "_";

            foreach (var p in lm.Parameters)
            {
                if (p.IsException)
                {
                    exceptionArg = p.Name;

                    if (p.UsedAsTemplate)
                    {
                        exceptionLambdaArg = lm.GetParameterNameInTemplate(p);
                    }

                    break;
                }
            }

            return (exceptionArg, exceptionLambdaArg);
        }

        static bool NeedsASlot(LoggingMethodParameter p)
        {
            if (p.UsedAsTemplate && !p.IsException)
            {
                return true;
            }

            return p.IsNormalParameter && !p.HasTagProvider && !p.HasProperties;
        }

        void GenPropertyLoads(LoggingMethod lm, string stateName, out int numReservedUnclassifiedTags, out int numReservedClassifiedTags)
        {
            int numUnclassifiedTags = 0;
            int numClassifiedTags = 0;

            foreach (var p in lm.Parameters)
            {
                if (NeedsASlot(p))
                {
                    if (p.HasDataClassification)
                    {
                        numClassifiedTags++;
                    }
                    else
                    {
                        numUnclassifiedTags++;
                    }
                }

                if (p.HasProperties && !p.SkipNullProperties)
                {
                    p.TraverseParameterPropertiesTransitively((_, member) =>
                    {
                        if (member.HasDataClassification)
                        {
                            numClassifiedTags++;
                        }
                        else
                        {
                            numUnclassifiedTags++;
                        }
                    });
                }
            }

            if (!string.IsNullOrEmpty(lm.Message))
            {
                numUnclassifiedTags++;
            }

            numReservedUnclassifiedTags = numUnclassifiedTags;
            numReservedClassifiedTags = numClassifiedTags;

            if (numReservedUnclassifiedTags > 0)
            {
                OutLn();
                OutLn($"_ = {stateName}.ReserveTagSpace({numReservedUnclassifiedTags});");
                int count = numReservedUnclassifiedTags;
                foreach (var p in lm.Parameters)
                {
                    if (NeedsASlot(p) && !p.HasDataClassification)
                    {
                        var key = $"\"{lm.GetParameterNameInTemplate(p)}\"";
                        string value;

                        if (p.IsEnumerable)
                        {
                            value = p.PotentiallyNull
                                ? $"{p.NameWithAt} != null ? {LoggerMessageHelperType}.Stringify({p.NameWithAt}) : null"
                                : $"{LoggerMessageHelperType}.Stringify({p.NameWithAt})";
                        }
                        else
                        {
                            value = ShouldStringify(p.Type)
                                ? ConvertToString(p, p.NameWithAt)
                                : p.NameWithAt;
                        }

                        OutLn($"{stateName}.TagArray[{--count}] = new({key}, {value});");
                    }
                }

                foreach (var p in lm.Parameters)
                {
                    if (p.HasProperties && !p.SkipNullProperties)
                    {
                        p.TraverseParameterPropertiesTransitively((propertyChain, member) =>
                        {
                            if (!member.HasDataClassification)
                            {
                                var propName = PropertyChainToString(propertyChain, member, "_", omitParameterName: p.OmitParameterName);
                                var accessExpression = PropertyChainToString(propertyChain, member, "?.", nonNullSeparator: ".");

                                var ts = ShouldStringify(member.Type)
                                    ? ConvertPropertyToString(member, accessExpression)
                                    : accessExpression;

                                var value = member.IsEnumerable
                                    ? $"{LoggerMessageHelperType}.Stringify({accessExpression})"
                                    : ts;

                                OutLn($"{stateName}.TagArray[{--count}] = new(\"{propName}\", {value});");
                            }
                        });
                    }
                }

                if (!string.IsNullOrEmpty(lm.Message))
                {
                    OutLn($"{stateName}.TagArray[{--count}] = new(\"{{OriginalFormat}}\", \"{EscapeMessageString(lm.Message)}\");");
                }
            }

            if (numReservedClassifiedTags > 0)
            {
                OutLn();
                OutLn($"_ = {stateName}.ReserveClassifiedTagSpace({numReservedClassifiedTags});");
                int count = numReservedClassifiedTags;
                foreach (var p in lm.Parameters)
                {
                    if (NeedsASlot(p) && p.HasDataClassification)
                    {
                        var key = $"\"{lm.GetParameterNameInTemplate(p)}\"";
                        var classification = $"_{EncodeTypeName(p.ClassificationAttributeType!)}_Classification";

                        var value = ShouldStringify(p.Type)
                            ? ConvertToString(p, p.NameWithAt)
                            : p.NameWithAt;

                        OutLn($"{stateName}.ClassifiedTagArray[{--count}] = new({key}, {value}, {classification});");
                    }
                }

                foreach (var p in lm.Parameters)
                {
                    if (p.HasProperties && !p.SkipNullProperties)
                    {
                        p.TraverseParameterPropertiesTransitively((propertyChain, member) =>
                        {
                            if (member.HasDataClassification)
                            {
                                var propName = PropertyChainToString(propertyChain, member, "_", omitParameterName: p.OmitParameterName);
                                var accessExpression = PropertyChainToString(propertyChain, member, "?.", nonNullSeparator: ".");

                                var value = ConvertPropertyToString(member, accessExpression);
                                var classification = $"_{EncodeTypeName(member.ClassificationAttributeType!)}_Classification";

                                OutLn($"{stateName}.ClassifiedTagArray[{--count}] = new(\"{propName}\", {value}, {classification});");
                            }
                        });
                    }
                }
            }

            foreach (var p in lm.Parameters)
            {
                if (p.HasProperties && p.SkipNullProperties)
                {
                    p.TraverseParameterPropertiesTransitively((propertyChain, member) =>
                    {
                        if (!member.HasDataClassification)
                        {
                            var propName = PropertyChainToString(propertyChain, member, "_", omitParameterName: p.OmitParameterName);
                            var accessExpression = PropertyChainToString(propertyChain, member, "?.", nonNullSeparator: ".");

                            var ts = ShouldStringify(member.Type)
                                ? ConvertPropertyToString(member, accessExpression)
                                : accessExpression;

                            var value = member.IsEnumerable ? $"{LoggerMessageHelperType}.Stringify({accessExpression})" : ts;

                            var skipNull = member.PotentiallyNull && !member.IsEnumerable
                                ? $"if ({value} != null) "
                                : string.Empty;

                            OutLn($"{skipNull}{stateName}.AddTag(\"{propName}\", {value});");
                        }
                        else
                        {
                            var propName = PropertyChainToString(propertyChain, member, "_", omitParameterName: p.OmitParameterName);
                            var accessExpression = PropertyChainToString(propertyChain, member, "?.", nonNullSeparator: ".");

                            var value = ConvertPropertyToString(member, accessExpression);
                            var classification = $"_{EncodeTypeName(member.ClassificationAttributeType!)}_Classification";

                            var skipNull = member.PotentiallyNull
                                ? $"if ({value} != null) "
                                : string.Empty;

                            OutLn($"{skipNull}{stateName}.AddClassifiedTag(\"{propName}\", {value}, {classification});");
                        }
                    });
                }

                if (p.HasTagProvider)
                {
                    if (p.OmitParameterName)
                    {
                        OutLn($"{stateName}.TagNamePrefix = string.Empty;");
                    }
                    else
                    {
                        OutLn($"{stateName}.TagNamePrefix = nameof({p.NameWithAt});");
                    }

                    OutLn($"{p.TagProvider!.ContainingType}.{p.TagProvider.MethodName}({stateName}, {p.NameWithAt});");
                }
            }
        }

        bool GenVariableAssignments(LoggingMethod lm, string lambdaStateName, int numReservedUnclassifiedTags, int numReservedClassifiedTags)
        {
            bool generatedAssignments = false;

            int index = numReservedUnclassifiedTags - 1;
            foreach (var p in lm.Parameters)
            {
                if (NeedsASlot(p) && !p.HasDataClassification)
                {
                    if (p.UsedAsTemplate)
                    {
                        var key = lm.GetParameterNameInTemplate(p);

                        var atSign = p.NeedsAtSign ? "@" : string.Empty;
                        if (p.PotentiallyNull)
                        {
                            const string Null = "\"(null)\"";
                            OutLn($"var {atSign}{key} = {lambdaStateName}.TagArray[{index}].Value ?? {Null};");
                        }
                        else
                        {
                            OutLn($"var {atSign}{key} = {lambdaStateName}.TagArray[{index}].Value;");
                        }

                        generatedAssignments = true;
                    }

                    index--;
                }
            }

            index = numReservedClassifiedTags - 1;
            foreach (var p in lm.Parameters)
            {
                if (NeedsASlot(p) && p.HasDataClassification)
                {
                    if (p.UsedAsTemplate)
                    {
                        var key = lm.GetParameterNameInTemplate(p);

                        var atSign = p.NeedsAtSign ? "@" : string.Empty;
                        if (p.PotentiallyNull)
                        {
                            const string Null = "\"(null)\"";
                            OutLn($"var {atSign}{key} = {lambdaStateName}.RedactedTagArray[{index}].Value ?? {Null};");
                        }
                        else
                        {
                            OutLn($"var {atSign}{key} = {lambdaStateName}.RedactedTagArray[{index}].Value;");
                        }

                        generatedAssignments = true;
                    }

                    index--;
                }
            }

            return generatedAssignments;
        }

        static (string name, bool isNullable) GetLogger(LoggingMethod lm)
        {
            string logger = lm.LoggerField;
            bool isNullable = lm.LoggerFieldNullable;

            foreach (var p in lm.Parameters)
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
    }

    private string AddAtSymbolsToTemplates(string template, IEnumerable<LoggingMethodParameter> parameters)
    {
        StringBuilder? stringBuilder = null;
        foreach (var item in parameters.Where(p => p.UsedAsTemplate))
        {
            if (!item.NeedsAtSign)
            {
                continue;
            }

            if (stringBuilder is null)
            {
                stringBuilder = _sbPool.GetStringBuilder();
                _ = stringBuilder.Append(template);
            }

            _ = stringBuilder.Replace(item.Name, item.NameWithAt);
        }

        var result = stringBuilder is null
            ? template
            : stringBuilder.ToString();

        if (stringBuilder != null)
        {
            _sbPool.ReturnStringBuilder(stringBuilder);
        }

        return result;
    }

    private void GenParameters(LoggingMethod lm)
    {
        OutEnumeration(lm.Parameters.Select(static p =>
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
}
