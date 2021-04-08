/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import { assertMatchesSnapshot } from './infrastructure/TestUtilities';

// See GrammarTests.test.ts for details on exporting this test suite instead of running in place.

export function RunModelDirectiveSuite() {
    describe('@model directive', () => {
        it('No model', async () => {
            await assertMatchesSnapshot('@model');
        });

        it('No model spaced', async () => {
            await assertMatchesSnapshot('@model              ');
        });

        it('Incomplete model, generic', async () => {
            await assertMatchesSnapshot('@model List<string');
        });

        it('Incomplete model, tuple', async () => {
            await assertMatchesSnapshot('@model (string abc, bool def');
        });

        it('Model provided', async () => {
            await assertMatchesSnapshot('@model Person');
        });

        it('Model provided spaced', async () => {
            await assertMatchesSnapshot('@model              Person         ');
        });
    });
}
