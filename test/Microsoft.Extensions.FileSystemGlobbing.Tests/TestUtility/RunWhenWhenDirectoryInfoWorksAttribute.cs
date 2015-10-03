// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;

namespace Microsoft.Extensions.FileSystemGlobbing.Tests.TestUtility
{
    public class RunWhenWhenDirectoryInfoWorksAttribute :
        Attribute, Microsoft.AspNet.Testing.xunit.ITestCondition
    {
        public bool IsMet
        {
            get
            {
                return TestCoreCLRIssue();
            }
        }

        public string SkipReason
        {
            get
            {
                return "Issue: \"DirectoryInfo.Exists returns incorrect results when it is cast from FileSystemInfo.\" is fixed. Please remove the workaround in the produce code and this test case.";
            }
        }

        private bool TestCoreCLRIssue()
        {
            string root = null;
            try
            {
                root = Path.GetTempFileName();
                File.Delete(root);
                Directory.CreateDirectory(root);

                var testDir = new DirectoryInfo(root);
                var betaDir = Path.Combine(root, "beta");

                Directory.CreateDirectory(betaDir);

                var beta = testDir.EnumerateFileSystemInfos("beta", SearchOption.TopDirectoryOnly).First() as DirectoryInfo;
                return !beta.Exists;
            }
            finally
            {
                if (root != null && Directory.Exists(root))
                {
                    Directory.Delete(root, true);
                }
            }
        }
    }
}