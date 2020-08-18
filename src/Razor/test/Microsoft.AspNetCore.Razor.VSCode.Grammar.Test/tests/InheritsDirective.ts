/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import { assertMatchesSnapshot } from './infrastructure/TestUtilities';

// See GrammarTests.test.ts for details on exporting this test suite instead of running in place.

export function RunInheritsDirectiveSuite() {
    describe('@inherits directive', () => {
        it('No type', async () => {
            await assertMatchesSnapshot('@inherits');
        });

        it('No type spaced', async () => {
            await assertMatchesSnapshot('@inherits              ');
        });

        it('Incomplete type, generic', async () => {
            await assertMatchesSnapshot('@inherits SomeViewBase<string');
        });

        it('Type provided', async () => {
            await assertMatchesSnapshot('@inherits Person');
        });

        it('Type provided spaced', async () => {
            await assertMatchesSnapshot('@inherits              Person         ');
        });
    });
}
