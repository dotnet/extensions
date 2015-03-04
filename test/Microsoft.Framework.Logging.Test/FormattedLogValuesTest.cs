// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.Framework.Logging.Internal;
using Xunit;

namespace Microsoft.Framework.Logging.Test
{
    public class FormattedLogValuesTest
    {
        [Theory]
        [InlineData("", "", new object[] { })]
        [InlineData("arg1 arg2", "{0} {1}", new object[] { "arg1", "arg2" })]
        [InlineData("arg1 arg2", "{Start} {End}", new object[] { "arg1", "arg2" })]
        [InlineData("arg1     arg2", "{Start,-6} {End,6}", new object[] { "arg1", "arg2" })]
        [InlineData("0064", "{Hex:X4}", new object[] { 100 })]
        public void LogValues_With_Basic_Types(string expected, string format, object[] args)
        {
            var logValues = new FormattedLogValues(format, args);
            Assert.Equal(expected, logValues.Format());

            // Original format is expected to be returned from GetValues.
            Assert.Equal(format, logValues.GetValues().First(v => v.Key == "{OriginalFormat}").Value);
        }

        [Theory]
        [InlineData("1 2015", "{Year,6:d yyyy}")]
        [InlineData("1:01:2015 AM,:        01", "{Year,-10:d:MM:yyyy tt},:{second,10:ss}")]
        [InlineData("{prefix{1 2015}suffix}", "{{prefix{{{Year,6:d yyyy}}}suffix}}")]
        public void LogValues_With_DateTime(string expected, string format)
        {
            var dateTime = new DateTime(2015, 1, 1, 1, 1, 1);
            var logValues = new FormattedLogValues(format, new object[] { dateTime, dateTime });
            Assert.Equal(expected, logValues.Format());

            // Original format is expected to be returned from GetValues.
            Assert.Equal(format, logValues.GetValues().First(v => v.Key == "{OriginalFormat}").Value);
        }

        [Theory]
        [InlineData("{", "{{", null)]
        [InlineData("'{'", "'{{'", null)]
        [InlineData("'{}'", "'{{}}'", null)]
        [InlineData("arg1 arg2 '{}'  '{' '{:}' '{,:}' {,}- test string",
            "{0} {1} '{{}}'  '{{' '{{:}}' '{{,:}}' {{,}}- test string",
            new object[] { "arg1", "arg2" })]
        [InlineData("{prefix{arg1}suffix}", "{{prefix{{{Argument}}}suffix}}", new object[] { "arg1" })]
        public void LogValues_With_Escaped_Braces(string expected, string format, object[] args)
        {
            var logValues = args == null ?
                new FormattedLogValues(format) :
                new FormattedLogValues(format, args);

            Assert.Equal(expected, logValues.Format());

            // Original format is expected to be returned from GetValues.
            Assert.Equal(format, logValues.GetValues().First(v => v.Key == "{OriginalFormat}").Value);
        }

        [Theory]
        [InlineData("{foo")]
        [InlineData("bar}")]
        [InlineData("{foo bar}")]
        public void LogValues_With_UnbalancedBraces(string format)
        {
            Assert.Throws<FormatException>(() =>
            {
                var logValues = new FormattedLogValues(format);
                logValues.Format();
            });
        }
    }
}