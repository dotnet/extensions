namespace Microsoft.AspNet.DependencyInjection.Tests.Fakes
{
    public interface IFakeMultipleService : IFakeService
    {
    }

    public class FakeOneMultipleService : IFakeMultipleService
    {
        public string SimpleMethod()
        {
            return "FakeOneMultipleServiceAnotherMethod";
        }
    }

    public class FakeTwoMultipleService : IFakeMultipleService
    {
        public string SimpleMethod()
        {
            return "FakeTwoMultipleServiceAnotherMethod";
        }
    }
}