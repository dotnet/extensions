// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Framework.Logging
{
    /// <summary>
    /// Used to create logger instances of the given name.
    /// </summary>
#if ASPNETCORE50
    [Runtime.AssemblyNeutral]
#endif
    public interface ILoggerProvider
    {
        /// <summary>
        /// Creates a new ILogger instance of the given name.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        ILogger Create(string name);
    }
}
