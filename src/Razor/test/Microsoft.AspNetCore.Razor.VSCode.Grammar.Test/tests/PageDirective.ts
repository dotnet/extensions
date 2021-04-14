/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import { assertMatchesSnapshot } from './infrastructure/TestUtilities';

// See GrammarTests.test.ts for details on exporting this test suite instead of running in place.

export function RunPageDirectiveSuite() {
    describe('@page directive', () => {
        it('No route', async () => {
            await assertMatchesSnapshot('@page');
        });

        it('No route spaced', async () => {
            await assertMatchesSnapshot('@page              ');
        });

        it('Incomplete route', async () => {
            await assertMatchesSnapshot('@page "');
        });

        it('Routed', async () => {
            await assertMatchesSnapshot('@page "/counter"');
        });

        it('Routed spaced', async () => {
            await assertMatchesSnapshot('@page              "/counter"         ');
        });
    });
}
