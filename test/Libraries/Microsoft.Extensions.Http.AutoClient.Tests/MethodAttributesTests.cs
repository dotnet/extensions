// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Microsoft.Extensions.Http.AutoClient.Test
{
    public class MethodAttributesTests
    {
        [Fact]
        public void DeleteAttributePath()
        {
            var a = new DeleteAttribute("some-path");
            Assert.Equal("some-path", a.Path);
        }

        [Fact]
        public void GetAttributePath()
        {
            var a = new GetAttribute("some-path");
            Assert.Equal("some-path", a.Path);
        }

        [Fact]
        public void HeadAttributePath()
        {
            var a = new HeadAttribute("some-path");
            Assert.Equal("some-path", a.Path);
        }

        [Fact]
        public void OptionsAttributePath()
        {
            var a = new OptionsAttribute("some-path");
            Assert.Equal("some-path", a.Path);
        }

        [Fact]
        public void PatchAttributePath()
        {
            var a = new PatchAttribute("some-path");
            Assert.Equal("some-path", a.Path);
        }

        [Fact]
        public void PostAttributePath()
        {
            var a = new PostAttribute("some-path");
            Assert.Equal("some-path", a.Path);
        }

        [Fact]
        public void PutAttributePath()
        {
            var a = new PutAttribute("some-path");
            Assert.Equal("some-path", a.Path);
        }
    }
}
