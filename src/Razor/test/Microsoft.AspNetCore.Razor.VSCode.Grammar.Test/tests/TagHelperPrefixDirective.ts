/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import { assertMatchesSnapshot } from './infrastructure/TestUtilities';

// See GrammarTests.test.ts for details on exporting this test suite instead of running in place.

export function RunTagHelperPrefixDirectiveSuite() {
    describe('@tagHelperPrefix directive', () => {
        it('No parameter', async () => {
            await assertMatchesSnapshot('@tagHelperPrefix');
        });

        it('No parameter, spaced', async () => {
            await assertMatchesSnapshot('@tagHelperPrefix                 ');
        });

        it('Incomplete parameter', async () => {
            await assertMatchesSnapshot('@tagHelperPrefix "');
        });

        it('Unquoted parameter', async () => {
            await assertMatchesSnapshot('@tagHelperPrefix th:');
        });

        it('Quoted parameter', async () => {
            await assertMatchesSnapshot('@tagHelperPrefix "th:"');
        });

        it('Quoted parameter spaced', async () => {
            await assertMatchesSnapshot('@tagHelperPrefix       "th:   "            ');
        });
    });
}
