// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Framework.Internal;

namespace Microsoft.Dnx.TestAdapter
{
    public interface ITestExecutionSink
    {
        void RecordStart([NotNull] Test test);

        void RecordResult([NotNull] TestResult testResult);
    }
}
