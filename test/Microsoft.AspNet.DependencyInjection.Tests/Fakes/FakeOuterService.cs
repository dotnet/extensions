using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNet.DependencyInjection.Tests.Fakes
{
    public class FakeOuterService : IFakeOuterService
    {
        private readonly IFakeService _singleService;
        private readonly IEnumerable<IFakeMultipleService> _multipleServices;

        public FakeOuterService(
            IFakeService singleService,
            IEnumerable<IFakeMultipleService> multipleServices)
        {
            _singleService = singleService;
            _multipleServices = multipleServices;
        }


        public void Interrogate(out string singleValue, out string[] multipleValues)
        {
            singleValue = _singleService.SimpleMethod();

            multipleValues = _multipleServices
                .Select(x => x.SimpleMethod())
                .ToArray();
        }
    }
}