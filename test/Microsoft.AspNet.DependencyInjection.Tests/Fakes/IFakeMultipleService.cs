namespace Microsoft.AspNet.DependencyInjection.Tests.Fakes
{
    public interface IFakeMultipleService
    {
        string AnotherMethod();
    }

    public class FakeOneMultipleService : IFakeMultipleService
    {
        public string AnotherMethod()
        {
            return "FakeOneMultipleServiceAnotherMethod";
        }
    }

    public class FakeTwoMultipleService : IFakeMultipleService
    {
        public string AnotherMethod()
        {
            return "FakeTwoMultipleServiceAnotherMethod";
        }
    }
}