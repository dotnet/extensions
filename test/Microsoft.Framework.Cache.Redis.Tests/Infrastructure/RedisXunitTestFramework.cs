// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Reflection;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

// TODO: Disabled due to CI failures. [assembly: TestFramework("Microsoft.Framework.Cache.Redis.RedisXunitTestFramework", "Microsoft.Framework.Cache.Redis.Tests")]

namespace Microsoft.Framework.Cache.Redis
{
    public class RedisXunitTestFramework : XunitTestFramework
    {
        protected override ITestFrameworkExecutor CreateExecutor(AssemblyName assemblyName)
        {
            return new RedisXunitTestExecutor(assemblyName, SourceInformationProvider);
        }
    }
}
