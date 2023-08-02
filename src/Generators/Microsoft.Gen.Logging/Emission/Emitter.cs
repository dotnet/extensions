// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Gen.Logging.Model;
using Microsoft.Gen.Shared;

namespace Microsoft.Gen.Logging.Emission;

// Stryker disable all

internal sealed partial class Emitter : EmitterBase
{
    private const string LoggerMessageHelperType = "global::Microsoft.Extensions.Telemetry.Logging.LoggerMessageHelper";

    private readonly StringBuilderPool _sbPool = new();

    public string Emit(IEnumerable<LoggingType> logTypes, CancellationToken cancellationToken)
    {
        foreach (var lt in logTypes.OrderBy(static lt => lt.Namespace + "." + lt.Name))
        {
            cancellationToken.ThrowIfCancellationRequested();
            GenType(lt);
        }

        return Capture();
    }

    private static string GetAttributeClassification(string classificationAttributeType)
    {
        var classificationVariableName = EncodeTypeName(classificationAttributeType);

        return $"_{classificationVariableName}_Classification";
    }

    private void GenType(LoggingType lt)
    {
        if (!string.IsNullOrWhiteSpace(lt.Namespace))
        {
            OutLn();
            OutLn($"namespace {lt.Namespace}");
            OutOpenBrace();
        }

        var parent = lt.Parent;
        var parentTypes = new List<string>();

        // loop until you find top level nested class
        while (parent != null)
        {
            parentTypes.Add($"partial {parent.Keyword} {parent.Name}");
            parent = parent.Parent;
        }

        // write down top level nested class first
        for (int i = parentTypes.Count - 1; i >= 0; i--)
        {
            OutLn(parentTypes[i]);
            OutOpenBrace();
        }

        OutLn($"partial {lt.Keyword} {lt.Name}");
        OutOpenBrace();

        var isRedactionRequired =
            lt.Methods.SelectMany(static lm => lm.Parameters).Any(static lp => lp.ClassificationAttributeType != null)
            || lt.Methods.SelectMany(static lm => GetLogPropertiesAttributes(lm)).Any();

        if (isRedactionRequired)
        {
            GenAttributeClassifications(lt);
        }

        var first = true;
        foreach (LoggingMethod lm in lt.Methods)
        {
            if (first)
            {
                first = false;
            }
            else
            {
                OutLn();
            }

            GenLogMethod(lm);
        }

        OutCloseBrace();

        parent = lt.Parent;
        while (parent != null)
        {
            OutCloseBrace();
            parent = parent.Parent;
        }

        if (!string.IsNullOrWhiteSpace(lt.Namespace))
        {
            OutCloseBrace();
        }
    }

    private void GenAttributeClassifications(LoggingType lt)
    {
        // Generates fields which contain the data clasification associated with each attribute used in the type

        var logPropsDataClasses = lt.Methods.SelectMany(lm => GetLogPropertiesAttributes(lm));
        var classificationAttributeTypes = lt.Methods
            .SelectMany(static lm => lm.Parameters)
            .Where(static lp => lp.ClassificationAttributeType is not null)
            .Select(static lp => lp.ClassificationAttributeType!)
            .Concat(logPropsDataClasses)
            .Distinct();

        foreach (var classificationAttributeType in classificationAttributeTypes.OrderBy(static x => x))
        {
            var attrClassificationFieldName = GetAttributeClassification(classificationAttributeType);
            OutGeneratedCodeAttribute();
            OutLn($"private static readonly Microsoft.Extensions.Compliance.Classification.DataClassification {attrClassificationFieldName} = new {classificationAttributeType}().Classification;");
            OutLn();
        }
    }
}
