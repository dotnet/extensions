// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.StackTrace.Sources;
using ThrowingLibrary;
using Xunit;

namespace Microsoft.Extensions.Internal
{
    public class StackTraceHelperTest
    {
        [Fact]
        public void StackTraceHelper_IncludesLineNumbersForFiles()
        {
            // Arrange
            Exception exception = null;
            try
            {
                // Throwing an exception in the current assembly always seems to populate the full stack
                // trace regardless of symbol type. Crossing assembly boundaries ensures PortablePdbReader gets used
                // on desktop.
                Thrower.Throw();
            }
            catch (Exception ex)
            {
                exception = ex;
            }

            // Act
            var stackFrames = StackTraceHelper.GetFrames(exception);

            // Assert
            Assert.Collection(stackFrames,
                frame =>
                {
                    Assert.Contains("Thrower.cs", frame.FilePath);
                    Assert.Equal(15, frame.LineNumber);
                },
                frame =>
                {
                    Assert.Contains("StackTraceHelperTest.cs", frame.FilePath);
                });
        }
    }
}
