// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.AI.Evaluation.Reporting.Formats.Html;
using Microsoft.TestUtilities;
using Xunit;

namespace Microsoft.Extensions.AI.Evaluation.Reporting.Tests;

public class EmbeddingTests
{
    [ConditionalFact]
    public void CIBuildsMustIncludeEmbeddedHTML()
    {
        // TF_BUILD should be set in our CI pipeline
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("TF_BUILD")))
        {
            throw new SkipTestException("Skipping test because it is not running in CI");
        }

        Assert.NotEmpty(HtmlReportWriter.HtmlTemplateBefore);
        Assert.NotEmpty(HtmlReportWriter.HtmlTemplateAfter);
    }
}
