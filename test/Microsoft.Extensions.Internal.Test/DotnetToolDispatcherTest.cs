// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;
using Microsoft.DotNet.Cli.Utils;
using Xunit;

namespace Microsoft.Extensions.Internal
{
    public class DotnetToolDispatcherTest
    {
        private static readonly string TestToolName = typeof(ProjectDependenciesCommandFactory).GetTypeInfo().Assembly.GetName().Name;

        public static TheoryData IsDispatcherData
        {
            get
            {
                return new TheoryData<string[], bool>
                {
                    { new[] { "--dispatcher-version" }, false },
                    { new[] { "--dispatcher-Version", "1.0.0.0" }, false },
                    { new[] { "resolve-data", "./project.json", "random", "--Dispatcher-version" }, false },
                    { new[] { "resolve-data", "./project.json", "random", "--dispatcher-version", "1.0.0.0" }, false },
                    { new[] { "resolve-data", "./project.json", "random" }, true },
                    { new string[0], true },
                };
            }
        }

        [Theory]
        [MemberData(nameof(IsDispatcherData))]
        public void IsDispatcher_WorksAsExpected(string[] programArgs, bool expectedResult)
        {
            // Act
            var isDispatcher = DotnetToolDispatcher.IsDispatcher(programArgs);

            // Assert
            Assert.Equal(expectedResult, isDispatcher);
        }

        public static TheoryData EnsureValidDispatchRecipientThrowsData
        {
            get
            {
                return new TheoryData<string[]>
                {
                    new[] { "--dispatcher-version" },
                    new[] { "--dispatcher-Version", "0.1.2.3" },
                    new[] { "resolve-data", "./project.json", "random", "--Dispatcher-version" },
                    new[] { "resolve-data", "./project.json", "random", "--dispatcher-version", "1.0.0.0-123" },
                };
            }
        }

        [Theory]
        [MemberData(nameof(EnsureValidDispatchRecipientThrowsData))]
        public void EnsureValidDispatchRecipient_ThrowsWhenDispatcherVersionIsInvalid(string[] programArgs)
        {
            // Arrange
            var expectedToolName = typeof(ProjectDependenciesCommandFactory).GetTypeInfo().Assembly.GetName().Name;

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(
                () => DotnetToolDispatcher.EnsureValidDispatchRecipient(ref programArgs, TestToolName));
            Assert.Equal(
                $"Could not invoke tool {expectedToolName}. Ensure it has matching versions in the project.json's 'dependencies' and 'tools' sections.",
                exception.Message);
        }

        public static TheoryData EnsureValidDispatchRecipientNoopData
        {
            get
            {
                var expectedDispatcherVersion = DotnetToolDispatcher.ResolveDispatcherVersionArgumentValue(TestToolName);

                return new TheoryData<string[]>
                {
                    new[] { "--dispatcher-Version", expectedDispatcherVersion },
                    new[] { "resolve-data", "--dispatcher-Version", expectedDispatcherVersion, "-flag" },
                    new[] { "resolve-data", "./project.json", "random", "--dispatcher-version", expectedDispatcherVersion },
                };
            }
        }

        [Theory]
        [MemberData(nameof(EnsureValidDispatchRecipientNoopData))]
        public void EnsureValidDispatchRecipient_NoopsWhenDispatcherVersionIsValid(string[] programArgs)
        {
            // Act & Assert (does not throw)
            DotnetToolDispatcher.EnsureValidDispatchRecipient(ref programArgs, TestToolName);
        }
    }
}
