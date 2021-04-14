/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import { assertMatchesSnapshot } from './infrastructure/TestUtilities';

// See GrammarTests.test.ts for details on exporting this test suite instead of running in place.

export function RunSectionDirectiveSuite() {
    describe('@section directive', () => {
        it('No name', async () => {
            await assertMatchesSnapshot('@section');
        });

        it('As C# local', async () => {
            await assertMatchesSnapshot('@section.method()');
        });

        it('No name, spaced', async () => {
            await assertMatchesSnapshot('@section                 ');
        });

        it('Invalid name', async () => {
            await assertMatchesSnapshot('@section -$*&^');
        });

        it('Single line incomplete body', async () => {
            await assertMatchesSnapshot('@section Name {');
        });

        it('Multi line incomplete body', async () => {
            await assertMatchesSnapshot(`@section
Name
{`);
        });

        it('Single line', async () => {
            await assertMatchesSnapshot('@section Name {@DateTime.Now}');
        });

        it('Multi line complex', async () => {
            await assertMatchesSnapshot(
`@section Name {
    <section>
        @DateTime.Now
        @section INVALID {}
    </section>
}`);
        });
    });
}
