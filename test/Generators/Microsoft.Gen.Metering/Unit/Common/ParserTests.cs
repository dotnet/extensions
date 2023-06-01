﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Telemetry.Metering;
using Microsoft.Gen.Shared;
using Xunit;

namespace Microsoft.Gen.Metering.Test;

public partial class ParserTests
{
    [Fact]
    public async Task InvalidMethodName()
    {
        var d = await RunGenerator(@"
            partial class C
            {
                [Counter(""d1"")]
                static partial MetricName1 __M1(Meter meter);
            }");

        _ = Assert.Single(d);
        Assert.Equal(DiagDescriptors.ErrorInvalidMethodName.Id, d[0].Id);
    }

    [Theory]
    [InlineData("Nested.MetricClassName")]
    [InlineData("Nested.Inner.MetricClassName")]
    public async Task InvalidReturnTypeLocation(string returnType)
    {
        var d = await RunGenerator(@$"
            partial class C
            {{
                [Counter(""d1"")]
                static partial {returnType} CreateMetricName(Meter meter);
            }}");

        _ = Assert.Single(d);
        Assert.Equal(DiagDescriptors.ErrorInvalidMethodReturnTypeLocation.Id, d[0].Id);
    }

    [Theory]
    [InlineData("GenericMetricClass<int>")]
    [InlineData("GenericMetricClass<int, string>")]
    public async Task InvalidReturnTypeArity(string returnType)
    {
        var d = await RunGenerator(@$"
            partial class C
            {{
                [Counter(""d1"")]
                static partial {returnType} CreateMetricName(Meter meter);
            }}");

        _ = Assert.Single(d);
        Assert.Equal(DiagDescriptors.ErrorInvalidMethodReturnTypeArity.Id, d[0].Id);
    }

    [Theory]
    [InlineData("void")]
    [InlineData("int")]
    [InlineData("double")]
    [InlineData("object")]
    [InlineData("CustomClass")]
    public async Task InvalidReturnType(string returnType)
    {
        var d = await RunGenerator(@$"
            partial class C
            {{
                class CustomClass {{ }}

                [Counter(""d1"")]
                static partial {returnType} CreateMetricName(Meter meter);
            }}");

        _ = Assert.Single(d);
        Assert.Equal(DiagDescriptors.ErrorInvalidMethodReturnType.Id, d[0].Id);
    }

    [Theory]
    [InlineData("uint")]
    [InlineData("ulong")]
    [InlineData("ushort")]
    [InlineData("System.UInt16")]
    [InlineData("System.UInt32")]
    [InlineData("System.UInt64")]
    [InlineData("bool")]
    [InlineData("System.Boolean")]
    [InlineData("char")]
    [InlineData("System.Char")]
    [InlineData("CustomStruct")]
    public async Task InvalidAttributeGenericType(string genericType)
    {
        var d = await RunGenerator(@$"
            partial class C
            {{
                struct CustomStruct {{ }}

                [Counter<{genericType}>]
                static partial MeteringInstrument CreateMetricInstrument(Meter meter);
            }}");

        _ = Assert.Single(d);
        Assert.Equal(DiagDescriptors.ErrorInvalidAttributeGenericType.Id, d[0].Id);
    }

    [Fact]
    public async Task InvalidStaticDimensionsKeyNames()
    {
        var d = await RunGenerator(@"
            partial class C
            {
                [Counter(""Env&Name"")]
                static partial TestCounter CreateMetricName(Meter meter);
            }");

        _ = Assert.Single(d);
        Assert.Equal(DiagDescriptors.ErrorInvalidDimensionNames.Id, d[0].Id);
    }

    [Fact]
    public async Task InvalidDynamicDimensionsKeyNames()
    {
        var d = await RunGenerator(@"
            partial class C
            {
                [Counter(""Req*Name"")]
                static partial TestCounter CreateMetricName(Meter meter);
            }");

        _ = Assert.Single(d);
        Assert.Equal(DiagDescriptors.ErrorInvalidDimensionNames.Id, d[0].Id);
    }

    [Fact]
    public async Task ValidDimensionsKeyNames()
    {
        var d = await RunGenerator(@"
            partial class C
            {
                [Counter(""Env.Name"", ""clustr:region"", ""Req_Name"", ""Req-Status"")]
                static partial TestCounter CreateMetricName(Meter meter, string env, string region);
            }");

        Assert.Empty(d);
    }

    [Fact]
    public async Task ValidGenericAttribute()
    {
        var d = await RunGenerator(@"
            partial class C
            {
                [Counter<int>(""d1"")]
                static partial TestCounter CreateTestCounter(Meter meter);
            }");

        Assert.Empty(d);
    }

#if ROSLYN_4_0_OR_GREATER
    [Fact]
    public async Task NotPartialMethod()
    {
        var d = await RunGenerator(@"
            partial class C
            {
                [Counter(""d1"")]
                static MetricName1 CreateMetricName(Meter meter);
            }");

        _ = Assert.Single(d);
        Assert.Equal(DiagDescriptors.ErrorNotPartialMethod.Id, d[0].Id);
    }
#endif

    [Fact]
    public async Task NotStaticMethod()
    {
        var d = await RunGenerator(@"
            partial class C
            {
                [Counter(""d1"")]
                partial MetricName1 CreateMetricName(Meter meter);
            }");

        _ = Assert.Single(d);
        Assert.Equal(DiagDescriptors.ErrorNotStaticMethod.Id, d[0].Id);
    }

    [Fact]
    public async Task MetricNameStartingLowercaseChar()
    {
        var d = await RunGenerator(@"
            partial class C
            {
                [Counter(""d1"")]
                static partial myMetric CreateMetricName(Meter meter);
            }");

        _ = Assert.Single(d);
        Assert.Equal(DiagDescriptors.ErrorInvalidMetricName.Id, d[0].Id);
    }

    [Fact]
    public async Task MetricNameStartingWithNonSymbol()
    {
        var d = await RunGenerator(@"
            partial class C
            {
                [Counter(""d1"")]
                static partial _Metric CreateMetricName(Meter meter);
            }");

        _ = Assert.Single(d);
        Assert.Equal(DiagDescriptors.ErrorInvalidMetricName.Id, d[0].Id);
    }

    [Fact]
    public async Task MethodIsGeneric()
    {
        var d = await RunGenerator(@"
            partial class C
            {
                [Counter(""d1"")]
                static partial MetricName1 CreateMetricName<T>(Meter meter);
            }");

        _ = Assert.Single(d);
        Assert.Equal(DiagDescriptors.ErrorMethodIsGeneric.Id, d[0].Id);
    }

    [Fact]
    public async Task InvalidParameterName()
    {
        var d = await RunGenerator(@"
            partial class C
            {
                [Counter(""d1"")]
                static partial MetricName1 CreateMetricName(Meter _meter);
            }");

        _ = Assert.Single(d);
        Assert.Equal(DiagDescriptors.ErrorInvalidParameterName.Id, d[0].Id);
    }

    [Fact]
    public async Task NullDimensionNames()
    {
        var d = await RunGenerator(@"
            partial class C
            {
                [Counter(null)]
                static partial MetricName1 CreateMetricName(Meter meter);
            }");

        Assert.Empty(d);
    }

    [Fact]
    public async Task NullMetricNameParameter()
    {
        var d = await RunGenerator(@"
            partial class C
            {
                [Counter(Name = null)]
                static partial MetricName1 CreateMetricName(Meter meter);
            }");

        Assert.Empty(d);
    }

    [Fact]
    public async Task ValidParameterChecks()
    {
        var d = await RunGenerator(@"
            internal class MetricConstants
            {
                public const string Env = ""Env.Name"";
                public const string Region = ""region"";
                public const string RequestName = ""requestName"";
                public const string RequestStatus = ""requestStatus"";
            }

            partial class C
            {
                [Counter(MetricConstants.Env, Name = ""myMetricName"")]
                static partial MetricName1 CreateMetric(Meter meter);

                [Counter(MetricConstants.Env, MetricConstants.Region)]
                static partial MetricName2 CreateMetric2(Meter meter);

                [Counter(MetricConstants.Env, MetricConstants.Region, MetricConstants.RequestName, MetricConstants.RequestStatus)]
                static partial MetricName3 CreateMetric3(Meter meter);

                [Counter(MetricConstants.Env, MetricName = @""MetricType\\Standard"")]
                static partial MetricName4 CreateMetric4(Meter meter);

                [Counter(MetricConstants.Env, MetricName = @""MetricType\Custom"")]
                static partial MetricName5 CreateMetric5(Meter meter);
            }");

        Assert.Empty(d);
    }

    [Fact]
    public async Task ValidExtensionMethodsChecks()
    {
        var d = await RunGenerator(@"
            internal class MetricConstants
            {
                public const string Env = ""Env.Name"";
                public const string Region = ""region"";
                public const string RequestName = ""requestName"";
                public const string RequestStatus = ""requestStatus"";
            }

            static partial class C
            {
                [Counter(MetricConstants.Env, Name = ""myMetricName"")]
                static partial MetricName1 CreateMetric(this Meter meter);

                [Counter(MetricConstants.Env, MetricConstants.Region)]
                static partial MetricName2 CreateMetric2(this Meter meter);

                [Counter(MetricConstants.Env, MetricConstants.Region, MetricConstants.RequestName, MetricConstants.RequestStatus)]
                static partial MetricName3 CreateMetric3(this Meter meter);

                [Counter(MetricConstants.Env, MetricName = @""MetricType\\Standard"")]
                static partial MetricName4 CreateMetric4(this Meter meter);

                [Counter(MetricConstants.Env, MetricName = @""MetricType\Custom"")]
                static partial MetricName5 CreateMetric5(this Meter meter);
            }");

        Assert.Empty(d);
    }

    [Fact]
    public async Task ExistingMetricName()
    {
        var d = await RunGenerator(@"
            partial class C
            {
                [Counter(""d1"")]
                static partial MetricName1 CreateMetricName(Meter meter);

                [Counter(""d2"")]
                static partial MetricName1 CreateMetricWithSameNameAgain(Meter meter);
            }");

        _ = Assert.Single(d);
        Assert.Equal(DiagDescriptors.ErrorMetricNameReuse.Id, d[0].Id);
    }

    [Fact]
    public async Task NestedType()
    {
        var d = await RunGenerator(@"
            partial class C
            {
                public partial class Nested
                {
                    [Counter(""d1"")]
                    static partial MetricName1 CreateMetricName(Meter meter);
                }
            }");

        Assert.Empty(d);
    }

#if ROSLYN_4_0_OR_GREATER
    [Fact]
    public async Task FileScopedNamespace()
    {
        var d = await RunGenerator(@"
            namespace Test;

            public partial class C
            {
                [Counter(""d1"")]
                static partial MetricName1 CreateMetricName(Meter meter);
            }", inNamespace: false);

        Assert.Empty(d);
    }
#endif

    [Theory]
    [InlineData("")]
    [InlineData("string s")]
    [InlineData("int a, string s")]
    public async Task MissingMeterObject(string args)
    {
        var d = await RunGenerator(@$"
            partial class C
            {{
                [Counter(""d1"")]
                static partial MetricName1 CreateMetric({args});
            }}");

        var diag = Assert.Single(d);
        Assert.Equal(DiagDescriptors.ErrorMissingMeter.Id, diag.Id);
    }

    [Fact]
    public async Task MeterIsNotFirst()
    {
        var d = await RunGenerator(@"
            partial class C
            {
                [Counter(""d1"")]
                static partial MetricName1 CreateMetric(string s, Meter meter);
            }");

        var diag = Assert.Single(d);
        Assert.Equal(DiagDescriptors.ErrorMissingMeter.Id, diag.Id);
    }

    [Fact]
    public async Task InvalidMethodBody()
    {
        var d = await RunGenerator(@"
            partial class C
            {
                [Counter(""d1"")]
                static partial MetricName1 CreateMetricName(Meter meter)
                {}
            }");

        _ = Assert.Single(d);
        Assert.Equal(DiagDescriptors.ErrorMethodHasBody.Id, d[0].Id);
    }

    [Fact]
    public async Task SemanticProblems()
    {
        var d = await RunGenerator(@"
            partial class C
            {
                [Counter(""d1"")]

                [Histogram(""d1"", ""d2"")]

                [Gauge(""d1"", ""d2"")]

                [Counter(""d1"")]
                static partial 1Metric CreateMetricName(Meter meter);

                [CounterUnknown(""Unknown"")]
                [Counter()]
                static partial NewMetric CreateNewMetric(Meter meter);

                [Fact]
                static partial NewMetric1 CreateNewMetric1(Meter meter)
                {}

                // badly formatted
                [Counter(""d1"")]
                static partial Metric&Name1 Metric&Name1(Meter meter);

                // bogus parameter type
                [Counter]
                static partial Metric CreateMetric(XIMeter meter);

                // missing parameter name
                [Counter]
                static partial Metric2 CreateMetric2(Meter);

                // attribute applied to something other than method
                [Counter]
                int x;
            }");

        Assert.Empty(d);
    }

    [Fact]
    public async Task MissingMeterType()
    {
        var d = await RunGenerator(@"
            namespace Microsoft.Extensions.Telemetry.Metering
            {
                public sealed class CounterAttribute : System.Attribute {}
                public sealed class HistogramAttribute : System.Attribute {}
            }
            partial class C
            {
                [Microsoft.Extensions.Telemetry.Metering.Counter]
                static partial MetricName1 CreateMetricName(Meter meter);
            }",
            wrap: false,
            inNamespace: false,
            includeBaseReferences: true,
            includeMeterReferences: false);

        Assert.Empty(d);
    }

    [Fact]
    public async Task MissingCounterAttributeType()
    {
        var d = await RunGenerator(@"
            namespace System.Diagnostics.Metrics
            {
                public class Meter {}
            }
            namespace Microsoft.Extensions.Telemetry.Metering
            {
                public class HistogramAttribute : System.Attribute {} 
            }
            partial class C
            {
                [Microsoft.Extensions.Telemetry.Metering.Histogram]
                static partial MetricName1 CreateMetricName(Meter meter);
            }",
            wrap: false,
            includeBaseReferences: true,
            includeMeterReferences: false);

        Assert.Empty(d);
    }

    [Fact]
    public async Task MissingHistogramAttributeType()
    {
        var d = await RunGenerator(@"
            namespace System.Diagnostics.Metrics
            {
                public class Meter {}
            }
            namespace Microsoft.Extensions.Telemetry.Metering
            {
                public class CounterAttribute : System.Attribute {}
            }
            partial class C
            {
                [Microsoft.Extensions.Telemetry.Metering.Counter]
                static partial MetricName1 CreateMetricName(Meter meter);
            }",
            wrap: false,
            includeBaseReferences: true,
            includeMeterReferences: false);

        Assert.Empty(d);
    }

    [Fact]
    public async Task Cancellation()
    {
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            _ = await RunGenerator(@"
                partial class C
                {
                    [Counter(""d1"")]
                    static partial MetricName1 CreateMetricName(Meter meter);
                }",
                cancellationToken: new CancellationToken(true)));
    }

    [Fact]
    public async Task ContainingClassIsInNestedNamespace()
    {
        var d = await RunGenerator(@"
            namespace Nested
            {
                partial class C
                {
                    [Counter(""d1"")]
                    static partial MetricName1 CreateMetricName(Meter meter);
                }
            }");

        Assert.Empty(d);
    }

    [Fact]
    public async Task ContainingClassHasTypeParameter()
    {
        var d = await RunGenerator(@"
            partial class C<T>
            {
                [Counter(""d1"")]
                static partial MetricName1 CreateMetricName(Meter meter);
            }");

        Assert.Empty(d);
    }

    [Fact]
    public async Task MeterTypeIsConvertableToIMeter()
    {
        var d = await RunGenerator(@"
            partial class C
            {
                [Counter(""d1"")]
                static partial MetricName1 CreateMetricName(Meter<string> meter);
            }");

        Assert.Empty(d);
    }

    [Fact]
    public async Task GaugeNotSupported()
    {
        var d = await RunGenerator(@"
            partial class C
            {
                [Gauge(""d1"")]
                static partial NotSupportedGauge CreateGauge(Meter meter);
            }");

        _ = Assert.Single(d);
        Assert.Equal(DiagDescriptors.ErrorGaugeNotSupported.Id, d[0].Id);
    }

    [Fact]
    public async Task DimensionIsDocumentedCounter()
    {
        var d = await RunGenerator(@"
            partial class C
            {
                /// <summary>
                /// InClassDim description.
                /// </summary>
                private const string InClassDimensionName = ""InClassDim"";
                [Counter(InClassDimensionName)]
                static partial TestCounter CreateTestCounter(Meter meter);
            }");

        Assert.Empty(d);
    }

    [Fact]
    public async Task DimensionIsDocumentedHistogram()
    {
        var d = await RunGenerator(@"
            partial class C
            {
                /// <summary>
                /// InClassDim description.
                /// </summary>
                private const string InClassDimensionName = ""InClassDim"";
                [Histogram(InClassDimensionName)]
                static partial TestHistogram CreateTestHistogram(Meter meter);
            }");

        Assert.Empty(d);
    }

    [Fact]
    public async Task CounterIsDocumented()
    {
        var d = await RunGenerator(@"
            partial class C
            {
                /// <summary>
                /// TestCounter description.
                /// </summary>
                [Counter]
                static partial TestCounter CreateTestCounter(Meter meter);
            }");

        Assert.Empty(d);
    }

    [Fact]
    public async Task HistogramIsDocumented()
    {
        var d = await RunGenerator(@"
            partial class C
            {
                /// <summary>
                /// TestHistogram description.
                /// </summary>
                [Histogram]
                static partial TestHistogram CreateTestHistogram(Meter meter);
            }");

        Assert.Empty(d);
    }

    [Fact]
    public async Task CounterIsNotProperlyDocumented()
    {
        var d = await RunGenerator(@"
            partial class C
            {
                /// <summary>
                /// TestCounter description.
                /// < /summary>
                [Counter]
                static partial TestCounter CreateTestCounter(Meter meter);
            }");

        Assert.Single(d);
        Assert.Equal(DiagDescriptors.ErrorXmlNotLoadedCorrectly.Id, d[0].Id);
    }

    [Fact]
    public async Task HistogramIsNotXmlDocumented()
    {
        var d = await RunGenerator(@"
            partial class C
            {
                /// no xml tags.
                [Histogram]
                static partial TestHistogram CreateTestHistogram(Meter meter);
            }");

        Assert.Empty(d);
    }

    [Fact]
    public async Task HistogramHasNoSummaryInXmlComment()
    {
        var d = await RunGenerator(@"
            partial class C
            {
                /// <remarks>
                /// TestHistogram remarks.
                /// </remarks>
                [Histogram]
                static partial TestHistogram CreateTestHistogram(Meter meter);
            }");

        Assert.Empty(d);
    }

    [Fact]
    public async Task IMeterTypeParameter()
    {
        var d = await RunGenerator(@"
            partial class C
            {
                [Histogram]
                static partial TestHistogram CreateTestHistogram(IMeter meter);
            }");

        Assert.Empty(d);
    }

    private static async Task<IReadOnlyList<Diagnostic>> RunGenerator(
        string code,
        bool wrap = true,
        bool inNamespace = true,
        bool includeBaseReferences = true,
        bool includeMeterReferences = true,
        CancellationToken cancellationToken = default)
    {
        var text = code;
        if (wrap)
        {
            var nspaceStart = "namespace Test {";
            var nspaceEnd = "}";
            if (!inNamespace)
            {
                nspaceStart = "";
                nspaceEnd = "";
            }

            text = $@"
                    {nspaceStart}
                    using Microsoft.Extensions.Telemetry.Metering;
                    using System.Diagnostics.Metrics;
                    {code}
                    {nspaceEnd}
                ";
        }

        Assembly[]? refs = null;
        if (includeMeterReferences)
        {
            refs = new[]
            {
                Assembly.GetAssembly(typeof(Meter))!,
                Assembly.GetAssembly(typeof(CounterAttribute))!,
                Assembly.GetAssembly(typeof(HistogramAttribute))!,
                Assembly.GetAssembly(typeof(GaugeAttribute))!,
            };
        }

        var (d, _) = await RoslynTestUtils.RunGenerator(
            new MeteringGenerator(),
            refs,
            new[] { text },
            includeBaseReferences: includeBaseReferences,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return d;
    }
}
