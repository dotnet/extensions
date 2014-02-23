namespace Microsoft.AspNet.DependencyInjection
{
    interface INestedProviderManager<T>
    {
        void Invoke(T context);
    }
}
