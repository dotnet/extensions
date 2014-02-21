using System;

namespace Microsoft.AspNet.DependencyInjection
{
    public interface INestedProvider<T>
    {
        int Order { get; }

        void Invoke(NestedProviderContext<T> context, Action callNext);
    }
}
