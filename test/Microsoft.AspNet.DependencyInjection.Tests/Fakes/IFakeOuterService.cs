namespace Microsoft.AspNet.DependencyInjection.Tests.Fakes
{
    public interface IFakeOuterService
    {
        void Interrogate(out string singleValue, out string[] multipleValues);
    }
}