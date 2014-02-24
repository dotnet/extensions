namespace Microsoft.AspNet.DependencyInjection.Tests.Fakes
{
    public class AnotherClassAcceptingData
    {
        private readonly IFakeService _fakeService;
        private readonly string _one;
        private readonly string _two;

        public AnotherClassAcceptingData(IFakeService fakeService, string one, string two)
        {
            _fakeService = fakeService;
            _one = one;
            _two = two;
        }

        public string LessSimpleMethod()
        {
            return string.Format("[{0}] {1} {2}", _fakeService.SimpleMethod(), _one, _two);
        }
    }
}