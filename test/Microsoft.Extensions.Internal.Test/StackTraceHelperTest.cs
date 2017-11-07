// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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
                    Assert.Equal(17, frame.LineNumber);
                },
                frame =>
                {
                    Assert.Contains("StackTraceHelperTest.cs", frame.FilePath);
                });
        }

        [Fact]
        public void StackTraceHelper_PrettyPrintsStackTraceForGenericMethods()
        {
            // Arrange
            var exception = Record.Exception(() => GenericMethod<string>(null));

            // Act
            var stackFrames = StackTraceHelper.GetFrames(exception);

            // Assert
            var methods = stackFrames.Select(frame => frame.MethodDisplayInfo.ToString()).ToArray();
            Assert.Equal("Microsoft.Extensions.Internal.StackTraceHelperTest.GenericMethod<T>(T val)", methods[0]);
        }

        [Fact]
        public void StackTraceHelper_PrettyPrintsStackTraceForMethodsOnGenericTypes()
        {
            // Arrange
            var exception = Record.Exception(() => new GenericType<int>().Throw(0));

            // Act
            var stackFrames = StackTraceHelper.GetFrames(exception);

            // Assert
            var methods = stackFrames.Select(frame => frame.MethodDisplayInfo.ToString()).ToArray();
            Assert.Equal("Microsoft.Extensions.Internal.StackTraceHelperTest+GenericType<T>.Throw(T parameter)", methods[0]);
        }

        [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
        private void GenericMethod<T>(T val) where T: class => throw new Exception();

        private class GenericType<T>
        {
            [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
            public void Throw(T parameter) => throw new Exception();
        }
    }
}
