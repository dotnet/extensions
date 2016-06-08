// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Linq.Expressions;
using ClassLibraryWithPortablePdbs;
using Microsoft.Extensions.StackTrace.Sources;
using Xunit;

namespace Microsoft.Extensions.Internal.Test
{
    public class StackTraceTest
    {
        public static TheoryData CanGetStackTraceData => new TheoryData<Action, string>
        {
            { ThrowsException, nameof(ThrowsException) },
            { new ExceptionType().MethodThatThrows, nameof(ExceptionType.MethodThatThrows) },
            { ExceptionType.StaticMethodThatThrows, nameof(ExceptionType.StaticMethodThatThrows) }
        };

        [Theory]
        [MemberData(nameof(CanGetStackTraceData))]
        public void GetFrames_CanGetStackTrace(Action action, string expectedMethodName)
        {
            try
            {
                action();
            }
            catch (Exception exception)
            {
                // Arrange and Act
                var frames = StackTraceHelper.GetFrames(exception);

                // Assert
                Assert.Equal(expectedMethodName, frames.First().Method);
                Assert.Equal(nameof(GetFrames_CanGetStackTrace), frames.Last().Method);
            }
        }

        [Fact]
        public void GetFrames_DoesNotFailForDynamicallyGeneratedAssemblies()
        {
            // Arrange
            var action = (Action)Expression.Lambda(
                Expression.Throw(
                    Expression.New(typeof(Exception)))).Compile();

            // Act
            try
            {
                action();
            }
            catch (Exception exception)
            {
                var frames = StackTraceHelper.GetFrames(exception);

                // Assert
                Assert.Null(frames.First().FilePath);
                Assert.Equal(nameof(GetFrames_DoesNotFailForDynamicallyGeneratedAssemblies), frames.Last().Method);
            }
        }

        public static void ThrowsException()
        {
            throw new Exception();
        }
    }
}
