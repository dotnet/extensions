// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Xunit;

namespace Microsoft.Framework.Logging.Test
{
    public class LoggerStructureFormatTest
    {
        [Theory]
        [InlineData("", "", new object[] { })]
        [InlineData("arg1 arg2", "{0} {1}", new object[] { "arg1", "arg2" })]
        [InlineData("arg1 arg2", "{Start} {End}", new object[] { "arg1", "arg2" })]
        [InlineData("arg1     arg2", "{Start,-6} {End,6}", new object[] { "arg1", "arg2" })]
        [InlineData("0064", "{Hex:X4}", new object[] { 100 })]
        public void LoggerStructure_With_Basic_Types(string expected, string format, object[] args)
        {
            var loggerStructure = new LoggerStructureFormat(format, args);
            Assert.Equal(expected, loggerStructure.Format());

            // Original format is expected to be returned from GetValues.
            Assert.Equal(format, loggerStructure.GetValues().First(v => v.Key == "{OriginalFormat}").Value);
        }

        [Theory]
        [InlineData("1 2015", "{Year,6:d yyyy}")]
        [InlineData("1:01:2015 AM,:        01", "{Year,-10:d:MM:yyyy tt},:{second,10:ss}")]
        [InlineData("{prefix{1 2015}suffix}", "{{prefix{{{Year,6:d yyyy}}}suffix}}")]
        public void LoggerStructure_With_DateTime(string expected, string format)
        {
            var dateTime = new DateTime(2015, 1, 1, 1, 1, 1);
            var loggerStructure = new LoggerStructureFormat(format, new object[] { dateTime, dateTime });
            Assert.Equal(expected, loggerStructure.Format());

            // Original format is expected to be returned from GetValues.
            Assert.Equal(format, loggerStructure.GetValues().First(v => v.Key == "{OriginalFormat}").Value);
        }

        [Theory]
        [InlineData("{", "{{", null)]
        [InlineData("'{'", "'{{'", null)]
        [InlineData("'{}'", "'{{}}'", null)]
        [InlineData("arg1 arg2 '{}'  '{' '{:}' '{,:}' {,}- test string",
            "{0} {1} '{{}}'  '{{' '{{:}}' '{{,:}}' {{,}}- test string",
            new object[] { "arg1", "arg2" })]
        [InlineData("{prefix{arg1}suffix}", "{{prefix{{{Argument}}}suffix}}", new object[] { "arg1" })]
        public void LoggerStructure_With_Escaped_Braces(string expected, string format, object[] args)
        {
            var loggerStructure = args == null ?
                new LoggerStructureFormat(format) :
                new LoggerStructureFormat(format, args);

            Assert.Equal(expected, loggerStructure.Format());

            // Original format is expected to be returned from GetValues.
            Assert.Equal(format, loggerStructure.GetValues().First(v => v.Key == "{OriginalFormat}").Value);
        }

        [Theory]
        [InlineData("{foo")]
        [InlineData("bar}")]
        [InlineData("{foo bar}")]
        public void LoggerStructure_With_UnbalancedBraces(string format)
        {
            Assert.Throws<FormatException>(() =>
            {
                var loggerStructure = new LoggerStructureFormat(format);
                loggerStructure.Format();
            });
        }
    }
}