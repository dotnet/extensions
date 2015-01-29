// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.Framework.Logging.Test
{
    public class LoggerStructureFormatTest
    {
        [Theory]
        [InlineData("arg1 arg2", "{0} {1}", new object[] { "arg1", "arg2" })]
        [InlineData("arg1 arg2", "{Start} {End}", new object[] { "arg1", "arg2" })]
        [InlineData("arg1     arg2", "{Start,-6} {End,6}", new object[] { "arg1", "arg2" })]
        [InlineData("0064", "{Hex:X4}", new object[] { 100 })]
        public void LoggerStructure_With_Basic_Types(string expected, string format, object[] args)
        {
            var loggerStructure = new LoggerStructureFormat(format, args);
            Assert.Equal(expected, loggerStructure.Format());
        }

        [Theory]
        [InlineData("1 2015", "{Year,6:d yyyy}")]
        [InlineData("1:01:2015 AM,:        01", "{Year,-10:d:MM:yyyy tt},:{second,10:ss}")]
        public void LoggerStructure_With_DateTime(string expected, string format)
        {
            var dateTime = new DateTime(2015, 1, 1, 1, 1, 1);
            var loggerStructure = new LoggerStructureFormat(format, new object[] { dateTime, dateTime });
            Assert.Equal(expected, loggerStructure.Format());
        }
    }
}