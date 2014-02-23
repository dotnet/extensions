using System;

namespace Microsoft.AspNet.DependencyInjection
{
    public interface INestedProvider<T>
    {
        int Order { get; }

        void Invoke(T context, Action callNext);
    }
}
