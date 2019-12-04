/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import { assertMatchesSnapshot } from './infrastructure/TestUtilities';

// See GrammarTests.test.ts for details on exporting this test suite instead of running in place.

export function RunFunctionsDirectiveSuite() {
    describe('@functions directive', () => {
        it('No code block', async () => {
            await assertMatchesSnapshot('@functions');
        });

        it('Incomplete code block', async () => {
            await assertMatchesSnapshot('@functions {');
        });

        it('Single line', async () => {
            await assertMatchesSnapshot('@functions { public void Foo() {} }');
        });

        it('Multi line', async () => {
            await assertMatchesSnapshot(
                `@functions {
    private int currentCount = 0;

    private void IncrementCount()
    {
        currentCount++;
    }
}`);
        });
    });
}
