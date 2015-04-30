// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.Framework.Logging
{
    public interface ILogValues
    {
        /// <summary>
        /// Returns an enumerable of key value pairs mapping the name of the structured data to the data.
        /// </summary>
        IEnumerable<KeyValuePair<string, object>> GetValues();
    }
}