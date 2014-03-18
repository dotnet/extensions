using System;
using System.Linq;
using System.Reflection;

namespace Microsoft.AspNet.DependencyInjection
{
    public class TypeActivator : ITypeActivator
    {
        private readonly IServiceProvider _services;

        public TypeActivator(IServiceProvider services)
        {
            _services = services;
        }

        public object CreateInstance(Type instanceType, params object[] parameters)
        {
            int bestLength = -1;
            ConstructorMatcher bestMatcher = null;

            foreach (var matcher in instanceType
                .GetTypeInfo()
                .DeclaredConstructors
                .Select(constructor => new ConstructorMatcher(constructor)))
            {
                var length = matcher.Match(parameters);
                if (length == -1)
                {
                    continue;
                }
                if (bestLength < length)
                {
                    bestLength = length;
                    bestMatcher = matcher;
                }
            }

            if (bestMatcher == null)
            {
                throw new Exception(
                    string.Format(
                        "TODO: unable to locate suitable constructor for {0}. " + 
                        "Ensure 'instanceType' is concrete and all parameters are accepted by a constructor.",
                        instanceType));
            }

            return bestMatcher.CreateInstance(_services);
        }

        class ConstructorMatcher
        {
            private readonly ConstructorInfo _constructor;
            private readonly ParameterInfo[] _parameters;
            private readonly object[] _parameterValues;
            private readonly bool[] _parameterValuesSet;

            public ConstructorMatcher(ConstructorInfo constructor)
            {
                _constructor = constructor;
                _parameters = _constructor.GetParameters();
                _parameterValuesSet = new bool[_parameters.Length];
                _parameterValues = new object[_parameters.Length];
            }

            public int Match(object[] givenParameters)
            {

                var applyIndexStart = 0;
                var applyExactLength = 0;
                for (var givenIndex = 0; givenIndex != givenParameters.Length; ++givenIndex)
                {
                    var givenType = givenParameters[givenIndex] == null ? null : givenParameters[givenIndex].GetType().GetTypeInfo();
                    var givenMatched = false;

                    for (var applyIndex = applyIndexStart; givenMatched == false && applyIndex != _parameters.Length; ++applyIndex)
                    {
                        if (_parameterValuesSet[applyIndex] == false &&
                            _parameters[applyIndex].ParameterType.GetTypeInfo().IsAssignableFrom(givenType))
                        {
                            givenMatched = true;
                            _parameterValuesSet[applyIndex] = true;
                            _parameterValues[applyIndex] = givenParameters[givenIndex];
                            if (applyIndexStart == applyIndex)
                            {
                                applyIndexStart++;
                                if (applyIndex == givenIndex)
                                {
                                    applyExactLength = applyIndex;
                                }
                            }
                        }
                    }

                    if (givenMatched == false)
                    {
                        return -1;
                    }
                }
                return applyExactLength;
            }

            public object CreateInstance(IServiceProvider _services)
            {
                for (var index = 0; index != _parameters.Length; ++index)
                {
                    if (_parameterValuesSet[index] == false)
                    {
                        var value = _services.GetService(_parameters[index].ParameterType);
                        if (value == null)
                        {
                            throw new Exception(string.Format("TODO: unable to resolve service {1} to create {0}", _constructor.DeclaringType, _parameters[index].ParameterType));
                        }
                        _parameterValues[index] = value;
                    }
                }
                return _constructor.Invoke(_parameterValues);
            }
        }
    }
}
