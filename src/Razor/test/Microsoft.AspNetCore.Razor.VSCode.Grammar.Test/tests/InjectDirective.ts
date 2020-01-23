/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import { assertMatchesSnapshot } from './infrastructure/TestUtilities';

// See GrammarTests.test.ts for details on exporting this test suite instead of running in place.

export function RunInjectDirectiveSuite() {
    describe('@inject directive', () => {
        it('No parameters', async () => {
            await assertMatchesSnapshot('@inject');
        });

        it('No parameters spaced', async () => {
            await assertMatchesSnapshot('@inject              ');
        });

        it('Incomplete type, generic', async () => {
            await assertMatchesSnapshot('@inject List<string');
        });

        it('Incomplete type, tuple', async () => {
            await assertMatchesSnapshot('@inject (string abc, bool def');
        });

        it('Invalid property', async () => {
            await assertMatchesSnapshot('@inject DateTime !Something');
        });

        it('Fulfilled inject', async () => {
            await assertMatchesSnapshot('@inject DateTime TheTime');
        });

        it('Fulfilled inject spaced', async () => {
            await assertMatchesSnapshot('@inject      DateTime        TheTime         ');
        });
    });
}
