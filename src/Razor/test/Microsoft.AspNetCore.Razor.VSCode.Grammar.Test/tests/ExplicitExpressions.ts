/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import { assertMatchesSnapshot } from './infrastructure/TestUtilities';

// See GrammarTests.test.ts for details on exporting this test suite instead of running in place.

export function RunExplicitExpressionSuite() {
    describe('Explicit expressions', () => {
        it('Empty', async () => {
            await assertMatchesSnapshot('@()');
        });

        it('Single line simple', async () => {
            await assertMatchesSnapshot('@(DateTime.Now)');
        });

        it('Single line complex', async () => {
            await assertMatchesSnapshot('@(456 + new Array<int>(){1,2,3}[0] + await GetValueAsync<string>() ?? someArray[await DoMoreAsync(() => {})])');
        });

        it('Multi line', async () => {
            await assertMatchesSnapshot(
                `@(
    Html.BeginForm(
        "Login",
        "Home",
        new
        {
            @class = "someClass",
            notValid = Html.DisplayFor<object>(
                (_) => Model,
                "name",
                "someName",
                new { })
        })
)`);
        });
    });
}
