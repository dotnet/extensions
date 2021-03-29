/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import { assertMatchesSnapshot } from './infrastructure/TestUtilities';

// See GrammarTests.test.ts for details on exporting this test suite instead of running in place.

export function RunAttributeDirectiveSuite() {
    describe('@attribute directive', () => {
        it('No attribute', async () => {
            await assertMatchesSnapshot('@attribute');
        });

        it('As C# local', async () => {
            await assertMatchesSnapshot('@attribute.method()');
        });

        it('No attribute spaced', async () => {
            await assertMatchesSnapshot('@attribute              ');
        });

        it('Incomplete attribute, simple', async () => {
            await assertMatchesSnapshot('@attribute [CustomAttribute');
        });

        it('Incomplete attribute, generic', async () => {
            await assertMatchesSnapshot('@attribute [CustomAttribute(info = typeof(GenericClass1<string');
        });

        it('Single line, simple', async () => {
            await assertMatchesSnapshot('@attribute [Authorize]');
        });

        it('Multi line, complex', async () => {
            await assertMatchesSnapshot(`@attribute [
    CustomAttribute(
        Info = typeof(GenericClass<string>),
        Foo = true
    )
]`);
        });
    });
}
