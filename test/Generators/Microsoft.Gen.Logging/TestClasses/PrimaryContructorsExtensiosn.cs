// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

#pragma warning disable SA1402

namespace TestClasses
{
    public partial class ClassWithPrimaryConstructor(ILogger logger)
    {
        [LoggerMessage(0, LogLevel.Debug, "Test.")]
        public partial void Test();
    }

    public partial class ClassWithPrimaryConstructorInDifferentPartialDeclaration(ILogger logger);

    public partial class ClassWithPrimaryConstructorInDifferentPartialDeclaration
    {
        [LoggerMessage(0, LogLevel.Debug, "Test.")]
        public partial void Test();
    }

    public partial class ClassWithPrimaryConstructorAndField(ILogger logger)
    {
#pragma warning disable IDE0052 // Remove unread private members
#pragma warning disable S3604 // Member initializer values should not be redundant
        private readonly ILogger _logger = logger;
#pragma warning restore S3604 // Member initializer values should not be redundant
#pragma warning restore IDE0052 // Remove unread private members

        [LoggerMessage(0, LogLevel.Debug, "Test.")]
        public partial void Test();
    }
}
