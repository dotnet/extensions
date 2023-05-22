// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Telemetry.Metering;

// Generator emits the code without compilation errors only when
// a class named 'TestClassesNspace' is a 'part' of the namespace 'TestClassesNspace.Metering'.
namespace TestClassesNspace.Metering
{
    [SuppressMessage("Usage", "CA1801:Review unused parameters",
        Justification = "Method body is source generated where the parameters will be used")]
    [SuppressMessage("Readability", "R9A046:Source generated metrics (fast metrics) should be located in 'Metric' class",
        Justification = "Metering generator tests")]
    public static partial class OverlappingNamesTestExtensions
    {
        [Counter(typeof(StrongTypeDimensionsOverlappingNames))]
        public static partial OverlappingNamesCounter CreateOverlappingNamesCounter(Meter meter);

        [Histogram(typeof(StrongTypeDimensionsOverlappingNames))]
        public static partial OverlappingNamesHistogram CreateOverlappingNamesHistogram(Meter meter);
    }

#pragma warning disable SA1402 // File may only contain a single type
    public class StrongTypeDimensionsOverlappingNames
    {
        public string? Dimension1;
        public string? Dimension2;
    }
}

#pragma warning disable SA1403 // File may only contain a single namespace
namespace TestClassesNspace
{
    public class TestClassesNspace
    {
    }
}
