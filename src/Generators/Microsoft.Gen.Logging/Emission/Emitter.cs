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
    private const string LoggerMessageHelperType = "global::Microsoft.Extensions.Logging.LoggerMessageHelper";

    private readonly StringBuilderPool _sbPool = new();
    private readonly Dictionary<string, string> _classificationMap = [];

    public string Emit(IEnumerable<LoggingType> logTypes, CancellationToken cancellationToken)
    {
        foreach (var lt in logTypes.OrderBy(static lt => lt.Namespace + "." + lt.Name))
        {
            cancellationToken.ThrowIfCancellationRequested();
            GenType(lt);
        }

        return Capture();
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

        GenAttributeClassifications(lt);

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
        // gather all the classification attributes referenced by the logging type
        var classificationAttrs = new HashSet<string>();
        foreach (var lm in lt.Methods)
        {
            foreach (var parameter in lm.Parameters)
            {
                if (parameter.HasProperties)
                {
                    parameter.TraverseParameterPropertiesTransitively((_, property) => classificationAttrs.UnionWith(property.ClassificationAttributeTypes));
                }
                else
                {
                    classificationAttrs.UnionWith(parameter.ClassificationAttributeTypes);
                }
            }
        }

        _classificationMap.Clear();
        foreach (var classificationAttr in classificationAttrs)
        {
            var fieldName = PickUniqueName($"_{EncodeTypeName(classificationAttr)}", lt.AllMembers);
            _classificationMap.Add(classificationAttr, fieldName);

            OutGeneratedCodeAttribute();
            OutLn($"private static readonly Microsoft.Extensions.Compliance.Classification.DataClassification {fieldName} = new {classificationAttr}().Classification;");
            OutLn();
        }
    }
}
