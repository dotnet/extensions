// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.Framework.FileSystemGlobbing.Infrastructure;
using Xunit;

namespace Microsoft.Framework.FileSystemGlobbing.Tests.Infrastructure
{
    internal class Scenario
    {
        public Scenario(string basePath)
        {
            BasePath = basePath;
            DirectoryInfo = new DirectoryInfoStub(
                recorder: Recorder,
                parentDirectory: null,
                fullName: BasePath,
                name: ".",
                paths: new string[0]);
        }

        public SystemIoRecorder Recorder { get; set; } = new SystemIoRecorder();
        public Matcher PatternMatching { get; set; } = new Matcher();
        public DirectoryInfoStub DirectoryInfo { get; set; }
        public string BasePath { get; set; }
        public PatternMatchingResult Result { get; set; }


        public Scenario Include(params string[] patterns)
        {
            foreach (var pattern in patterns)
            {
                PatternMatching.AddInclude(pattern);
            }
            return this;
        }

        public Scenario Exclude(params string[] patterns)
        {
            foreach (var pattern in patterns)
            {
                PatternMatching.AddExclude(pattern);
            }
            return this;
        }

        public Scenario Files(params string[] files)
        {
            DirectoryInfo = new DirectoryInfoStub(
                DirectoryInfo.Recorder,
                DirectoryInfo.ParentDirectory,
                DirectoryInfo.FullName,
                DirectoryInfo.Name,
                DirectoryInfo.Paths.Concat(files.Select(file => BasePath + file)).ToArray());
            return this;
        }

        public Scenario Execute()
        {
            Result = PatternMatching.Execute(DirectoryInfo);
            return this;
        }

        public Scenario AssertExact(params string[] files)
        {
            Assert.Subset(new HashSet<string>(files), new HashSet<string>(Result.Files));
            Assert.Superset(new HashSet<string>(files), new HashSet<string>(Result.Files));
            return this;
        }

    }
}