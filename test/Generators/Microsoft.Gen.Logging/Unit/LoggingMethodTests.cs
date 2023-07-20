// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Gen.Logging.Model;
using Xunit;

namespace Microsoft.Gen.Logging.Test;

public class LoggingMethodTests
{
    [Fact]
    public void Fields_Should_BeInitialized()
    {
        var instance = new LoggingMethod();
        Assert.Empty(instance.Name);
        Assert.Empty(instance.Message);
        Assert.Empty(instance.Modifiers);
        Assert.Equal("_logger", instance.LoggerField);
        Assert.Equal("_redactorProvider", instance.RedactorProviderField);
    }

    [Fact]
    public void ShouldReturnParameterNameIfNotFoundInMap()
    {
        var p = new LoggingMethodParameter { Name = "paramName" };
        var method = new LoggingMethod();
        Assert.Equal(p.Name, method.GetParameterNameInTemplate(p));
    }

    [Fact]
    public void ShouldReturnNameForParameterFromMap()
    {
        var p = new LoggingMethodParameter { Name = "paramName" };
        var method = new LoggingMethod();
        method.TemplateMap[p.Name] = "Name from the map";

        Assert.Equal(method.TemplateMap[p.Name], method.GetParameterNameInTemplate(p));
    }
}
