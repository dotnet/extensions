// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using ClassLibraryWithPortablePdbs;
using Microsoft.Extensions.StackTrace.Sources;
using Xunit;

namespace Microsoft.Extensions.Internal.Test
{
    public class StackTraceTest
    {
        public static TheoryData CanGetStackTraceData => new TheoryData<Action, string>
        {
            {
                ThrowsException,
                $"{typeof(StackTraceTest).GetTypeInfo().FullName}.{nameof(ThrowsException)}()"
            },
            {
                new ExceptionType().MethodThatThrows,
                $"{typeof(ExceptionType).GetTypeInfo().FullName}.{nameof(ExceptionType.MethodThatThrows)}()"
            },
            {
                ExceptionType.StaticMethodThatThrows,
                $"{typeof(ExceptionType).GetTypeInfo().FullName}.{nameof(ExceptionType.StaticMethodThatThrows)}()"
            }
        };

        [Theory]
        [MemberData(nameof(CanGetStackTraceData))]
        public void GetFrames_CanGetStackTrace(Action action, string expectedDisplay)
        {
            try
            {
                //
                action();
            }
            catch (Exception exception)
            {
                // Arrange and Act
                var frames = StackTraceHelper.GetFrames(exception);

                // Assert
                Assert.Equal(expectedDisplay, frames.First().MethodDisplayInfo.ToString());
                Assert.Equal(
                    $"{typeof(StackTraceTest).GetTypeInfo().FullName}.{nameof(GetFrames_CanGetStackTrace)}" +
                    "(Action action, string expectedDisplay)",
                    frames.Last().MethodDisplayInfo.ToString());
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
                Assert.Equal(
                    $"{typeof(StackTraceTest).GetTypeInfo().FullName}.{nameof(GetFrames_DoesNotFailForDynamicallyGeneratedAssemblies)}()",
                    frames.Last().MethodDisplayInfo.ToString());
            }
        }

        public static TheoryData GetMethodDisplayString_ReturnsTypeNameQualifiedMethodsData
        {
            get
            {
                var thisType = typeof(StackTraceTest);
                var intParse = ((Func<string, int>)int.Parse).GetMethodInfo();
                var dateTimeOffsetTryParse = typeof(DateTimeOffset).GetMethods()
                    .First(m => m.Name == nameof(DateTimeOffset.TryParse) && m.GetParameters().Length == 2);
                var genericTypeMethod = typeof(List<Process>).GetMethod(nameof(List<Process>.Remove));
                var genericMethod = thisType.GetMethod(nameof(GenericMethod));
                var nestedGenericMethod = thisType.GetMethod(nameof(NestedGenericMethod));
                var byRefMethod = thisType.GetMethod(nameof(ByRefMethod));
                var asyncMethod = thisType.GetMethod(nameof(AsyncMethod));
                var nullableParam = thisType.GetMethod(nameof(MethodWithNullableParams));

                return new TheoryData<MethodBase, string>
                {
                    { intParse, "System.int.Parse(string s)" },
                    { dateTimeOffsetTryParse, "System.DateTimeOffset.TryParse(string input, out DateTimeOffset result)" },
                    { genericTypeMethod, "System.Collections.Generic.List<Process>.Remove(Process item)" },
                    { genericMethod, $"{thisType}.{nameof(GenericMethod)}<TVal>(TVal value)" },
                    { nestedGenericMethod, $"{thisType}.NestedGenericMethod<TKey, TVal>(KeyValuePair<TKey, TVal> keyValuePair)" },
                    { byRefMethod, $"{thisType}.{nameof(ByRefMethod)}(int a, CultureInfo b, ref long c)" },
                    { asyncMethod, $"{thisType}.{nameof(AsyncMethod)}(string name)" },
                    { nullableParam, $"{thisType}.{nameof(MethodWithNullableParams)}(Nullable<int> name, string value)" }
                };
            }
        }

        [Theory]
        [MemberData(nameof(GetMethodDisplayString_ReturnsTypeNameQualifiedMethodsData))]
        public void GetMethodDisplayString_ReturnsTypeNameQualifiedMethods(MethodBase method, string expected)
        {
            // Act
            var actual = StackTraceHelper.GetMethodDisplayString(method);

            // Assert
            Assert.Equal(expected, actual.ToString());
        }

        public static void ThrowsException()
        {
            throw new Exception();
        }

        public string GenericMethod<TVal>(TVal value) => value.ToString();

        public void NestedGenericMethod<TKey, TVal>(KeyValuePair<TKey, TVal> keyValuePair)
        {
        }

        public decimal ByRefMethod(int a, CultureInfo b, ref long c) => a + c;

        public async Task<object> AsyncMethod(string name) => await Task.FromResult(0);

        public void MethodWithNullableParams(int? name, string value)
        {
        }
    }
}
