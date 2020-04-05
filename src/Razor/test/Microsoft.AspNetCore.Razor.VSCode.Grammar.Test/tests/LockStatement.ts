/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import { assertMatchesSnapshot } from './infrastructure/TestUtilities';

// See GrammarTests.test.ts for details on exporting this test suite instead of running in place.

export function RunLockStatementSuite() {
    describe('@lock ( ... ) { ... }', () => {
        it('Incomplete lock statement, no reference or body', async () => {
            await assertMatchesSnapshot('@lock');
        });

        it('Incomplete lock statement, no reference', async () => {
            await assertMatchesSnapshot('@lock {}');
        });

        it('Single line', async () => {
            await assertMatchesSnapshot('@lock (someObject) { var x = 123;<p>Hello World</p> }');
        });

        it('Multi line reference', async () => {
            await assertMatchesSnapshot(
`@lock (
    await GetSomeObjectAsync(
        () => true,
        name: "The Good Disposable",
        new {
            Foo = false,
        }
)){}`);
        });

        it('Multi line body', async () => {
            await assertMatchesSnapshot(
`@lock (SomeObject)
{
    var x = 123;
    <div>
        @lock (GetAnotherObject()) {
            <p></p>
        }
    </div>
}`);
        });
    });
}
