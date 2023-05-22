// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Options.Validation;
using Microsoft.Gen.Shared;
using Microsoft.Shared.Data.Validation;
using Xunit;

namespace Microsoft.Gen.OptionsValidation.Test;

public class EmitterTests
{
    [Fact]
    public async Task TestEmitter()
    {
        var sources = new List<string>();
        foreach (var file in Directory.GetFiles("TestClasses"))
        {
#if !ROSLYN_4_0_OR_GREATER
            if (file.EndsWith("Nested.cs") || file.EndsWith("RecordTypes.cs"))
            {
                continue;
            }
#endif

#if NETCOREAPP3_1_OR_GREATER
            sources.Add("#define NETCOREAPP3_1_OR_GREATER\n" + File.ReadAllText(file));
#else
            sources.Add(File.ReadAllText(file));
#endif
        }

        var (d, r) = await RoslynTestUtils.RunGenerator(
            new Generator(),
            new[]
            {
                Assembly.GetAssembly(typeof(RequiredAttribute))!,
                Assembly.GetAssembly(typeof(TimeSpanAttribute))!,
                Assembly.GetAssembly(typeof(OptionsValidatorAttribute))!,
                Assembly.GetAssembly(typeof(IValidateOptions<object>))!,
            },
            sources)
            .ConfigureAwait(false);

        Assert.Empty(d);
        _ = Assert.Single(r);

        var golden = File.ReadAllText($"GoldenFiles/Microsoft.Gen.OptionsValidation/Microsoft.Gen.OptionsValidation.Generator/Validators.g.cs");
        var result = r[0].SourceText.ToString();
        Assert.Equal(golden, result);
    }
}
