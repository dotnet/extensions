// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Razor.LanguageServer.AutoInsert
{
    public class CloseTextTagOnAutoInsertProviderTest : RazorOnAutoInsertProviderTestBase
    {
        [Fact]
        public void OnTypeCloseAngle_ClosesTextTag()
        {
            RunAutoInsertTest(
input: @"
@{
    <text|
}
",
expected: @"
@{
    <text>$0</text>
}
",
character: ">");
        }

        [Fact]
        public void OnTypeCloseAngle_OutsideRazorBlock_DoesNotCloseTextTag()
        {
            RunAutoInsertTest(
input: @"
    <text|
",
expected: @"
    <text>
",
character: ">");
        }

        internal override RazorOnAutoInsertProvider CreateProvider()
        {
            var optionsMonitor = new Mock<IOptionsMonitor<RazorLSPOptions>>();
            optionsMonitor.SetupGet(o => o.CurrentValue).Returns(RazorLSPOptions.Default);
            var provider = new CloseTextTagOnAutoInsertProvider(optionsMonitor.Object);

            return provider;
        }
    }
}
