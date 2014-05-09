// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if K10 || NET45
using System;
using System.IO;

namespace Microsoft.Framework.ConfigurationModel
{
    // REVIEW: Should this be public or should it just be shared code?
    public static class PathResolver
    {
        private static string ApplicationBaseDirectory
        {
            get
            {
#if NET45
                return AppDomain.CurrentDomain.BaseDirectory;
#else
                return ApplicationContext.BaseDirectory;
#endif
            }
        }

        public static string ResolveAppRelativePath(string path)
        {
            return Path.Combine(ApplicationBaseDirectory, path);
        }
    }
}
#endif