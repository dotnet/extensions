// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.Extensions.ExtraAnalyzers.CallAnalysis;
using Xunit;

namespace Microsoft.Extensions.ExtraAnalyzers.Test;

public class UsingStringFormatterFixerTests
{
    private static IEnumerable<Assembly> References => new[]
    {
            Assembly.GetAssembly(typeof(CompositeFormat))!,
        };

    [Theory]
    [MemberData(nameof(FixerData))]
    public async Task CanFixWarning(string original, string expected)
    {
        var actual = (await RoslynTestUtils.RunAnalyzerAndFixer(
            new CallAnalyzer(),
            new StringFormatFixer(),
            References,
            new[] { original }).ConfigureAwait(false))[0];

        Assert.Equal(expected.Replace("\r\n", "\n", StringComparison.Ordinal), actual);
    }

    [Fact]
    public void UtilityMethods()
    {
        var f = new StringFormatFixer();
        Assert.Single(f.FixableDiagnosticIds);
        Assert.Equal(DiagDescriptors.StringFormat.Id, f.FixableDiagnosticIds[0]);
        Assert.Equal(WellKnownFixAllProviders.BatchFixer, f.GetFixAllProvider());
    }

    public static IEnumerable<object[]> FixerData => new List<object[]>
        {
            new object[]
            {
@"namespace Example
{
    public class TestClass
    {
        public static void Test()
        {
            string.Format(""Hello {0}"", ""World"");
        }
    }
}",
@"using System.Text;

namespace Example
{
    public class TestClass
    {
        public static void Test()
        {
            _sf1.Format(null, ""World"");
        }

        private static readonly CompositeFormat _sf1 = new CompositeFormat(""Hello {0}"");
    }
}"
            },
            new object[]
            {
@"namespace Example
{
    public class TestClass
    {
        public static void Test()
        {
            string.Format(""Hello {0}"", ""test"");
            string.Format(""Hello {0}"", ""World"");
            string.Format(""Hello World {0}"", ""test"");
        }
    }
}",
@"using System.Text;

namespace Example
{
    public class TestClass
    {
        public static void Test()
        {
            _sf1.Format(null, ""test"");
            _sf1.Format(null, ""World"");
            _sf2.Format(null, ""test"");
        }

        private static readonly CompositeFormat _sf1 = new CompositeFormat(""Hello {0}"");
        private static readonly CompositeFormat _sf2 = new CompositeFormat(""Hello World {0}"");
    }
}"
            },
            new object[]
            {
@"using System.Globalization;

namespace Example
{
    public class TestClass
    {
        public static void Test()
        {
            string.Format(CultureInfo.InvariantCulture, ""Hello {0}"", ""test"");
            var formatter = CultureInfo.InvariantCulture;
            string.Format(formatter, ""Hello world {0}"", ""test 123"");
        }
    }
}",
@"using System.Globalization;
using System.Text;

namespace Example
{
    public class TestClass
    {
        public static void Test()
        {
            _sf1.Format(CultureInfo.InvariantCulture, ""test"");
            var formatter = CultureInfo.InvariantCulture;
            _sf2.Format(formatter, ""test 123"");
        }

        private static readonly CompositeFormat _sf1 = new CompositeFormat(""Hello {0}"");
        private static readonly CompositeFormat _sf2 = new CompositeFormat(""Hello world {0}"");
    }
}"
            },
            new object[]
            {
@"using System.Globalization;

namespace Example
{
    public class TestClass
    {
        public static string GetFormat()
        {
            return ""format"";
        }
        public static void Test()
        {
            var formatter = CultureInfo.InvariantCulture;
            var format = GetFormat();
            string.Format(formatter, format, ""test 123"");
            string.Format(""test"", 123);
        }
    }
}",
@"using System.Globalization;
using System.Text;

namespace Example
{
    public class TestClass
    {
        public static string GetFormat()
        {
            return ""format"";
        }
        public static void Test()
        {
            var formatter = CultureInfo.InvariantCulture;
            var format = GetFormat();
            string.Format(formatter, format, ""test 123"");
            _sf1.Format(null, 123);
        }

        private static readonly CompositeFormat _sf1 = new CompositeFormat(""test"");
    }
}"
            },
            new object[]
            {
@"namespace Example
{
    public class TestClass
    {
        public static void Test()
        {
            string.Format(""Hello {0}"", ""World"");
        }
        public static string _sf1 = ""test"";
    }
}",
@"using System.Text;

namespace Example
{
    public class TestClass
    {
        public static void Test()
        {
            _sf2.Format(null, ""World"");
        }
        public static string _sf1 = ""test"";
        private static readonly CompositeFormat _sf2 = new CompositeFormat(""Hello {0}"");
    }
}"
            },
            new object[]
            {
@"using System.Text;

namespace Example
{
    public class TestClass
    {
        public static void Test()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat(""Hello {0}"", ""World"");
        }
    }
}",
@"using System.Text;

namespace Example
{
    public class TestClass
    {
        public static void Test()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat(_sf1, null, ""World"");
        }

        private static readonly CompositeFormat _sf1 = new CompositeFormat(""Hello {0}"");
    }
}"
            },
            new object[]
            {
@"using System.Text;
using System.Globalization;

namespace Example
{
    public class TestClass
    {
        public static void Test()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat(CultureInfo.InvariantCulture, ""Hello {0}"", ""World"");
        }
    }
}",
@"using System.Text;
using System.Globalization;

namespace Example
{
    public class TestClass
    {
        public static void Test()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat(_sf1, CultureInfo.InvariantCulture, ""World"");
        }

        private static readonly CompositeFormat _sf1 = new CompositeFormat(""Hello {0}"");
    }
}"
            },
            new object[]
            {
@"namespace Example
{
    public struct TestClass
    {
        public static void Test()
        {
            string.Format(""Hello {0}"", ""World"");
        }
    }
}",
@"using System.Text;

namespace Example
{
    public struct TestClass
    {
        public static void Test()
        {
            _sf1.Format(null, ""World"");
        }

        private static readonly CompositeFormat _sf1 = new CompositeFormat(""Hello {0}"");
    }
}"
            },
            new object[]
            {
@"using System.Text;

namespace Example
{
    public struct TestClass
    {
        public static void Test()
        {
            string.Format(""Hello {0}"", ""World"");
        }

        private static readonly CompositeFormat _sf = new CompositeFormat(""Hello {0}"");
    }
}",
@"using System.Text;

namespace Example
{
    public struct TestClass
    {
        public static void Test()
        {
            _sf.Format(null, ""World"");
        }

        private static readonly CompositeFormat _sf = new CompositeFormat(""Hello {0}"");
    }
}"
            },
            new object[]
            {
@"using System.Globalization;
using System.Text;

namespace Example
{
    public struct TestClass
    {
        public static void Test()
        {
            string.Format(CultureInfo.InvariantCulture, ""Hello 1 {0}"", ""World"");
        }

        private static readonly CompositeFormat _sf1 = new CompositeFormat(""Hello {0}"");
    }
}",
@"using System.Globalization;
using System.Text;

namespace Example
{
    public struct TestClass
    {
        public static void Test()
        {
            _sf2.Format(CultureInfo.InvariantCulture, ""World"");
        }

        private static readonly CompositeFormat _sf1 = new CompositeFormat(""Hello {0}"");
        private static readonly CompositeFormat _sf2 = new CompositeFormat(""Hello 1 {0}"");
    }
}"
            },
            new object[]
            {
@"namespace Example
{
    public class TestClass
    {
        public static void Test()
        {
            string.Format(null, ""World"");
        }
    }
}",
@"namespace Example
{
    public class TestClass
    {
        public static void Test()
        {
            string.Format(null, ""World"");
        }
    }
}"
            },
            new object[]
            {
@"namespace Example
{
    public class TestClass
    {
        public static void Test()
        {
            string.Format(""Hello {0}"");
        }
    }
}",
@"using System.Text;

namespace Example
{
    public class TestClass
    {
        public static void Test()
        {
            _sf1.Format(null);
        }

        private static readonly CompositeFormat _sf1 = new CompositeFormat(""Hello {0}"");
    }
}"
            },
        };
}
