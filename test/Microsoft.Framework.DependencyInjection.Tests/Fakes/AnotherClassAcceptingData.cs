// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

namespace Microsoft.Framework.DependencyInjection.Tests.Fakes
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