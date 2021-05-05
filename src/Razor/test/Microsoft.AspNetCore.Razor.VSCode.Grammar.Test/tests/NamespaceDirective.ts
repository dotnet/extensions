/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import { assertMatchesSnapshot } from './infrastructure/TestUtilities';

// See GrammarTests.test.ts for details on exporting this test suite instead of running in place.

export function RunNamespaceDirectiveSuite() {
    describe('@namespace directive', () => {
        it('No namespace', async () => {
            await assertMatchesSnapshot('@namespace');
        });

        it('No namespace spaced', async () => {
            await assertMatchesSnapshot('@namespace              ');
        });

        it('Incomplete namespace', async () => {
            await assertMatchesSnapshot('@namespace MyApp.');
        });

        it('Broken up namespace', async () => {
            await assertMatchesSnapshot('@namespace     MyApp  .  Models  .   Data    ');
        });

        it('Namespace provided', async () => {
            await assertMatchesSnapshot('@namespace MyApp');
        });

        it('Namespace provided spaced', async () => {
            await assertMatchesSnapshot('@namespace     MyApp.Models.Data    ');
        });
    });
}
