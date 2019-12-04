/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import { assertMatchesSnapshot } from './infrastructure/TestUtilities';

// See GrammarTests.test.ts for details on exporting this test suite instead of running in place.

export function RunRemoveTagHelperDirectiveSuite() {
    describe('@removeTagHelper directive', () => {
        it('No parameter', async () => {
            await assertMatchesSnapshot('@removeTagHelper');
        });

        it('No parameter, spaced', async () => {
            await assertMatchesSnapshot('@removeTagHelper                 ');
        });

        it('Incomplete parameter', async () => {
            await assertMatchesSnapshot('@removeTagHelper "');
        });

        it('Unquoted parameter', async () => {
            await assertMatchesSnapshot('@removeTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers');
        });

        it('Quoted parameter', async () => {
            await assertMatchesSnapshot('@removeTagHelper "*, Microsoft.AspNetCore.Mvc.TagHelpers"');
        });

        it('Quoted parameter spaced', async () => {
            await assertMatchesSnapshot('@removeTagHelper       "*     ,      Microsoft.AspNetCore.Mvc.TagHelpers   "            ');
        });
    });
}
