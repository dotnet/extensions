// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Xunit;

namespace Microsoft.Extensions.ExtraAnalyzers.Test;

public class UsingExcessiveSetLookupAnalyzerTests
{
    private static IEnumerable<Assembly> References => new[]
    {
            Assembly.GetAssembly(typeof(IDictionary<,>))!,
            Assembly.GetAssembly(typeof(ImmutableHashSet<>))!,
            Assembly.GetAssembly(typeof(IEnumerable))!,
            Assembly.GetAssembly(typeof(CollectionExtensions))!,
            Assembly.GetAssembly(typeof(SortedSet<>))!,
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
        public void DoSomething(SortedSet<string> collection)
        {
            int b = 6;

            if (collection.Contains(""key""))
            {
                collection.TryGetValue(""key"", out _);
                b = 5;
            }

            if (collection.Contains(""key""))
            {
                collection.Remove(""key"");
            }

            if (!collection.Contains(""key""))
            {
                b = 6;
                collection.Add(""key"");
                b = 5;
            }

            if (!collection.Contains(""key""))
                collection.Add(""key"");

            if (collection.Contains(""key""))
            {
                _ = collection.TryGetValue(""key"", out _);
            }

            if (b < 5)
            {
                throw new Exception(""Error"");
            }
        }
    }
}",
@"using System;
using System.Collections.Generic;

namespace Example
{
    public class TestClass
    {
        public void DoSomething(SortedSet<string> collection)
        {
            int b = 6;

            if (collection.TryGetValue(""key"", out _))
            {
                b = 5;
            }

            collection.Remove(""key"");

            if (!collection.Contains(""key""))
            {
                b = 6;
                collection.Add(""key"");
                b = 5;
            }

            collection.Add(""key"");

            _ = collection.TryGetValue(""key"", out _);

            if (b < 5)
            {
                throw new Exception(""Error"");
            }
        }
    }
}"
            },
            new[]
            {
@"using System.Collections.Generic;

namespace Example
{
    public class TestClass
    {
        public int DoSomething(HashSet<string> collection)
        {
            int b = 6;
            if (!collection.Contains(""key""))
                b = 7;

            collection.Add(""key"");

            if (collection.Contains(""key""))
                b = 7;

            _ = collection.Remove(""key"");

            if (collection.Contains(""key""))
                b = 7;

            _ = collection.TryGetValue(""key"", out string _);
            return b;
        }
    }
}",
@"using System.Collections.Generic;

namespace Example
{
    public class TestClass
    {
        public int DoSomething(HashSet<string> collection)
        {
            int b = 6;
            if (!collection.Add(""key""))
                b = 7;
            if (!collection.Remove(""key""))
                b = 7;
            if (collection.Contains(""key""))
                b = 7;

            _ = collection.TryGetValue(""key"", out string _);
            return b;
        }
    }
}"
            }
        };

    public static IEnumerable<object[]> AnalyzerData => new List<object[]>
        {
            new object[]
            {
                6,
                @"using System;
                using System.Collections.Generic;
                using System.Collections.Immutable;

                namespace Example
                {
                    public class TestClass
                    {
                        public void DoSomething(SortedSet<string> collection, ImmutableHashSet<string> anotherCollection)
                        {
                            int b = 6;

                            if (!/*0+*/collection.Contains(""key"")/*-0*/)
                            {
                                collection.Add(""key"");
                                b = 5;
                            }

                            if (!/*1+*/anotherCollection.Contains(""key"")/*-1*/)
                            {
                                anotherCollection.Add(""key"");
                            }

                            if (!collection.Contains(""key""))
                            {
                                b = 6;
                                collection.Add(""key"");
                                b = 5;
                            }

                            if (/*2+*/collection.Contains(""key"")/*-2*/)
                            {
                                collection.Remove(""key"");
                                b = 5;
                            }

                            if (/*3+*/anotherCollection.Contains(""key"")/*-3*/)
                            {
                                anotherCollection.Remove(""key"");
                            }

                            if (collection.Contains(""key""))
                            {
                                b = 6;
                                collection.Remove(""key"");
                                b = 5;
                            }

                            if (/*4+*/collection.Contains(""key"")/*-4*/)
                            {
                                collection.TryGetValue(""key"", out _);
                                b = 5;
                            }

                            if (/*5+*/anotherCollection.Contains(""key"")/*-5*/)
                            {
                                anotherCollection.TryGetValue(""key"", out _);
                            }

                            if (collection.Contains(""key""))
                            {
                                b = 6;
                                collection.TryGetValue(""key"", out _);
                                b = 5;
                            }

                            if (collection.Contains(""key""))
                            {
                                if (!collection.TryGetValue(""key"", out _))
                                {
                                    throw new Exception(""Error"");
                                }

                                b = 5;
                            }

                            if (!collection.Contains(""key"") && b < 5)
                            {
                                collection.TryGetValue(""key"", out _);
                            }
                        }
                    }
                }"
            },
            new object[]
            {
                6,
                @"using System.Collections.Generic;
                using System.Collections.Immutable;

                namespace Example
                {
                    public class TestClass
                    {
                        public void DoSomething(HashSet<string> collection, ImmutableSortedSet<string> anotherCollection)
                        {
                            int b = 6;

                            if (!/*0+*/collection.Contains(""key"")/*-0*/)
                                _ = collection.Add(""key"");

                            if (/*1+*/collection.Contains(""key"")/*-1*/)
                                _ = !collection.Remove(""key"");

                            if (/*2+*/collection.Contains(""key"")/*-2*/)
                                collection.TryGetValue(""key"", out _);

                            if (!/*3+*/anotherCollection.Contains(""key"")/*-3*/)
                                anotherCollection.Add(""key"");

                            if (/*4+*/anotherCollection.Contains(""key"")/*-4*/)
                                anotherCollection.Remove(""key"");

                            if (/*5+*/anotherCollection.Contains(""key"")/*-5*/)
                                anotherCollection.TryGetValue(""key"", out _);

                            bool flag = false;
                            if (!collection.Contains(""key""))
                                flag = collection.Add(""key"");

                            if (anotherCollection.Contains(""key""))
                                b = 7;

                            _ = anotherCollection.Remove(""key"");

                            if (collection.Contains(""key"") && b < 5 && flag)
                                collection.TryGetValue(""key"", out _);
                        }
                    }
                }"
            },
            new object[]
            {
                4,
                @"using System.Collections.Immutable;

                namespace Example
                {
                    public class TestClass
                    {
                        public void DoSomething(ImmutableHashSet<int>.Builder builder, ImmutableSortedSet<int>.Builder anotherBuilder)
                        {
                            if (!/*0+*/builder.Contains(1)/*-0*/)
                                builder.Add(1);

                            if (/*1+*/builder.Contains(2)/*-1*/)
                                builder.Remove(2);

                            if (!/*2+*/anotherBuilder.Contains(1)/*-2*/)
                                anotherBuilder.Add(1);

                            if (/*3+*/anotherBuilder.Contains(2)/*-3*/)
                                anotherBuilder.Remove(2);
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
                        public void DoSomething(HashSet<string> collection)
                        {
                            if (!collection.Contains(""key""))
                            {
                                collection.Add(""key2"");
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
                        public void DoSomething(ICollection<string> collection)
                        {
                            if (!collection.Contains(""key""))
                            {
                                collection.Add(""key"");
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
                        public void DoSomething(HashSet<string> collection)
                        {
                            if (!collection.Contains(GetKey()))
                            {
                                collection.Add(GetKey());
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
                        public bool DoSomething(HashSet<string> collection)
                        {
                            if (collection.Contains(""key""))
                            {
                                return GetCollection().Remove(""key"");
                            }

                            if (GetAnotherCollection().Contains(""key""))
                            {
                                return collection.Remove(""key"");
                            }

                            if (GetCollection().Contains(""key""))
                            {
                                return collection.Remove(""key"");
                            }

                            return false;
                        }

                        private HashSet<string> GetCollection()
                        {
                            return new HashSet<string>();
                        }

                        private ICollection<string> GetAnotherCollection()
                        {
                            return new HashSet<string>();
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
                        public void DoSomething(HashSet<string> collection, HashSet<string> anotherCollection)
                        {
                            if (collection.Contains(""key""))
                            {
                                anotherCollection.Add(""key"");
                            }

                            if (collection.Contains(""key""))
                                anotherCollection.Add(""key"");

                            if (!collection.Contains(""key""))
                            {
                                collection.Add(""key"");
                            }
                            else
                            {
                                anotherCollection.Add(""key"");
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
                        public void DoSomething(HashSet<string> collection)
                        {
                            if (collection.Contains(""key""))
                            {
                            }
                        }
                    }
                }"
            },
            new object[]
            {
                2,
                @"using System.Collections.Generic;

                namespace Example
                {
                    public class TestClass
                    {
                        public void DoSomething(ISet<string> collection)
                        {
                            if (!/*0+*/collection.Contains(""key"")/*-0*/)
                                collection.Add(""key"");

                            if (/*1+*/collection.Contains(""key"")/*-1*/)
                                collection.Remove(""key"");
                        }
                    }
                }"
            },
            new object[]
            {
                2,
                @"using System.Collections.Generic;

                namespace Example
                {
                    public class TestClass
                    {
                        public int DoSomething(ISet<string> collection, ISet<string> collection2)
                        {
                            var b = 6;
                            if (!collection.Contains(""key""))
                                collection.Add(""key2"");

                            collection.Add(""key"");

                            if (collection.Contains(""key""))
                                collection.Add(""key2"");

                            _ = collection.Add(""key"");

                            if (!collection.Contains(""key""))
                                b = 7;

                            /*0+*/collection.Add(""key"")/*-0*/;

                            if (collection.Contains(""key""))
                                b = 7;

                            _ = !/*1+*/collection.Add(""key"")/*-1*/;

                            if (collection.Contains(""key""))
                                b = 7;

                            _ = !collection.Add(""key2"");

                            if (collection.Contains(""key""))
                                b = 7;

                            _ = !collection2.Add(""key"");

                            return b;
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
            new UsingExcessiveSetLookupAnalyzer(),
            new UsingExcessiveSetLookupFixer(),
            References,
            new[] { source }).ConfigureAwait(false))[0];

        Assert.Equal(expected.Replace("\r\n", "\n", StringComparison.Ordinal), actual);
    }

    [Theory]
    [MemberData(nameof(AnalyzerData))]
    public async Task ShouldFindWarnings(int expectedNumberOfWarnings, string source)
    {
        var d = await RoslynTestUtils.RunAnalyzer(
            new UsingExcessiveSetLookupAnalyzer(),
            References,
            new[] { source }).ConfigureAwait(false);

        Assert.Equal(expectedNumberOfWarnings, d.Count);
        for (int i = 0; i < d.Count; i++)
        {
            source.AssertDiagnostic(i, DiagDescriptors.UsingExcessiveSetLookup, d[i]);
        }
    }

    [Fact]
    public void CheckExceptionIsThrownWhenNullIsPassedToInitializeCall()
    {
        var a = new UsingExcessiveSetLookupAnalyzer();
        Assert.Throws<ArgumentNullException>(() => a.Initialize(null!));
    }

    [Fact]
    public void CheckFixerPropertiesAreSetCorrectly()
    {
        var fixer = new UsingExcessiveSetLookupFixer();

        Assert.Single(fixer.FixableDiagnosticIds);
        Assert.Equal(DiagDescriptors.UsingExcessiveSetLookup.Id, fixer.FixableDiagnosticIds[0]);
        Assert.Equal(WellKnownFixAllProviders.BatchFixer, fixer.GetFixAllProvider());
    }
}
