/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import { assertMatchesSnapshot } from './infrastructure/TestUtilities';

// See GrammarTests.test.ts for details on exporting this test suite instead of running in place.

export function RunAddTagHelperDirectiveSuite() {
    describe('@addTagHelper directive', () => {
        it('No parameter', async () => {
            await assertMatchesSnapshot('@addTagHelper');
        });

        it('No parameter, spaced', async () => {
            await assertMatchesSnapshot('@addTagHelper                 ');
        });

        it('Incomplete parameter', async () => {
            await assertMatchesSnapshot('@addTagHelper "');
        });

        it('Unquoted parameter', async () => {
            await assertMatchesSnapshot('@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers');
        });

        it('Quoted parameter', async () => {
            await assertMatchesSnapshot('@addTagHelper "*, Microsoft.AspNetCore.Mvc.TagHelpers"');
        });

        it('Quoted parameter spaced', async () => {
            await assertMatchesSnapshot('@addTagHelper       "*     ,      Microsoft.AspNetCore.Mvc.TagHelpers   "            ');
        });
    });
}
