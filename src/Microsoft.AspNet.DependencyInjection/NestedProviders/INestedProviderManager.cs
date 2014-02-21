namespace Microsoft.AspNet.DependencyInjection
{
    interface INestedProviderManager<T>
    {
        void Invoke(NestedProviderContext<T> context);
    }
}
