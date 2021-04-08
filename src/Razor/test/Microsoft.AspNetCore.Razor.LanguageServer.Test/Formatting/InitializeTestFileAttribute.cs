// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Reflection;
using Microsoft.AspNetCore.Razor.LanguageServer.Semantic;
using Xunit.Sdk;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Formatting
{
    public class IntializeTestFileAttribute : BeforeAfterTestAttribute
    {
        public override void Before(MethodInfo methodUnderTest)
        {
            var typeName = methodUnderTest.ReflectedType.Name;
            if (typeof(FormattingTestBase).GetTypeInfo().IsAssignableFrom(methodUnderTest.DeclaringType.GetTypeInfo()))
            {
                FormattingTestBase.FileName = $"Formatting/TestFiles/{typeName}/{methodUnderTest.Name}";
            }
            else if (typeof(SemanticTokenTestBase).GetTypeInfo().IsAssignableFrom(methodUnderTest.DeclaringType.GetTypeInfo()))
            {
                SemanticTokenTestBase.FileName = $"Semantic\\TestFiles\\{typeName}\\{methodUnderTest.Name}";
            }
        }

        public override void After(MethodInfo methodUnderTest)
        {
            if (typeof(FormattingTestBase).GetTypeInfo().IsAssignableFrom(methodUnderTest.DeclaringType.GetTypeInfo()))
            {
                FormattingTestBase.FileName = null;
            }
            else if (typeof(SemanticTokenTestBase).GetTypeInfo().IsAssignableFrom(methodUnderTest.DeclaringType.GetTypeInfo()))
            {
                SemanticTokenTestBase.FileName = null;
            }
        }
    }
}
