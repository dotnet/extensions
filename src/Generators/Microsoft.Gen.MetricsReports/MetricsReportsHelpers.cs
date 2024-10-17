// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Gen.Metrics.Model;

namespace Microsoft.Gen.MetricsReports;
internal static class MetricsReportsHelpers
{
    internal static ReportedMetricClass[] MapToCommonModel(IReadOnlyList<MetricType> meteringClasses, string? rootNamespace)
    {
        var reportedMetrics = meteringClasses
            .Select(meteringClass => new ReportedMetricClass(
                Name: meteringClass.Name,
                RootNamespace: rootNamespace ?? meteringClass.Namespace,
                Constraints: meteringClass.Constraints,
                Modifiers: meteringClass.Modifiers,
                Methods: meteringClass.Methods.Select(meteringMethod => new ReportedMetricMethod(
                    MetricName: meteringMethod.MetricName ?? "(Missing Name)",
                    Summary: meteringMethod.XmlDefinition ?? "(Missing Summary)",
                    Kind: meteringMethod.InstrumentKind,
                    Dimensions: meteringMethod.TagKeys,
                    DimensionsDescriptions: meteringMethod.TagDescriptionDictionary))
                .ToArray()));

        return reportedMetrics.ToArray();
    }
}
