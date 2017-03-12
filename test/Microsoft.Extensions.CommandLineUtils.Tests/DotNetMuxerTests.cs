// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if NETCOREAPP2_0
using System.IO;
using Xunit;

namespace Microsoft.Extensions.CommandLineUtils
{
    public class DotNetMuxerTests
    {
        [Fact]
        public void FindsTheMuxer()
        {
            var muxerPath = DotNetMuxer.MuxerPath;
            Assert.NotNull(muxerPath);
            Assert.True(File.Exists(muxerPath), "The file did not exist");
        }
    }
}
#endif
