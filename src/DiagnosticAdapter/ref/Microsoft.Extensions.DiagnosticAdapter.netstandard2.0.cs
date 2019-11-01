// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Extensions.DiagnosticAdapter
{
    public partial class DiagnosticNameAttribute : System.Attribute
    {
        public DiagnosticNameAttribute(string name) { }
        public string Name { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
    }
    public partial class DiagnosticSourceAdapter : System.IObserver<System.Collections.Generic.KeyValuePair<string, object>>
    {
        public DiagnosticSourceAdapter(object target) { }
        public DiagnosticSourceAdapter(object target, System.Func<string, bool> isEnabled) { }
        public DiagnosticSourceAdapter(object target, System.Func<string, bool> isEnabled, Microsoft.Extensions.DiagnosticAdapter.IDiagnosticSourceMethodAdapter methodAdapter) { }
        public DiagnosticSourceAdapter(object target, System.Func<string, object, object, bool> isEnabled) { }
        public DiagnosticSourceAdapter(object target, System.Func<string, object, object, bool> isEnabled, Microsoft.Extensions.DiagnosticAdapter.IDiagnosticSourceMethodAdapter methodAdapter) { }
        public bool IsEnabled(string diagnosticName) { throw null; }
        public bool IsEnabled(string diagnosticName, object arg1, object arg2 = null) { throw null; }
        void System.IObserver<System.Collections.Generic.KeyValuePair<System.String,System.Object>>.OnCompleted() { }
        void System.IObserver<System.Collections.Generic.KeyValuePair<System.String,System.Object>>.OnError(System.Exception error) { }
        void System.IObserver<System.Collections.Generic.KeyValuePair<System.String,System.Object>>.OnNext(System.Collections.Generic.KeyValuePair<string, object> value) { }
        public void Write(string diagnosticName, object parameters) { }
    }
    public partial interface IDiagnosticSourceMethodAdapter
    {
        System.Func<object, object, bool> Adapt(System.Reflection.MethodInfo method, System.Type inputType);
    }
    public partial class ProxyDiagnosticSourceMethodAdapter : Microsoft.Extensions.DiagnosticAdapter.IDiagnosticSourceMethodAdapter
    {
        public ProxyDiagnosticSourceMethodAdapter() { }
        public System.Func<object, object, bool> Adapt(System.Reflection.MethodInfo method, System.Type inputType) { throw null; }
    }
}
namespace Microsoft.Extensions.DiagnosticAdapter.Infrastructure
{
    public partial interface IProxy
    {
        T Upwrap<T>();
    }
    public partial interface IProxyFactory
    {
        TProxy CreateProxy<TProxy>(object obj);
    }
}
namespace Microsoft.Extensions.DiagnosticAdapter.Internal
{
    public partial class InvalidProxyOperationException : System.InvalidOperationException
    {
        public InvalidProxyOperationException(string message) { }
    }
    public abstract partial class ProxyBase : Microsoft.Extensions.DiagnosticAdapter.Infrastructure.IProxy
    {
        public readonly System.Type WrappedType;
        protected ProxyBase(System.Type wrappedType) { }
        public abstract object UnderlyingInstanceAsObject { get; }
        public T Upwrap<T>() { throw null; }
    }
    public partial class ProxyBase<T> : Microsoft.Extensions.DiagnosticAdapter.Internal.ProxyBase where T : class
    {
        public readonly T Instance;
        public ProxyBase(T instance) : base (default(System.Type)) { }
        public T UnderlyingInstance { get { throw null; } }
        public override object UnderlyingInstanceAsObject { get { throw null; } }
    }
    public partial class ProxyEnumerable<TSourceElement, TTargetElement> : System.Collections.Generic.IEnumerable<TTargetElement>, System.Collections.IEnumerable
    {
        public ProxyEnumerable(System.Collections.Generic.IEnumerable<TSourceElement> source, System.Type proxyType) { }
        public System.Collections.Generic.IEnumerator<TTargetElement> GetEnumerator() { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        public partial class ProxyEnumerator : System.Collections.Generic.IEnumerator<TTargetElement>, System.Collections.IEnumerator, System.IDisposable
        {
            public ProxyEnumerator(System.Collections.Generic.IEnumerator<TSourceElement> source, System.Type proxyType) { }
            public TTargetElement Current { get { throw null; } }
            object System.Collections.IEnumerator.Current { get { throw null; } }
            public void Dispose() { }
            public bool MoveNext() { throw null; }
            public void Reset() { }
        }
    }
    public partial class ProxyFactory : Microsoft.Extensions.DiagnosticAdapter.Infrastructure.IProxyFactory
    {
        public ProxyFactory() { }
        public TProxy CreateProxy<TProxy>(object obj) { throw null; }
    }
    public partial class ProxyList<TSourceElement, TTargetElement> : System.Collections.Generic.IEnumerable<TTargetElement>, System.Collections.Generic.IReadOnlyCollection<TTargetElement>, System.Collections.Generic.IReadOnlyList<TTargetElement>, System.Collections.IEnumerable
    {
        public ProxyList(System.Collections.Generic.IList<TSourceElement> source) { }
        protected ProxyList(System.Collections.Generic.IList<TSourceElement> source, System.Type proxyType) { }
        public int Count { get { throw null; } }
        public TTargetElement this[int index] { get { throw null; } }
        public System.Collections.Generic.IEnumerator<TTargetElement> GetEnumerator() { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
    }
    public partial class ProxyTypeCache : System.Collections.Concurrent.ConcurrentDictionary<System.Tuple<System.Type, System.Type>, Microsoft.Extensions.DiagnosticAdapter.Internal.ProxyTypeCacheResult>
    {
        public ProxyTypeCache() { }
    }
    public partial class ProxyTypeCacheResult
    {
        public ProxyTypeCacheResult() { }
        public System.Reflection.ConstructorInfo Constructor { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public string Error { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public bool IsError { get { throw null; } }
        public System.Tuple<System.Type, System.Type> Key { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public System.Type Type { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public static Microsoft.Extensions.DiagnosticAdapter.Internal.ProxyTypeCacheResult FromError(System.Tuple<System.Type, System.Type> key, string error) { throw null; }
        public static Microsoft.Extensions.DiagnosticAdapter.Internal.ProxyTypeCacheResult FromType(System.Tuple<System.Type, System.Type> key, System.Type type, System.Reflection.ConstructorInfo constructor) { throw null; }
    }
}
namespace System.Diagnostics
{
    public static partial class DiagnosticListenerExtensions
    {
        public static System.IDisposable SubscribeWithAdapter(this System.Diagnostics.DiagnosticListener diagnostic, object target) { throw null; }
        public static System.IDisposable SubscribeWithAdapter(this System.Diagnostics.DiagnosticListener diagnostic, object target, System.Func<string, bool> isEnabled) { throw null; }
        public static System.IDisposable SubscribeWithAdapter(this System.Diagnostics.DiagnosticListener diagnostic, object target, System.Func<string, object, object, bool> isEnabled) { throw null; }
    }
}
