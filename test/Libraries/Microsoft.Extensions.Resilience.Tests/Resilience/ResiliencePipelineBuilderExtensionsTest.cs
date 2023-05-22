// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Resilience.Internal;
using Microsoft.Extensions.Resilience.Test.Helpers;
using Moq;

namespace Microsoft.Extensions.Resilience.Test;

public partial class ResiliencePipelineBuilderExtensionsTest : ResilienceTestHelper
{
    private readonly IResiliencePipelineBuilder<string> _builder;
    private readonly Mock<Resilience.Internal.IPolicyPipelineBuilder<string>> _pipelineBuilder = new(MockBehavior.Strict);

    public ResiliencePipelineBuilderExtensionsTest()
    {
        Services.TryAddSingleton(_pipelineBuilder.Object);

        _builder = Services.AddResiliencePipeline<string>(DefaultPipelineName);
        _pipelineBuilder.Setup(b => b.Initialize(It.Is<PipelineId>(v => v.PipelineName == DefaultPipelineName && v.PipelineKey == DefaultPipelineKey)));
    }
}
