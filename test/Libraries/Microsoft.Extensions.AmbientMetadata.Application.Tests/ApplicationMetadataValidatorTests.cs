// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using FluentAssertions;
using Xunit;

namespace Microsoft.Extensions.AmbientMetadata.Test;

public class ApplicationMetadataValidatorTests
{
    [Fact]
    public void Ctor_CreatesAnInstance()
    {
        var act = () => _ = new ApplicationMetadataValidator();

        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_ObjectHasNoIssues_Success()
    {
        var validator = new ApplicationMetadataValidator();
        var result = validator.Validate(
            "model",
            new ApplicationMetadata { ApplicationName = "test", EnvironmentName = "test2" });

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Validate_NullApplicationName_Fails()
    {
        var validator = new ApplicationMetadataValidator();
        var applicationMetadata = new ApplicationMetadata { ApplicationName = null! };

        validator.Validate("model", applicationMetadata).Failed.Should().BeTrue();
    }

    [Fact]
    public void Validate_NullEnvironmentName_Fails()
    {
        var validator = new ApplicationMetadataValidator();
        var applicationMetadata = new ApplicationMetadata { EnvironmentName = null! };

        validator.Validate("model", applicationMetadata).Failed.Should().BeTrue();
    }
}
