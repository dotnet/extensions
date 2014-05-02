// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

using System;
using System.Diagnostics;

namespace Microsoft.AspNet.Logging
{
    /// <summary>
    /// A generic interface for logging.
    /// </summary>
    public interface ILogger
    {
        /// <summary>
        /// Aggregates most logging patterns to a single method.  This must be compatible with the Func representation in the OWIN environment.
        /// 
        /// To check IsEnabled call WriteCore with only TraceEventType and check the return value, no event will be written.
        /// </summary>
        /// <param name="eventType"></param>
        /// <param name="eventId"></param>
        /// <param name="state"></param>
        /// <param name="exception"></param>
        /// <param name="formatter"></param>
        /// <returns></returns>
        bool WriteCore(TraceType eventType, int eventId, object state, Exception exception, Func<object, Exception, string> formatter);
    }
}
