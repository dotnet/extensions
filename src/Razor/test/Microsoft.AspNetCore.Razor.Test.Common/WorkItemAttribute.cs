// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Razor.Test.Common
{
    /// <summary>
    /// Used to tag test methods or types which are created for a given WorkItem
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public sealed class WorkItemAttribute : Attribute
    {
        public string Location
        {
            get;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WorkItemAttribute"/>.
        /// </summary>
        /// <param name="issueUri">The URI where the original work item can be viewed.</param>
        public WorkItemAttribute(string issueUri)
        {
            Location = issueUri;
        }
    }
}
