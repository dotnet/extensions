// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using Microsoft.Framework.FileSystemGlobbing.Infrastructure;
using Xunit;

namespace Microsoft.Framework.FileSystemGlobbing.Tests.TestUtility
{
    internal class FileSystemGlobbingTestContext
    {
        private readonly string _basePath;
        private readonly FileSystemOperationRecorder _recorder;
        private readonly Matcher _patternMatching;

        private MockDirectoryInfo _directoryInfo;
        private PatternMatchingResult _result;

        public FileSystemGlobbingTestContext(string basePath, Matcher matcher)
        {
            _basePath = basePath;
            _recorder = new FileSystemOperationRecorder();
            _patternMatching = matcher;

            _directoryInfo = new MockDirectoryInfo(
                recorder: _recorder,
                parentDirectory: null,
                fullName: _basePath,
                name: ".",
                paths: new string[0]);
        }

        public FileSystemGlobbingTestContext Include(params string[] patterns)
        {
            foreach (var pattern in patterns)
            {
                _patternMatching.AddInclude(pattern);
            }

            return this;
        }

        public FileSystemGlobbingTestContext Exclude(params string[] patterns)
        {
            foreach (var pattern in patterns)
            {
                _patternMatching.AddExclude(pattern);
            }

            return this;
        }

        public FileSystemGlobbingTestContext Files(params string[] files)
        {
            _directoryInfo = new MockDirectoryInfo(
                _directoryInfo.Recorder,
                _directoryInfo.ParentDirectory,
                _directoryInfo.FullName,
                _directoryInfo.Name,
                _directoryInfo.Paths.Concat(files.Select(file => _basePath + file)).ToArray());

            return this;
        }

        public FileSystemGlobbingTestContext Execute()
        {
            _result = _patternMatching.Execute(_directoryInfo);

            return this;
        }

        public FileSystemGlobbingTestContext AssertExact(params string[] files)
        {
            Assert.Equal(files.OrderBy(file => file), _result.Files.OrderBy(file => file));

            return this;
        }
    }
}