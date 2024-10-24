// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

namespace ConsoleLogger
{
    internal class MyCustomSampler : LoggerSampler
    {
        public override bool ShouldSample(SamplingParameters parameters)
        {
            throw new System.NotImplementedException();
        }
    }
}
