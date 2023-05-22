// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using Xunit;

namespace Microsoft.Gen.MeteringReports.Test;

/// <summary>
/// Test for <see cref="Generator"/>.
/// </summary>
public class GeneratorTests
{
    [Fact]
    public void GeneratorShouldNotDoAnythingIfGeneralExecutionContextDoesNotHaveClassDeclarationSyntaxReceiver()
    {
        var defaultGeneralExecutionContext = default(GeneratorExecutionContext);
        new MetricDefinitionGenerator().Execute(defaultGeneralExecutionContext);

        Assert.Null(defaultGeneralExecutionContext.SyntaxReceiver);
    }
}
