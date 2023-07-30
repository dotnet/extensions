// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Compliance.Redaction;
using Microsoft.Extensions.Compliance.Testing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Telemetry.Enrichment;
using Microsoft.Extensions.Telemetry.Logging;
using Microsoft.Gen.Logging.Parsing;
using Microsoft.Gen.Shared;
using Xunit;

namespace Microsoft.Gen.Logging.Test;

public class AttributeParserTests
{
    [Fact]
    public async Task RandomAttribute()
    {
        var diagnostics = await RunGenerator(@"
                internal static partial class C
                {
                    [LogMethod(0, LogLevel.Debug, ""M {p0}"")]
                    static partial void M(ILogger logger, [Test] string p0);
                }
            ");

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task LegacyAttribute()
    {
        var diagnostics = await RunGenerator(@"
                internal static partial class C
                {
#pragma warning disable CS0618

                    [LoggerMessage(0, LogLevel.Debug, ""M {p0}"")]
                    static partial void M(ILogger logger, [Test] string p0);
                }
            ");

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task DataClassificationAttributeFullName()
    {
        var diagnostics = await RunGenerator(@"
                internal static partial class C
                {
                    [LogMethod(0, LogLevel.Debug, ""M {p0}"")]
                    static partial void M(ILogger logger, IRedactorProvider provider, [PrivateDataAttribute] string p0);
                }
            ");

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task DataClassificationAttributeShortName()
    {
        var diagnostics = await RunGenerator(@"
                internal static partial class C
                {
                    [LogMethod(0, LogLevel.Debug, ""M {p0}"")]
                    static partial void M(ILogger logger, IRedactorProvider provider, [PrivateData] string p0);
                }
            ");

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task MultipleAttributesOnDifferentTopics()
    {
        var diagnostics = await RunGenerator(@"
                internal static partial class C
                {
                    [LogMethod(0, LogLevel.Debug, ""M {p0}"")]
                    static partial void M(ILogger logger, IRedactorProvider provider, [Test][PrivateData] string p0);
                }
            ");

        Assert.Empty(diagnostics);
    }

    [Theory]
    [InlineData("[PrivateData] string")]
    public async Task RedactorProviderIsInTheInstance(string type)
    {
        var diagnostics = await RunGenerator(@$"
            internal partial class TestInstance
            {{
                private readonly ILogger _logger;
                private readonly IRedactorProvider _redactorProvider;

                public TestInstance(ILogger logger, IRedactorProvider redactorProvider)
                {{
                    _logger = logger;
                    _redactorProvider = redactorProvider;
                }}

                [LogMethod(0, LogLevel.Debug, ""M0 {{p0}}"")]
                public partial void M0({type} p0);
            }}");

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task DataClassOnAllTypes()
    {
        var diagnostics = await RunGenerator(@"
                internal static partial class C
                {
                    [LogMethod(0, LogLevel.Debug, ""M {p0}"")]
                    static partial void M(ILogger logger, IRedactorProvider provider, [PrivateData] int p0);
                }
            ");

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task MultipleDataClassificationAttributes()
    {
        var diagnostics = await RunGenerator(@"
                internal static partial class C
                {
                    [LogMethod(0, LogLevel.Debug, ""M {p0}"")]
                    static partial void M(ILogger logger, IRedactorProvider provider, [PrivateData][PrivateData] string p0);
                }
            ");

        Assert.Contains(diagnostics, d => d.Id == DiagDescriptors.MultipleDataClassificationAttributes.Id);
    }

    [Fact]
    public async Task MissingLogger()
    {
        var diagnostics = await RunGenerator(@"
                internal static partial class C
                {
                    [LogMethod(0, LogLevel.Debug, ""M {p0}"")]
                    static partial void M(IRedactorProvider provider, [PrivateData] string p0);
                }
            ");

        _ = Assert.Single(diagnostics);
        Assert.Equal(DiagDescriptors.MissingLoggerParameter.Id, diagnostics[0].Id);
    }

    [Theory]
    [InlineData("[PrivateData] string")]
    public async Task MissingRedactorProviderStaticMethod(string type)
    {
        var diagnostics = await RunGenerator(@$"
            internal static partial class C
            {{
                [LogMethod(0, LogLevel.Debug, ""M {{p0}}"")]
                static partial void M(ILogger logger, {type} p0);
            }}");

        _ = Assert.Single(diagnostics);
        Assert.Equal(DiagDescriptors.MissingRedactorProviderParameter.Id, diagnostics[0].Id);
    }

    [Theory]
    [InlineData("[PrivateData] string")]
    public async Task MissingRedactorProviderInstanceMethod(string type)
    {
        var diagnostics = await RunGenerator(@$"
            internal partial class TestInstance
            {{
                private readonly ILogger _logger;

                public TestInstance(ILogger logger)
                {{
                    _logger = logger;
                }}

                [LogMethod(0, LogLevel.Debug, ""M0 {{p0}}"")]
                public partial void M0({type} p0);
            }}");

        _ = Assert.Single(diagnostics);
        Assert.Equal(DiagDescriptors.MissingRedactorProviderField.Id, diagnostics[0].Id);
    }

    [Fact]
    public async Task MultipleRedactorProviders()
    {
        var diagnostics = await RunGenerator(@"
                internal partial class TestInstance
                {
                    private readonly ILogger _logger;
                    private readonly IRedactorProvider _redactorProvider;
                    private readonly IRedactorProvider _redactorProvider2;

                    public TestInstance(ILogger logger, IRedactorProvider redactorProvider)
                    {
                        _logger = logger;
                        _redactorProvider = redactorProvider;
                    }

                    [LogMethod(0, LogLevel.Debug, ""M0 {p0}"")]
                    public partial void M0([PrivateData] string p0);
                }
            ");

        _ = Assert.Single(diagnostics);
        Assert.Equal(DiagDescriptors.MultipleRedactorProviderFields.Id, diagnostics[0].Id);
    }

    [Fact]
    public async Task MissingDataClassificationAttributeInStatic()
    {
        var diagnostics = await RunGenerator(@"
                internal static partial class C
                {
                    [LogMethod(0, LogLevel.Debug, ""M {p0}"")]
                    static partial void M(ILogger logger, IRedactorProvider provider, string p0);
                }
            ");

        _ = Assert.Single(diagnostics);
        Assert.Equal(DiagDescriptors.MissingDataClassificationParameter.Id, diagnostics[0].Id);
    }

    [Fact]
    public async Task NotMissingDataClassificationAttributeInStatic()
    {
        var diagnostics = await RunGenerator(@"
                internal static partial class C
                {
                    [LogMethod(0, LogLevel.Debug, ""M {p0}"")]
                    static partial void M(ILogger logger, IRedactorProvider provider, [PublicData] string p0);
                }
            ");

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task MissingDataClassificationAttributeInInstance()
    {
        var diagnostics = await RunGenerator(@"
            partial class C
            {
                private ILogger _logger;

                [LogMethod(0, LogLevel.Debug, ""M {p0}"")]
                partial void M(IRedactorProvider provider, string p0);
            }");

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal(DiagDescriptors.MissingDataClassificationParameter.Id, diagnostic.Id);
    }

    [Theory]
    [InlineData("[PrivateData] string")]
    public async Task MethodNotStatic(string type)
    {
        var diagnostics = await RunGenerator(@$"
            internal partial class C
            {{
                [LogMethod(0, LogLevel.Debug, ""M {{p0}}"")]
                partial void M(ILogger logger, IRedactorProvider provider, {type} p0);
            }}");

        _ = Assert.Single(diagnostics);
        Assert.Equal(DiagDescriptors.LoggingMethodShouldBeStatic.Id, diagnostics[0].Id);
    }

    private static async Task<IReadOnlyList<Diagnostic>> RunGenerator(string code)
    {
        var text = $@"
                namespace Test {{
                using Microsoft.Extensions.Compliance.Classification;
                using Microsoft.Extensions.Compliance.Testing;
                using Microsoft.Extensions.Compliance.Redaction;
                using Microsoft.Extensions.Logging;
                using Microsoft.Extensions.Telemetry.Logging;
                {code}
                }}
            ";

        var loggerAssembly = Assembly.GetAssembly(typeof(ILogger));
        var logMethodAssembly = Assembly.GetAssembly(typeof(LogMethodAttribute));
        var enrichmentAssembly = Assembly.GetAssembly(typeof(IEnrichmentPropertyBag));
        var dataClassificationAssembly = Assembly.GetAssembly(typeof(DataClassification));
        var simpleDataClassificationAssembly = Assembly.GetAssembly(typeof(PrivateDataAttribute));
        var redactorProviderAssembly = Assembly.GetAssembly(typeof(IRedactorProvider));
        var refs = new[] { loggerAssembly!, logMethodAssembly!, enrichmentAssembly!, dataClassificationAssembly!, simpleDataClassificationAssembly!, redactorProviderAssembly! };

        var (d, _) = await RoslynTestUtils.RunGenerator(
            new LoggingGenerator(),
            refs,
            new[] { text }).ConfigureAwait(false);

        return d;
    }
}
