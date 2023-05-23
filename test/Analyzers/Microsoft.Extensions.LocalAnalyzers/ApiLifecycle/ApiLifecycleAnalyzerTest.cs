// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.LocalAnalyzers.Resource.Test;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.Extensions.LocalAnalyzers.ApiLifecycle.Test;

public class ApiLifecycleAnalyzerTest
{
    [Theory]
    [MemberData(nameof(CodeWithMissingApis))]
    public async Task Analyzer_Reports_Diagnostics_When_Code_Was_Not_Annotated_Correctly(int expectedDiagnostics, string fileName,
        string testAssemblyName, DiagnosticDescriptor descriptor, string source)
    {
        var options = AnalyzerOptionsFactory.WithFiles(fileName);

        var diagnostics = await RoslynTestUtils.RunAnalyzer(
                analyzer: new ApiLifecycleAnalyzer(),
                references: References,
                sources: new[]
                {
                    "[assembly: System.Runtime.Versioning.TargetFramework(\".NETCoreApp,Version=v6.0\")]",
                    source
                },
                options: options,
                testAssemblyName: testAssemblyName)
            .ConfigureAwait(false);

        Assert.Equal(expectedDiagnostics, diagnostics.Count);

        for (int i = 0; i < diagnostics.Count; i++)
        {
            source.AssertDiagnostic(i, descriptor, diagnostics[i]);
        }
    }

    private const string AttributeDefinition = @"
            namespace System.Diagnostics.CodeAnalysis;
            internal sealed class ExperimentalAttribute : System.Attribute { }
        ";

    [Theory]
    [MemberData(nameof(CodeWithMissingMembers))]
    public async Task Analyzer_Reports_Diagnostics_When_StableCode_Was_Not_Found_In_The_Compilation(int expectedDiagnostics, string fileName,
        string testAssemblyName, string[] ids, string source)
    {
        var options = AnalyzerOptionsFactory.WithFiles(fileName);
        var diagnostics = await RoslynTestUtils.RunAnalyzer(
                analyzer: new ApiLifecycleAnalyzer(),
                references: References,
                sources: new[]
                {
                    "[assembly: System.Runtime.Versioning.TargetFramework(\".NETCoreApp,Version=v6.0\")]",
                    AttributeDefinition,
                    source
                },
                options: options,
                testAssemblyName: testAssemblyName)
            .ConfigureAwait(false);

        if (expectedDiagnostics != diagnostics.Count)
        {
            System.Diagnostics.Debugger.Break();
        }

        Assert.Equal(expectedDiagnostics, diagnostics.Count);

        for (int i = 0; i < diagnostics.Count; i++)
        {
            var actual = diagnostics[i];

            Assert.Contains(actual.Id, ids);
        }
    }

    public static IEnumerable<System.Reflection.Assembly> References => new[]
    {
        Assembly.GetAssembly(typeof(ObsoleteAttribute))!,
        Assembly.GetAssembly(typeof(EditorBrowsableAttribute))!,
        Assembly.GetAssembly(typeof(Debugger))!,
        Assembly.GetAssembly(typeof(IReadOnlyList<>))!,
        Assembly.GetAssembly(typeof(ArgumentOutOfRangeException))!,
        Assembly.GetAssembly(typeof(IServiceProvider))!,
        Assembly.GetAssembly(typeof(RequiredAttribute))!,
        Assembly.GetAssembly(typeof(OptionsBuilder<>))!,
        Assembly.GetAssembly(typeof(IConfigurationSection))!,
        Assembly.GetAssembly(typeof(HttpRequestMessage))!,
        Assembly.GetAssembly(typeof(IDistributedCache))!,
        Assembly.GetAssembly(typeof(Microsoft.Extensions.ObjectPool.ObjectPool))!,
        Assembly.GetAssembly(typeof(IBufferWriter<>))!,
    };

    public static IEnumerable<object[]> CodeWithMissingMembers => new List<object[]>
    {
        new object[]
        {
            1,
            "ApiLifecycle/Data/CompletelyEmpty.json",
            "CompletelyEmpty",
            new [] { DiagDescriptors.NewSymbolsMustBeMarkedExperimental.Id },
            @"

            using System;
            using System.ComponentModel;
            using System.Diagnostics.CodeAnalysis;

            namespace Microsoft.Extensions.Data.Classification;

            [Obsolete(""Deleted at 1.18.0. Use AccessControlData<string> instead."", true)]
            [EditorBrowsable]
            [ExcludeFromCodeCoverage]
            public readonly struct AccessControlDataString2
            {
            }
            "
        },
        new object[]
        {
            0,
            "ApiLifecycle/Data/Delegates.json",
            "Delegates",
            Array.Empty<string>(),
            @"
                using System.Net.Http;

                namespace Microsoft.Extensions.HttpClient.Resilience
                {
                    public delegate string PipelineKeySelector(HttpRequestMessage requestMessage);
                }
            "
        },
        new object[]
        {
            0,
            "ApiLifecycle/Data/StringSplit.json",
            "StringSplit",
            Array.Empty<string>(),
            @"
                namespace System.Text;

                public static partial class StringSplitExtensions
                {
                }
            "
        },
        new object[]
        {
            0,
            "ApiLifecycle/Data/HttpClientResiliencePolly.json",
            "HttpClientResiliencePolly",
            Array.Empty<string>(),
            @"
                using System;
                using System.Collections.Generic;
                using System.Net.Http;

                namespace Microsoft.Extensions.HttpClient.Resilience;

                public static class HttpClientResilienceGenerators2
                {
                    public static readonly Func<List<HttpResponseMessage>, TimeSpan> HandleRetryAfterHeader = null!;
                }
            "
        },
        new object[]
        {
            1,
            "ApiLifecycle/Data/ICounterT.json",
            "ICounterT",
            new[] { DiagDescriptors.PublishedSymbolsCantChange.Id },
            @"
                namespace Microsoft.Extensions.Metering;

                using System.Collections.Generic;

                public interface ICounter2<in T>
                {
                    void Add(T value, params string[] dimensionValues);

                    void Add(T value, IDictionary<string, string> dimensions);
                }
            "
        },
        new object[]
        {
            0,
            "ApiLifecycle/Data/Protected.json",
            "Protected",
            Array.Empty<string>(),
            @"
                namespace Microsoft.Extensions.Security.Identity;

                using System.Collections.Generic;
                using System.Diagnostics.CodeAnalysis;

                [Experimental]
                public class AdditionalContext2
                {
                    protected IReadOnlyDictionary<string, object> Features { get; } = new Dictionary<string, object>();
                }
            "
        },
        new object[]
        {
            0,
            "ApiLifecycle/Data/ICounterT.json",
            "ICounterT",
            Array.Empty<string>(),
            @"
                namespace Microsoft.Extensions.Metering;

                using System.Collections.Generic;

                public interface ICounter2<in T>
                    where T : struct
                {
                    void Add(T value, params string[] dimensionValues);

                    void Add(T value, IDictionary<string, string> dimensions);
                }
            "
        },
        new object[]
        {
            1,
            "ApiLifecycle/Data/Experimental.json",
            "Experimental",
            new[] { DiagDescriptors.PublishedSymbolsCantChange.Id },
            @"
               namespace Microsoft.Extensions.Diagnostics;

               public sealed class ExperimentalAttribute2
               {
                   public string? Message { get; }

                   public ExperimentalAttribute2(string? message = null)
                   {
                       if (!string.IsNullOrWhiteSpace(message))
                       {
                           Message = message;
                       }
                   }
               }
            "
        },
        new object[]
        {
            0,
            "ApiLifecycle/Data/Histogram.json",
            "Histogram",
            Array.Empty<string>(),
            @"
                namespace Microsoft.Extensions.Metering;

                using System.Collections.Generic;

                public interface IHistogram2<in T> where T : struct
                {
                    void Record(T value, params string[] dimensionValues);

                    void Record(T value, IDictionary<string, string> dimensions);
                }
            "
        },
        new object[]
        {
            0,
            "ApiLifecycle/Data/Time.json",
            "Time",
            Array.Empty<string>(),
            @"
                namespace Microsoft.Extensions.Time;

                public sealed class SystemClock2
                {
                    public static readonly SystemClock2 Instance = new();
                }
            "
        },
        new object[]
        {
            0,
            "ApiLifecycle/Data/Indexer.json",
            "Indexer",
            Array.Empty<string>(),
            @"
            namespace Microsoft.Extensions.Collections.Frozen;

            public readonly struct FrozenOrdinalStringDictionary<TValue>
            {
                public TValue this[string key] => default!;
            }
            "
        },
        new object[]
        {
            0,
            "ApiLifecycle/Data/BufferWriter2.json",
            "BufferWriter2",
            Array.Empty<string>(),
            @"
            using System;
            using System.Buffers;
            using System.Diagnostics.CodeAnalysis;

            namespace Microsoft.Extensions.Buffers;

            public sealed class BufferWriter2<T> : IBufferWriter<T>
            {
                internal const int MaxArrayLength = 0X7FEF_FFFF;   // Copy of the internal Array.MaxArrayLength const
                private const int DefaultCapacity = 256;

                private T[] _buffer = Array.Empty<T>();

                [Experimental]
                public BufferWriter2() { }

                public ReadOnlyMemory<T> WrittenMemory => _buffer.AsMemory(0, WrittenCount);

                public ReadOnlySpan<T> WrittenSpan => _buffer.AsSpan(0, WrittenCount);

                /// <summary>
                /// Gets the amount of data written to the underlying buffer so far.
                /// </summary>
                public int WrittenCount { get; private set; }

                public int Capacity
                {
                    get => _buffer.Length;

                    set
                    {
                        _ = value;
                    }
                }

                public void Reset()
                {
                }

                public void Advance(int count)
                {
                }

                public Memory<T> GetMemory(int sizeHint = 0)
                {
                    return new Memory<T>(Array.Empty<T>());
                }

                public Span<T> GetSpan(int sizeHint = 0)
                {
                    return new Span<T>(Array.Empty<T>());
                }

                private void EnsureCapacity(int sizeHint)
                {
                }
            }
        "
        },

        new object[]
        {
            0,
            "ApiLifecycle/Data/ImplicitOperator.json",
            "ImplicitOperator",
            Array.Empty<string>(),
            @"
                using System;
                using System.ComponentModel;
                using System.Diagnostics.CodeAnalysis;

                namespace Microsoft.Extensions.Data.Classification;

                [Obsolete(""Deleted at 1.18.0. Use CustomerContent<string> instead."", true)]
                [EditorBrowsable(EditorBrowsableState.Never)]
                [ExcludeFromCodeCoverage]
                public readonly struct CustomerContentString2
                {
                    internal CustomerContentString2(int c)
                    {
                        // Intentionally left empty.
                    }

                    [Obsolete(""Deleted at 1.18.0. Use CustomerContent<string> instead."", true)]
                    [EditorBrowsable(EditorBrowsableState.Never)]
                    [ExcludeFromCodeCoverage]
                    public static implicit operator string(CustomerContentString2 value)
                    {
                        return string.Empty;
                    }

                    [Obsolete(""Deleted at 1.18.0. Use CustomerContent<string> instead."", true)]
                    [EditorBrowsable(EditorBrowsableState.Never)]
                    [ExcludeFromCodeCoverage]
                    public static implicit operator CustomerContentString2(string value)
                    {
                        return new(0);
                    }
                }
               "
        },
        new object[]
        {
            0,
            "ApiLifecycle/Data/AsyncState.json",
            "AsyncState",
            Array.Empty<string>(),
            @"
                using System;
                using System.Diagnostics.CodeAnalysis;

                namespace Microsoft.Extensions.AsyncState;

                [Obsolete(""Deprecated since 1.17.0 and will be removed in 1.20.0. Use IAsyncContext<T> instead."")]
                public interface IAsyncContext
                {
                    [SuppressMessage(""Minor Code Smell"", ""S4049:Properties should be preferred"", Justification = ""Not suitable"")]
                    T? GetAsyncState<T>()
                        where T : notnull;

                    void SetAsyncState<T>(T? instance)  where T : notnull;
                }
            "
        },
        new object[]
        {
            0,
            "ApiLifecycle/Data/Data.Classification2.json",
            "Data.Classification2",
            Array.Empty<string>(),
            @"
                namespace Microsoft.Extensions.Data.Classification2;

                using System.Diagnostics.CodeAnalysis;

                public interface IClassifiedData
                {
                    public DataClass DataClass { get; }
                }

                [Experimental]
                public enum DataClass
                {

                }
            "
        },
        new object[]
        {
            1,
            "ApiLifecycle/Data/CompletelyEmpty.json",
            "Something",
            new[] { DiagDescriptors.NewSymbolsMustBeMarkedExperimental.Id },
            @"                    
                namespace Example;                       

                /// <summary>
                /// Some text for test.
                /// </summary>
                public static class TestClass
                {                    
                }
            "
        },
        new object[]
        {
            0,
            "ApiLifecycle/Data/Pools.json",
            "Pools",
            Array.Empty<string>(),
            @"
               using Microsoft.Extensions.ObjectPool;

               namespace Microsoft.Extensions.Pools;

               public sealed class ScaledObjectPool<T> : ObjectPool<T>
                   where T : class
               {
                    public override T Get()
                    {
                        return default!;
                    }

                    public override void Return(T obj)
                    {
                        // Intentionally left empty.
                    }
               }
             "
        },
        new object[]
        {
            0,
            "ApiLifecycle/Data/Data.Classification.json",
            "Microsoft.Extensions.Data.Classification",
            Array.Empty<string>(),
            @"
                namespace Microsoft.Extensions.Data.Classification;

                public enum DataClass
                {
                    Unknown,
                    AccessControlData,
                    CustomerContent,
                    EUII,
                    SupportData,
                    FeedbackData,
                    AccountData,
                    PublicPersonalData,
                    EUPI,
                    OII,
                    SystemMetadata,
                    PublicNonPersonalData,
                }
            "
        },
        new object[]
        {
            2,
            "ApiLifecycle/Data/SomePackage.json",
            "SomePackage",
            new[] { DiagDescriptors.PublishedSymbolsCantBeMarkedExperimental.Id },
            @"

            namespace SomePackage;

            using System.Diagnostics.CodeAnalysis;

            [Experimental]
            public static class Test
            {
                [Experimental]
                public static void Load()
                {
                    // Intentionally left empty.
                }
            }
            "
        },
        new object[]
        {
            1,
            "ApiLifecycle/Data/Caching.Abstractions.json",
            "Microsoft.Extensions.Caching.Abstractions",
            new[] { DiagDescriptors.PublishedSymbolsCantBeDeleted.Id },
            @"
            using System;
            using Microsoft.Extensions.Caching.Distributed;

            namespace Microsoft.Extensions.Caching;

            public interface ICachePipelineBuilder
            {
                ICachePipelineBuilder SetCache(Func<IDistributedCache> createCache);

                ICachePipelineBuilder SetCache(IDistributedCache cache);

                ICachePipelineBuilder SetCache(Func<IServiceProvider, IDistributedCache> createCache, IServiceProvider serviceProvider);

                ICachePipelineBuilder AddCacheWrapper(Func<IDistributedCache, IDistributedCache> createCacheWrapper);

                ICachePipelineBuilder AddCacheWrapper(Func<IServiceProvider, IDistributedCache, IDistributedCache> createCacheWrapper, IServiceProvider serviceProvider);
            }
            "
        },
        new object[]
        {
            0,
            "ApiLifecycle/Data/Metering.Abstractions.json",
            "Microsoft.Extensions.Metering.Abstractions",
            new[] { DiagDescriptors.PublishedSymbolsCantBeDeleted.Id },
            @"
                using System;
                using System.Diagnostics;

                namespace Microsoft.Extensions.Metering;

                [AttributeUsage(AttributeTargets.Method)]
                [Conditional(""CODE_GENERATION_ATTRIBUTES"")]
                public sealed class CounterAttribute : Attribute
                {
                    public CounterAttribute(params string[] dimensions)
                    {
                        Dimensions = dimensions;
                    }

                    public CounterAttribute(Type type)
                    {
                        Type = type;
                    }

                    public string? Name { get; set; }

                    public string[]? Dimensions { get; }
                    public Type? Type { get; }
                }
            "
        },
        new object[]
        {
            2,
            "ApiLifecycle/Data/Essentials.json",
            "Microsoft.Extensions.Essentials",
            new[] { DiagDescriptors.PublishedSymbolsCantBeDeleted.Id },
            @"

            using System;
            using System.Collections;
            using System.Collections.Generic;

            namespace Microsoft.Extensions.Collections;

            public static class Empty
            {
                public static IReadOnlyCollection<T> ReadOnlyCollection<T>() => EmptyReadOnlyList<T>.Instance;

                public static IEnumerable<T> Enumerable<T>() => EmptyReadOnlyList<T>.Instance;

                public static IReadOnlyList<T> ReadOnlyList<T>() => EmptyReadOnlyList<T>.Instance;
            }


            internal sealed class EmptyReadOnlyList<T> : IReadOnlyList<T>
            {
                public static readonly EmptyReadOnlyList<T> Instance = new();
                private readonly Enumerator _enumerator = new();

                internal sealed class Enumerator : IEnumerator<T>
                {
                    public void Dispose()
                    {
                        // nop
                    }

                    public void Reset()
                    {
                        // nop
                    }

                    public bool MoveNext() => false;
                    public T Current => throw new InvalidOperationException();
                    object IEnumerator.Current => throw new InvalidOperationException();
                }

                public IEnumerator<T> GetEnumerator() => _enumerator;
                IEnumerator IEnumerable.GetEnumerator() => _enumerator;
                public int Count => 0;
                public T this[int index] => throw new ArgumentOutOfRangeException(nameof(index));
            }
        "
        },
        new object[]
        {
            0,
            "ApiLifecycle/Data/ExperimentalDeletedNoErorr.json",
            "Microsoft.Extensions.ExperimentalNoErrors",
            Array.Empty<string>(),
            ""
        },
        new object[]
        {
            0,
            "ApiLifecycle/Data/WindowsCountersOptions.json",
            "WindowsCountersOptions",
            Array.Empty<string>(),
            @"
                namespace Microsoft.Extensions.Diagnostics;

                using System;
                using System.ComponentModel.DataAnnotations;
                using System.Collections.Generic;
                using System.Diagnostics.CodeAnalysis;

                [Experimental]
                public class WindowsCountersOptions2
                {
                    [Required]
                    public ISet<string> InstanceIpAddresses { get; set; } = new HashSet<string>();

                    public TimeSpan CachingInterval { get; set; } = TimeSpan.FromSeconds(2);
                }
            "
        },
        new object[]
        {
            0,
            "ApiLifecycle/Data/Analyzers.json",
            "Microsoft.Extensions.Analyzers",
            Array.Empty<string>(),
            @"
                namespace Test;

                using System.Diagnostics.CodeAnalysis;

                [Experimental]
                public sealed class BufferWriter<T>
                {
                    internal const int MaxArrayLength = 0X7FEF_FFFF;   // Copy of the internal Array.MaxArrayLength const
                    private const int DefaultCapacity = 256;

                    private T[] _buffer = System.Array.Empty<T>();

                    public int WrittenMemory => 5;
                 }
            "
        },
        new object[]
        {
            0,
            "ApiLifecycle/Data/Empty.json",
            "Empty",
            Array.Empty<string>(),
            @"
                namespace Inheritance;

                using System.Diagnostics.CodeAnalysis;

                [Experimental]
                public class BaseType
                {
                    public virtual int P => 1;
                }

                public class InheritedType : BaseType
                {
                    public int ReadValue(string s) => P;
                }
            "
        },
        new object[]
        {
            0,
            "ApiLifecycle/Data/Empty.json",
            "Empty",
            Array.Empty<string>(),
            @"
                namespace Nested;

                using System.Diagnostics.CodeAnalysis;

                [Experimental]
                public class OuterType
                {
                    public int ReadValue(string s) => new InnerType().P;

                    public class InnerType
                    {
                        public int P => 2;
                    }
                }
            "
        }
    };

    public static IEnumerable<object[]> CodeWithMissingApis => new List<object[]>
    {
        new object[]
        {
            1,
            "ApiLifecycle/Data/Analyzers.json",
            "Microsoft.Extensions.Analyzers",
            DiagDescriptors.NewSymbolsMustBeMarkedExperimental,
            @"
                namespace Test;
                
                public sealed class /*0+*/BufferWriter/*-0*/
                {
                    public BufferWriter()
                    {
                    }

                    public int WrittenMemory => 5;
                 }
            "
        },
        new object[]
        {
            2,
            "ApiLifecycle/Data/Analyzers.json",
            "Microsoft.Extensions.Analyzers",
            DiagDescriptors.NewSymbolsMustBeMarkedExperimental,
            @"
                namespace Microsoft.Extensions.Diagnostics;

                using System.ComponentModel;

                public static class /*0+*/DebuggerState/*-0*/
                {
                    [EditorBrowsable(EditorBrowsableState.Never)]
                    public static IDebuggerState System => SystemDebugger.Instance;

                    [EditorBrowsable(EditorBrowsableState.Always)]
                    public static IDebuggerState Attached => AttachedDebugger.Instance;

                    public static IDebuggerState Detached => DetachedDebugger.Instance;
                }

                internal sealed class AttachedDebugger : IDebuggerState
                {
                    private AttachedDebugger()
                    {
                        // Intentionally left empty.
                    }

                    public static AttachedDebugger Instance { get; } = new();

                    public bool IsAttached => true;
                }

                internal sealed class DetachedDebugger : IDebuggerState
                {
                    private DetachedDebugger()
                    {
                        // Intentionally left empty.
                    }

                    public static DetachedDebugger Instance { get; } = new();

                    public bool IsAttached => false;
                }

                internal sealed class SystemDebugger : IDebuggerState
                {
                    private SystemDebugger()
                    {
                        // Intentionally left empty.
                    }

                    public static SystemDebugger Instance { get; } = new();

                    public bool IsAttached => System.Diagnostics.Debugger.IsAttached;
                }

                public interface /*1+*/IDebuggerState/*-1*/
                {
                    bool IsAttached { get; }
                }
            "
        },
        new object[]
        {
            1,
            "ApiLifecycle/Data/Analyzers.json",
            "Microsoft.Extensions.Analyzers",
            DiagDescriptors.NewSymbolsMustBeMarkedExperimental,
            @"
               namespace Test
               {
                    public static class /*0+*/Test/*-0*/
                    {
                        public static string Something()
                        {
                            return ""Hello"";
                        }
                    }
               }
             "
        }
    };
}
