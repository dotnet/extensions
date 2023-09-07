// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.Gen.Metrics.Model;

namespace Microsoft.Gen.MetricsReports;

internal readonly record struct ReportedMetricClass(string Name, string RootNamespace, string Constraints, string Modifiers, ReportedMetricMethod[] Methods);

internal readonly record struct ReportedMetricMethod(string MetricName, string Summary, InstrumentKind Kind, HashSet<string> Dimensions, Dictionary<string, string> DimensionsDescriptions);
