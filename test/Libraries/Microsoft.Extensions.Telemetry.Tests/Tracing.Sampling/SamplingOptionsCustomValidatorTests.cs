// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using FluentAssertions;
using Microsoft.Extensions.Telemetry.Tracing.Internal;
using Xunit;

namespace Microsoft.Extensions.Telemetry.Tracing.Test;

public class SamplingOptionsCustomValidatorTests
{
    private static readonly SamplingOptionsCustomValidator _validator = new();

    [Fact]
    public void CorrectOptions_Empty_DefaultsToAlwaysOn()
    {
        var options = new SamplingOptions { };

        _validator.Validate("test", options).Succeeded.Should().Be(true);
    }

    [Fact]
    public void CorrectOptions_AlwaysOn()
    {
        var options = new SamplingOptions
        {
            SamplerType = SamplerType.AlwaysOn
        };

        _validator.Validate("test", options).Succeeded.Should().Be(true);
    }

    [Fact]
    public void CorrectOptions_AlwaysOff()
    {
        var options = new SamplingOptions
        {
            SamplerType = SamplerType.AlwaysOff
        };

        _validator.Validate("test", options).Succeeded.Should().Be(true);
    }

    [Fact]
    public void CorrectOptions_TraceIdRatioBased_DefaultsTo1()
    {
        var options = new SamplingOptions
        {
            SamplerType = SamplerType.TraceIdRatioBased,
            TraceIdRatioBasedSamplerOptions = new TraceIdRatioBasedSamplerOptions { }
        };

        _validator.Validate("test", options).Succeeded.Should().Be(true);
    }

    [Fact]
    public void CorrectOptions_TraceIdRatioBased()
    {
        var options = new SamplingOptions
        {
            SamplerType = SamplerType.TraceIdRatioBased,
            TraceIdRatioBasedSamplerOptions = new TraceIdRatioBasedSamplerOptions
            {
                Probability = 0.42
            }
        };

        _validator.Validate("test", options).Succeeded.Should().Be(true);
    }

    [Fact]
    public void CorrectOptions_ParentBased_DefaultsToAlwaysOn()
    {
        var options = new SamplingOptions
        {
            SamplerType = SamplerType.ParentBased,
            ParentBasedSamplerOptions = new ParentBasedSamplerOptions { }
        };

        _validator.Validate("test", options).Succeeded.Should().Be(true);
    }

    [Fact]
    public void CorrectOptions_ParentBased_AlwaysOn()
    {
        var options = new SamplingOptions
        {
            SamplerType = SamplerType.ParentBased,
            ParentBasedSamplerOptions = new ParentBasedSamplerOptions
            {
                RootSamplerType = SamplerType.AlwaysOn
            }
        };

        _validator.Validate("test", options).Succeeded.Should().Be(true);
    }

    [Fact]
    public void CorrectOptions_ParentBased_AlwaysOff()
    {
        var options = new SamplingOptions
        {
            SamplerType = SamplerType.ParentBased,
            ParentBasedSamplerOptions = new ParentBasedSamplerOptions
            {
                RootSamplerType = SamplerType.AlwaysOff
            }
        };

        _validator.Validate("test", options).Succeeded.Should().Be(true);
    }

    [Fact]
    public void CorrectOptions_ParentBased_TraceIdRatioBased()
    {
        var options = new SamplingOptions
        {
            SamplerType = SamplerType.ParentBased,
            ParentBasedSamplerOptions = new ParentBasedSamplerOptions
            {
                RootSamplerType = SamplerType.TraceIdRatioBased,
                TraceIdRatioBasedSamplerOptions = new TraceIdRatioBasedSamplerOptions
                {
                    Probability = 0.42
                }
            }
        };

        _validator.Validate("test", options).Succeeded.Should().Be(true);
    }

    [Fact]
    public void IncorrectOptions_InvalidSamplerType()
    {
        var options = new SamplingOptions
        {
            SamplerType = (SamplerType)42
        };

        _validator.Validate("test", options).Succeeded.Should().Be(false);
    }

    [Fact]
    public void IncorrectOptions_TraceIdRatioBased_NoOptions()
    {
        var options = new SamplingOptions
        {
            SamplerType = SamplerType.TraceIdRatioBased
        };

        _validator.Validate("test", options).Succeeded.Should().Be(false);
    }

    [Fact]
    public void IncorrectOptions_ParentBased_NoOptions()
    {
        var options = new SamplingOptions
        {
            SamplerType = SamplerType.ParentBased
        };

        _validator.Validate("test", options).Succeeded.Should().Be(false);
    }

    [Fact]
    public void IncorrectOptions_ParentBased_InvalidRootSampler()
    {
        var options = new SamplingOptions
        {
            SamplerType = SamplerType.ParentBased,
            ParentBasedSamplerOptions = new ParentBasedSamplerOptions
            {
                RootSamplerType = SamplerType.ParentBased
            },
        };

        _validator.Validate("test", options).Succeeded.Should().Be(false);
    }

    [Fact]
    public void IncorrectOptions_ParentBased_TraceIdRatioBasedRootSampler_NoOptions()
    {
        var options = new SamplingOptions
        {
            SamplerType = SamplerType.ParentBased,
            ParentBasedSamplerOptions = new ParentBasedSamplerOptions
            {
                RootSamplerType = SamplerType.TraceIdRatioBased
            }
        };

        _validator.Validate("test", options).Succeeded.Should().Be(false);
    }

    [Fact]
    public void IncorrectOptions_ParentBased_TraceIdRatioBasedRootSampler_OptionsInWrongLocation()
    {
        var options = new SamplingOptions
        {
            SamplerType = SamplerType.ParentBased,
            ParentBasedSamplerOptions = new ParentBasedSamplerOptions
            {
                RootSamplerType = SamplerType.TraceIdRatioBased
            },
            TraceIdRatioBasedSamplerOptions = new TraceIdRatioBasedSamplerOptions
            {
                Probability = 0.42
            }
        };

        var validationResult = _validator.Validate("test", options);
        validationResult.Succeeded
            .Should().Be(false);
        validationResult.Failures
            .Should().Contain(str => str.Contains("Trace Id Ratio Based options should be set in Parent Based options"));
    }
}
