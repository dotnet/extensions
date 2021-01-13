/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import { assertMatchesSnapshot } from './infrastructure/TestUtilities';

// See GrammarTests.test.ts for details on exporting this test suite instead of running in place.

export function RunImplementsDirectiveSuite() {
    describe('@implements directive', () => {
        it('No type', async () => {
            await assertMatchesSnapshot('@implements');
        });

        it('No type spaced', async () => {
            await assertMatchesSnapshot('@implements              ');
        });

        it('Incomplete type, generic', async () => {
            await assertMatchesSnapshot('@implements SomeInterface<string');
        });

        it('Type provided', async () => {
            await assertMatchesSnapshot('@implements Person');
        });

        it('Type provided spaced', async () => {
            await assertMatchesSnapshot('@implements              Person         ');
        });
    });
}
