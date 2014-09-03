// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Framework.TestAdapter;
using Xunit.Abstractions;

namespace Xunit.KRunner
{
    public static class TestCaseExtensions
    {
        public static Test ToDesignTimeTest(this ITestCase testCase)
        {
            var test = new Test()
            {
                DisplayName = testCase.DisplayName,
                FullyQualifiedName = testCase.UniqueID,
            };

            if (testCase.SourceInformation != null)
            {
                test.CodeFilePath = testCase.SourceInformation.FileName;
                test.LineNumber = testCase.SourceInformation.LineNumber;
            }

            return test;
        }
    }
}