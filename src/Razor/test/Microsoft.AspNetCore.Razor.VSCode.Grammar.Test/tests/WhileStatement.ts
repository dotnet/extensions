/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import { assertMatchesSnapshot } from './infrastructure/TestUtilities';

// See GrammarTests.test.ts for details on exporting this test suite instead of running in place.

export function RunWhileStatementSuite() {
    describe('@while ( ... ) { ... }', () => {
        it('Incomplete while statement, no condition or body', async () => {
            await assertMatchesSnapshot('@while');
        });

        it('Incomplete while statement, no condition', async () => {
            await assertMatchesSnapshot('@while {}');
        });

        it('Single line', async () => {
            await assertMatchesSnapshot('@while (   true  ) { var x = 123;<p>Hello World</p> }');
        });

        it('Multi line condition', async () => {
            await assertMatchesSnapshot(
`@while (
    await GetATruthyValue(
        () => true,
        name: "Hello",
        new {
            Foo = false,
        }
)){}`);
        });

        it('Multi line body', async () => {
            await assertMatchesSnapshot(
`@while (SomeProperty)
{
    var x = 123;
    <div>
        @while (GetAnotherValue()) {
            <p></p>
        }
    </div>
}`);
        });
    });
}
