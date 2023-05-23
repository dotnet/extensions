// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Xunit;

namespace Microsoft.Extensions.ExtraAnalyzers.Test;

public class UsingExcessiveDictionaryLookupFixerTests
{
    private static IEnumerable<Assembly> References => new[]
    {
            Assembly.GetAssembly(typeof(IDictionary<,>))!,
            Assembly.GetAssembly(typeof(IEnumerable))!,
            Assembly.GetAssembly(typeof(CollectionExtensions))!
    };

    public static IEnumerable<object[]> FixerData => new List<object[]>
        {
            new[]
            {
@"using System;
using System.Collections.Generic;

namespace Example
{
    public class TestClass
    {
        public void DoSomething(Dictionary<string, string> dictionary)
        {
            int b = 6;

            if (dictionary.ContainsKey(""key""))
            {
                b = 7;
                var value = dictionary[""key""];
                var anotherValue = dictionary[""key""];
                var anotherValue2 = dictionary[""key2""];
                if (string.IsNullOrEmpty(dictionary[""key""]))
                {
                    throw new Exception(""Error"");
                }

                b = 5;
            }

            if (dictionary.ContainsKey(""key""))
            {
                dictionary.Remove(""key"", out _);
            }

            if (dictionary.ContainsKey(""key""))
            {
                dictionary.Remove(""key"");
            }

            if (!dictionary.ContainsKey(""key""))
            {
                dictionary.Add(""key"", ""value"");
            }

            if (!dictionary.ContainsKey(""key2""))
            {
                dictionary.TryAdd(""key2"", ""value2"");
            }

            if (!dictionary.ContainsKey(""key3""))
            {
                dictionary[""key3""] = ""value3"";
            }

            if (b < 5)
            {
                throw new Exception(""Error"");
            }
        }
    }
}
",
@"using System;
using System.Collections.Generic;

namespace Example
{
    public class TestClass
    {
        public void DoSomething(Dictionary<string, string> dictionary)
        {
            int b = 6;

            if (dictionary.TryGetValue(""key"", out var retrievedValue))
            {
                b = 7;
                var value = retrievedValue;
                var anotherValue = retrievedValue;
                var anotherValue2 = dictionary[""key2""];
                if (string.IsNullOrEmpty(retrievedValue))
                {
                    throw new Exception(""Error"");
                }

                b = 5;
            }

            dictionary.Remove(""key"", out _);

            dictionary.Remove(""key"");

            dictionary.TryAdd(""key"", ""value"");

            dictionary.TryAdd(""key2"", ""value2"");

            dictionary.TryAdd(""key3"", ""value3"");

            if (b < 5)
            {
                throw new Exception(""Error"");
            }
        }
    }
}
"
            },
            new[]
            {
@"using System;
using System.Collections.Generic;

namespace Example
{
    public class TestClass
    {
        public void DoSomething(Dictionary<string, string> dictionary)
        {
            string value = ""abc"";
            if (dictionary.ContainsKey(""key""))
                value = dictionary[""key""];

            if (dictionary.ContainsKey(""key""))
                dictionary.Remove(""key"", out _);

            if (dictionary.ContainsKey(""key""))
                dictionary.Remove(""key"");

            if (!dictionary.ContainsKey(""key""))
                dictionary.Add(""key"", ""value"");

            if (!dictionary.ContainsKey(""key2""))
                dictionary.TryAdd(""key2"", ""value2"");

            if (!dictionary.ContainsKey(""key3""))
                dictionary[""key3""] = ""value3"";

            if (dictionary.ContainsKey(""key""))
                dictionary.TryGetValue(""key"", out _);

            if (value == ""abc"")
            {
                throw new Exception(""Error"");
            }
        }
    }
}
",
@"using System;
using System.Collections.Generic;

namespace Example
{
    public class TestClass
    {
        public void DoSomething(Dictionary<string, string> dictionary)
        {
            string value = ""abc"";
            if (dictionary.TryGetValue(""key"", out var retrievedValue))
                value = retrievedValue;

            dictionary.Remove(""key"", out _);

            dictionary.Remove(""key"");

            dictionary.TryAdd(""key"", ""value"");

            dictionary.TryAdd(""key2"", ""value2"");

            dictionary.TryAdd(""key3"", ""value3"");

            dictionary.TryGetValue(""key"", out _);

            if (value == ""abc"")
            {
                throw new Exception(""Error"");
            }
        }
    }
}
"
            },
            new[]
            {
@"using System;
using System.Collections.Generic;

namespace Example
{
    public class TestClass
    {
        public void DoSomething(IDictionary<string, string> dictionary, IDictionary<string, string> anotherDictionary)
        {
            int b = 6;

            if (dictionary.ContainsKey(""key""))
            {
                b = 7;
                var value = dictionary[""key""];
                var anotherValue = dictionary[""key""];
                var anotherValue2 = dictionary[""key2""];
                anotherDictionary[""key""] = dictionary[""key""];
                b = 5;
            }

            if (dictionary.ContainsKey(""key""))
            {
                dictionary.Remove(""key"", out _);
            }

            if (dictionary.ContainsKey(""key""))
            {
                dictionary.Remove(""key"");
            }

            if (!dictionary.ContainsKey(""key""))
            {
                dictionary.Add(""key"", ""value"");
            }

            if (!dictionary.ContainsKey(""key2""))
            {
                dictionary.TryAdd(""key2"", ""value2"");
            }

            if (!dictionary.ContainsKey(""key3""))
            {
                dictionary[""key3""] = ""value3"";
            }

            if (dictionary.ContainsKey(""key""))
            {
                dictionary.TryGetValue(""key"", out _);
            }

            if (b < 5)
            {
                throw new Exception(""Error"");
            }
        }
    }
}
",
@"using System;
using System.Collections.Generic;

namespace Example
{
    public class TestClass
    {
        public void DoSomething(IDictionary<string, string> dictionary, IDictionary<string, string> anotherDictionary)
        {
            int b = 6;

            if (dictionary.TryGetValue(""key"", out var retrievedValue))
            {
                b = 7;
                var value = retrievedValue;
                var anotherValue = retrievedValue;
                var anotherValue2 = dictionary[""key2""];
                anotherDictionary[""key""] = retrievedValue;
                b = 5;
            }

            dictionary.Remove(""key"", out _);

            dictionary.Remove(""key"");

            dictionary.TryAdd(""key"", ""value"");

            dictionary.TryAdd(""key2"", ""value2"");

            dictionary.TryAdd(""key3"", ""value3"");

            dictionary.TryGetValue(""key"", out _);

            if (b < 5)
            {
                throw new Exception(""Error"");
            }
        }
    }
}
"
            },
            new[]
            {
@"using System;
using System.Collections.Generic;

namespace Example
{
    public class TestClass
    {
        public void DoSomething(IReadOnlyDictionary<string, string> dictionary)
        {
            int b = 6;

            if (dictionary.ContainsKey(""key""))
            {
                b = 7;
                var value = dictionary[""key""];
                var anotherValue = dictionary[""key""];
                var anotherValue2 = dictionary[""key2""];
                b = 5;
            }

            if (b < 5)
            {
                throw new Exception(""Error"");
            }
        }
    }
}
",
@"using System;
using System.Collections.Generic;

namespace Example
{
    public class TestClass
    {
        public void DoSomething(IReadOnlyDictionary<string, string> dictionary)
        {
            int b = 6;

            if (dictionary.TryGetValue(""key"", out var retrievedValue))
            {
                b = 7;
                var value = retrievedValue;
                var anotherValue = retrievedValue;
                var anotherValue2 = dictionary[""key2""];
                b = 5;
            }

            if (b < 5)
            {
                throw new Exception(""Error"");
            }
        }
    }
}
"
            },
            new[]
            {
@"using System.Collections.Generic;

namespace Example
{
    public class TestClass
    {
        public void DoSomething(IReadOnlyDictionary<string, string> dictionary)
        {
            if (dictionary.ContainsKey(""key""))
                dictionary.TryGetValue(""key"", out _);
        }
    }
}
",
@"using System.Collections.Generic;

namespace Example
{
    public class TestClass
    {
        public void DoSomething(IReadOnlyDictionary<string, string> dictionary)
        {
            dictionary.TryGetValue(""key"", out _);
        }
    }
}
"
            },
            new[]
            {
@"using System;
using System.Collections.Generic;

namespace Example
{
    public class TestClass
    {
        public void DoSomething(Dictionary<string, string> dictionary, Dictionary<string, string> anotherDictionary)
        {
            string value = ""abc"";
            if (dictionary.ContainsKey(""key""))
            {
                dictionary[""key""] = ""value"";
            }
            else
            {
                dictionary.Add(""key"", ""value"");
            }

            if (!dictionary.ContainsKey(""key""))
                dictionary.Add(""key"", ""value"");
            else
                dictionary[""key""] = ""value"";

            if (dictionary.ContainsKey(""key""))
                dictionary.Add(""key2"", ""value"");
            else
                dictionary[""key""] = ""value"";

            if (dictionary.ContainsKey(""key""))
                dictionary.Add(""key"", ""value"");
            else
                dictionary[""key2""] = ""value"";

            if (dictionary.ContainsKey(""key""))
                dictionary.Add(""key"", ""value"");
            else
                dictionary[""key""] = ""value2"";

            if (dictionary.ContainsKey(""key""))
                anotherDictionary.Add(""key"", ""value"");
            else
                anotherDictionary[""key""] = ""value"";

            if (dictionary.ContainsKey(""key""))
            {
                dictionary[""key""] = ""value"";
                dictionary[""key2""] = ""value"";
            }
            else
            {
                dictionary.Add(""key"", ""value"");
            }

            if (dictionary.ContainsKey(""key""))
            {
                value = dictionary[""key""];
            }
            else
            {
                dictionary.Add(""key"", ""value"");
            }

            if (value == ""abc"")
            {
                throw new Exception(""Error"");
            }
        }
    }
}
",
@"using System;
using System.Collections.Generic;

namespace Example
{
    public class TestClass
    {
        public void DoSomething(Dictionary<string, string> dictionary, Dictionary<string, string> anotherDictionary)
        {
            string value = ""abc"";
            dictionary[""key""] = ""value"";

            dictionary[""key""] = ""value"";

            if (dictionary.ContainsKey(""key""))
                dictionary.Add(""key2"", ""value"");
            else
                dictionary[""key""] = ""value"";

            if (dictionary.ContainsKey(""key""))
                dictionary.Add(""key"", ""value"");
            else
                dictionary[""key2""] = ""value"";

            if (dictionary.ContainsKey(""key""))
                dictionary.Add(""key"", ""value"");
            else
                dictionary[""key""] = ""value2"";

            if (dictionary.ContainsKey(""key""))
                anotherDictionary.Add(""key"", ""value"");
            else
                anotherDictionary[""key""] = ""value"";

            if (dictionary.ContainsKey(""key""))
            {
                dictionary[""key""] = ""value"";
                dictionary[""key2""] = ""value"";
            }
            else
            {
                dictionary.Add(""key"", ""value"");
            }

            if (dictionary.ContainsKey(""key""))
            {
                value = dictionary[""key""];
            }
            else
            {
                dictionary.Add(""key"", ""value"");
            }

            if (value == ""abc"")
            {
                throw new Exception(""Error"");
            }
        }
    }
}
"
            },
            new[]
            {
@"using System.Collections.Generic;

namespace Example
{
    public class TestClass
    {
        public void DoSomething(Dictionary<string, string> dictionary)
        {
            if (dictionary.ContainsKey(""key""))
                _ = dictionary.Remove(""key"", out _);

            if (dictionary.ContainsKey(""key""))
                _ = dictionary.Remove(""key"");

            if (!dictionary.ContainsKey(""key2""))
                _ = dictionary.TryAdd(""key2"", ""value2"");

            if (dictionary.ContainsKey(""key""))
                _ = dictionary.TryGetValue(""key"", out _);

            if (dictionary.ContainsKey(""key""))
                _ = dictionary.TryGetValue(""key"", out _).ToString();
        }
    }
}
",
@"using System.Collections.Generic;

namespace Example
{
    public class TestClass
    {
        public void DoSomething(Dictionary<string, string> dictionary)
        {
            _ = dictionary.Remove(""key"", out _);

            _ = dictionary.Remove(""key"");

            _ = dictionary.TryAdd(""key2"", ""value2"");

            _ = dictionary.TryGetValue(""key"", out _);

            if (dictionary.ContainsKey(""key""))
                _ = dictionary.TryGetValue(""key"", out _).ToString();
        }
    }
}
"
            }
        };

    public static IEnumerable<object[]> AnotherFixerData => new List<object[]>
    {
        new[]
        {
            @"using System;
            using System.Collections.Generic;

            namespace Example
            {
                public class TestClass
                {
                    public void DoSomething(Dictionary<string, string> dictionary)
                    {
                        int b = 6;
                        string value = ""123"";
                        if (dictionary.ContainsKey(""key""))
                        {
                            b = 7;
                        }

                        value = dictionary[""key""];
                        if (b == 6)
                        {
                            throw new Exception(""Error"");
                        }
                    }
                }
            }"
        },
        new[]
        {
            @"using System;
            using System.Collections.Generic;

            namespace Example
            {
                public class TestClass
                {
                    public void DoSomething(Dictionary<string, string> dictionary)
                    {
                        int b = 6;
                        if (dictionary.ContainsKey(""key""))
                            b = 7;

                        dictionary.Add(""key"", ""val"");

                        if (b == 6)
                        {
                            throw new Exception(""Error"");
                        }
                    }
                }
            }"
        }
    };

    [Theory]
    [MemberData(nameof(FixerData))]
    public async Task CanFixWarning(string source, string expected)
    {
        var actual = (await RoslynTestUtils.RunAnalyzerAndFixer(
            new UsingExcessiveDictionaryLookupAnalyzer(),
            new UsingExcessiveDictionaryLookupFixer(),
            References,
            new[] { source }).ConfigureAwait(false))[0];

        Assert.Equal(expected.Replace("\r\n", "\n", StringComparison.Ordinal), actual);
    }

    [Theory]
    [MemberData(nameof(AnotherFixerData))]
    public async Task ShouldNotFixWarning(string source)
    {
        var actual = (await RoslynTestUtils.RunAnalyzerAndFixer(
            new UsingExcessiveDictionaryLookupAnalyzer(),
            new UsingExcessiveDictionaryLookupFixer(),
            References,
            new[] { source }).ConfigureAwait(false))[0];

        Assert.Equal(source.Replace("\r\n", "\n", StringComparison.Ordinal), actual);
    }

    [Fact]
    public void CheckFixerPropertiesAreSetCorrectly()
    {
        var fixer = new UsingExcessiveDictionaryLookupFixer();

        Assert.Single(fixer.FixableDiagnosticIds);
        Assert.Equal(DiagDescriptors.UsingExcessiveDictionaryLookup.Id, fixer.FixableDiagnosticIds[0]);
        Assert.Equal(WellKnownFixAllProviders.BatchFixer, fixer.GetFixAllProvider());
    }
}
