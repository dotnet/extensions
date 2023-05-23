// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Extensions.ExtraAnalyzers.Test;

public class UsingExcessiveDictionaryLookupAnalyzerTests
{
    private static IEnumerable<Assembly> References => new[]
    {
        Assembly.GetAssembly(typeof(IDictionary<,>))!,
        Assembly.GetAssembly(typeof(IEnumerable))!,
        Assembly.GetAssembly(typeof(CollectionExtensions))!
    };

    public static IEnumerable<object[]> AnalyzerData => new List<object[]>
        {
            new object[]
            {
                2,
                @"using System.Collections.Generic;

                namespace Example
                {
                    public class TestClass
                    {
                        public void DoSomething(Dictionary<string, string> dictionary)
                        {
                            string key = ""key"";
                            var flag = false;
                            if (/*0+*/dictionary.ContainsKey(key)/*-0*/)
                                flag = dictionary.TryAdd(key, ""val"");

                            if (/*1+*/dictionary.ContainsKey(key)/*-1*/)
                                _ = dictionary.Remove(key);

                            if (!flag)
                                dictionary.Add(key, ""val"");
                        }
                    }
                }"
            },
            new object[]
            {
                7,
                @"using System;
                using System.Collections.Generic;

                namespace Example
                {
                    public class TestClass
                    {
                        public void DoSomething(Dictionary<string, string> dictionary)
                        {
                            int b = 6;

                            if (/*0+*/dictionary.ContainsKey(""key"")/*-0*/)
                            {
                                b = 7;
                                var value = dictionary[""key""];
                                b = 5;
                            }

                            string value2 = ""123"";
                            if (/*1+*/dictionary.ContainsKey(""key"")/*-1*/)
                            {
                                b = 6;
                                value2 = dictionary[""key""];
                                b = 5;
                            }

                            if (!/*2+*/dictionary.ContainsKey(""key"")/*-2*/)
                            {
                                dictionary.Add(""key"", ""val"");
                            }

                            if (!/*3+*/dictionary.ContainsKey(""key"")/*-3*/)
                            {
                                dictionary[""key""] = ""newval"";
                            }

                            if (/*4+*/dictionary.ContainsKey(""key"")/*-4*/)
                            {
                                dictionary.TryAdd(""key"", ""val"");
                            }

                            if (/*5+*/dictionary.ContainsKey(""key"")/*-5*/)
                            {
                                dictionary.Remove(""key"");
                            }

                            if (/*6+*/dictionary.ContainsKey(""key"")/*-6*/)
                            {
                                b = 7;
                                if (string.IsNullOrEmpty(dictionary[""key""]))
                                {
                                    throw new Exception(""Error"");
                                }

                                b = 5;
                            }

                            if (!dictionary.ContainsKey(""key"") && b < 5)
                            {
                                dictionary.Add(""key"", ""val"");
                            }
                        }
                    }
                }"
            },
            new object[]
            {
                6,
                @"using System.Collections.Generic;

                namespace Example
                {
                    public class TestClass
                    {
                        public void DoSomething(IDictionary<string, string> dictionary)
                        {
                            int b = 6;

                            if (/*0+*/dictionary.ContainsKey(""key"")/*-0*/)
                            {
                                b = 7;
                                var value = dictionary[""key""];
                                b = 5;
                            }

                            string value2 = ""123"";
                            if (/*1+*/dictionary.ContainsKey(""key"")/*-1*/)
                            {
                                b = 6;
                                value2 = dictionary[""key""];
                                b = 5;
                            }

                            if (!/*2+*/dictionary.ContainsKey(""key"")/*-2*/)
                            {
                                dictionary.Add(""key"", ""val"");
                            }

                            if (!/*3+*/dictionary.ContainsKey(""key"")/*-3*/)
                            {
                                dictionary[""key""] = ""newval"";
                            }

                            if (/*4+*/dictionary.ContainsKey(""key"")/*-4*/)
                            {
                                dictionary.Remove(""key"");
                            }

                            if (/*5+*/dictionary.ContainsKey(""key"")/*-5*/)
                            {
                                dictionary.TryAdd(""key"", ""val"");
                            }

                            if (!dictionary.ContainsKey(""key"") && b < 5)
                            {
                                dictionary.Add(""key"", ""val"");
                            }
                        }
                    }
                }"
            },
            new object[]
            {
                5,
                @"using System.Collections.Generic;

                namespace Example
                {
                    public class TestClass
                    {
                        public void DoSomething(Dictionary<string, string> dictionary)
                        {
                            int b = 6;
                            string key = ""key"";
                            if (!/*0+*/dictionary.ContainsKey(key)/*-0*/)
                                dictionary.Add(key, ""val"");

                            if (!/*1+*/dictionary.ContainsKey(key)/*-1*/)
                                dictionary[key] = ""newval"";

                            if (/*2+*/dictionary.ContainsKey(key)/*-2*/)
                                dictionary.TryAdd(key, ""val"");

                            if (/*3+*/dictionary.ContainsKey(key)/*-3*/)
                                dictionary.Remove(key);

                            string value = ""abc"";
                            if (/*4+*/dictionary.ContainsKey(key)/*-4*/)
                                value = dictionary[key];

                            if (!dictionary.ContainsKey(key) && b < 5 && value == ""abc"")
                                dictionary.Add(key, ""val"");
                        }
                    }
                }"
            },
            new object[]
            {
                4,
                @"using System.Collections.Generic;

                namespace Example
                {
                    public class TestClass
                    {
                        public void DoSomething(IDictionary<string, string> dictionary)
                        {
                            int b = 6;
                            if (!/*0+*/dictionary.ContainsKey(""key"")/*-0*/)
                                dictionary.Add(""key"", ""val"");

                            if (!/*1+*/dictionary.ContainsKey(""key"")/*-1*/)
                                dictionary[""key""] = ""newval"";

                            if (/*2+*/dictionary.ContainsKey(""key"")/*-2*/)
                                dictionary.Remove(""key"");

                            if (/*3+*/dictionary.ContainsKey(""key"")/*-3*/)
                                dictionary.TryAdd(""key"", ""val"");

                            if (!dictionary.ContainsKey(""key"") && b < 5)
                                dictionary.Add(""key"", ""val"");
                        }
                    }
                }"
            },
            new object[]
            {
                0,
                @"using System.Collections.Generic;

                namespace Example
                {
                    public class TestClass
                    {
                        public void DoSomething(Dictionary<string, string> dictionary)
                        {
                            if (!dictionary.ContainsKey(""key""))
                            {
                                dictionary.Add(""key2"", ""val"");
                            }
                        }
                    }
                }"
            },
            new object[]
            {
                0,
                @"using System.Collections.Generic;

                namespace Example
                {
                    public class TestClass
                    {
                        public void DoSomething(Dictionary<string, string> dictionary)
                        {
                            if (!dictionary.ContainsKey(GetKey()))
                            {
                                dictionary.Add(GetKey(), ""val"");
                            }
                        }

                        private string GetKey()
                        {
                            return ""someKey"";
                        }
                    }
                }"
            },
            new object[]
            {
                0,
                @"using System.Collections.Generic;

                namespace Example
                {
                    public class TestClass
                    {
                        public bool DoSomething(Dictionary<string, string> dictionary)
                        {
                            if (dictionary.ContainsKey(""key""))
                            {
                                return GetDictionary().Remove(""key"");
                            }

                            if (GetDictionary().ContainsKey(""key""))
                            {
                                return dictionary.Remove(""key"");
                            }

                            return false;
                        }

                        private Dictionary<string, string> GetDictionary()
                        {
                            return new Dictionary<string, string>();
                        }
                    }
                }"
            },
            new object[]
            {
                0,
                @"using System;
                using System.Collections.Generic;

                namespace Example
                {
                    public class TestClass
                    {
                        public void DoSomething(Dictionary<string, string> dictionary, Dictionary<string, string> anotherDictionary)
                        {
                            if (dictionary.ContainsKey(""key""))
                            {
                                anotherDictionary.Add(""key"", ""val"");
                            }

                            if (dictionary.ContainsKey(""key""))
                                anotherDictionary.Add(""key"", ""val"");

                            if (dictionary.ContainsKey(""key""))
                            {
                                anotherDictionary[""key""] = ""value"";
                            }

                            string value = ""abc"";
                            if (dictionary.ContainsKey(""key""))
                                value = anotherDictionary[""key""];

                            if (value == ""abc"")
                            {
                                throw new Exception(""Error"");
                            }
                        }
                    }
                }"
            },
            new object[]
            {
                0,
                @"using System.Collections.Generic;

                namespace Example
                {
                    public class TestClass
                    {
                        public void DoSomething(Dictionary<string, string> dictionary)
                        {
                            if (dictionary.ContainsKey(""key""))
                            {
                            }
                        }
                    }
                }"
            },
            new object[]
            {
                4,
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

                            value = /*0+*/dictionary[""key""]/*-0*/;

                            if (dictionary.ContainsKey(""key""))
                                b = 7;

                            /*1+*/dictionary.Add(""key"", ""val"")/*-1*/;

                            if (dictionary.ContainsKey(""key""))
                                b = 7;

                            /*2+*/dictionary[""key""]/*-2*/ = ""newval"";

                            if (dictionary.ContainsKey(""key""))
                                b = 7;

                            _ = /*3+*/dictionary.TryAdd(""key"", ""val"")/*-3*/;

                            if (dictionary.ContainsKey(""key""))
                                dictionary[""key2""] = ""7"";

                            _ = dictionary.TryAdd(""key"", ""val"");

                            if (dictionary.ContainsKey(""key""))
                            {
                                b = 7;
                            }
                            else
                            {
                                b = 9;
                            }

                            _ = dictionary.TryAdd(""key"", ""val"");

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
    [MemberData(nameof(AnalyzerData))]
    public async Task ShouldFindWarnings(int expectedNumberOfWarnings, string source)
    {
        var d = await RoslynTestUtils.RunAnalyzer(
            new UsingExcessiveDictionaryLookupAnalyzer(),
            References,
            new[] { source }).ConfigureAwait(false);

        Assert.Equal(expectedNumberOfWarnings, d.Count);
        for (int i = 0; i < d.Count; i++)
        {
            source.AssertDiagnostic(i, DiagDescriptors.UsingExcessiveDictionaryLookup, d[i]);
        }
    }
}
