/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import { assertMatchesSnapshot } from './infrastructure/TestUtilities';

// See GrammarTests.test.ts for details on exporting this test suite instead of running in place.

export function RunStyleBlockSuite() {
    describe('style block colors css', () => {
        it('colors declaration', async () => {
            await assertMatchesSnapshot(
`<style>
    hello {
        there: "friend"
    }
</style>`);
        });

        it('style block var plain', async () => {
            await assertMatchesSnapshot(
`<style>
    var x = "test";
</style>`);
        });
    });
}
