// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.LanguageServer.Common;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Formatting
{
    public class CloseTextTagFormatOnTypeProviderTest : FormatOnTypeProviderTestBase
    {
        [Fact]
        public void OnTypeCloseAngle_ClosesTextTag()
        {
            RunFormatOnTypeTest(
input: @"
@{
    <text|
}
",
expected: $@"
@{{
    <text>{LanguageServerConstants.CursorPlaceholderString}</text>
}}
",
character: ">");
        }

        [Fact]
        public void OnTypeCloseAngle_ClosesTextTag_DoesNotReturnPlaceholder()
        {
            RunFormatOnTypeTest(
input: @"
@{
    <text|
}
",
expected: @"
@{
    <text></text>
}
",
character: ">", expectCursorPlaceholder: false);
        }

        [Fact]
        public void OnTypeCloseAngle_OutsideRazorBlock_DoesNotCloseTextTag()
        {
            RunFormatOnTypeTest(
input: @"
    <text|
",
expected: $@"
    <text>
",
character: ">");
        }

        internal override RazorFormatOnTypeProvider CreateProvider()
        {
            var optionsMonitor = new Mock<IOptionsMonitor<RazorLSPOptions>>();
            optionsMonitor.SetupGet(o => o.CurrentValue).Returns(RazorLSPOptions.Default);
            var provider = new CloseTextTagFormatOnTypeProvider(optionsMonitor.Object);

            return provider;
        }
    }
}
