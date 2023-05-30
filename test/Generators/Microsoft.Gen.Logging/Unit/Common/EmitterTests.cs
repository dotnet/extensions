// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
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

public class EmitterTests
{
    [Fact]
    public async Task TestEmitter()
    {
        var sources = new List<string>();
        foreach (var file in Directory.GetFiles("TestClasses"))
        {
#if !ROSLYN_4_0_OR_GREATER
            if (file.EndsWith("NamespaceTestExtensions.cs"))
            {
                continue;
            }
#endif

            sources.Add(File.ReadAllText(file));
        }

#if NET6_0_OR_GREATER
        var symbols = new[] { "NET7_0_OR_GREATER", "NET6_0_OR_GREATER", "NET5_0_OR_GREATER" };
#else
        var symbols = new[] { "NET5_0_OR_GREATER" };
#endif

        var (d, r) = await RoslynTestUtils.RunGenerator(
            new LoggingGenerator(),
            new[]
            {
                Assembly.GetAssembly(typeof(ILogger))!,
                Assembly.GetAssembly(typeof(LogMethodAttribute))!,
                Assembly.GetAssembly(typeof(IEnrichmentPropertyBag))!,
                Assembly.GetAssembly(typeof(DataClassification))!,
                Assembly.GetAssembly(typeof(IRedactorProvider))!,
                Assembly.GetAssembly(typeof(PrivateDataAttribute))!,
            },
            sources,
            symbols)
            .ConfigureAwait(false);

        // we need this "Where()" hack because Roslyn 4.0 doesn't recognize #pragma warning disable for generator-produced warnings
        Assert.Empty(d.Where(diag
            => diag.Id != DiagDescriptors.ShouldntMentionExceptionInMessage.Id
            && diag.Id != DiagDescriptors.ShouldntMentionLoggerInMessage.Id
            && diag.Id != DiagDescriptors.ShouldntMentionLogLevelInMessage.Id));

        _ = Assert.Single(r);

        var golden = File.ReadAllText($"GoldenFiles/Microsoft.Gen.Logging/Microsoft.Gen.Logging.LoggingGenerator/Logging.g.cs");
        var result = r[0].SourceText.ToString();
        Assert.Equal(golden, result);
    }
}
