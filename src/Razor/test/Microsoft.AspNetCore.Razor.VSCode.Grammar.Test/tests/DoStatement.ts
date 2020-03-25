/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import { assertMatchesSnapshot } from './infrastructure/TestUtilities';

// See GrammarTests.test.ts for details on exporting this test suite instead of running in place.

export function RunDoStatementSuite() {
    describe('@do { ... } while ( ... );', () => {
        it('Incomplete do statement, no body', async () => {
            await assertMatchesSnapshot('@do');
        });

        it('Incomplete do while statement, no condition', async () => {
            await assertMatchesSnapshot('@do { } while ;');
        });

        it('Incomplete do while statement, no terminator', async () => {
            await assertMatchesSnapshot('@do { } while');
        });

        it('Single line', async () => {
            await assertMatchesSnapshot('@do { var x = 123;<p>Hello World</p> }while (   true  );');
        });

        it('Multi line condition', async () => {
            await assertMatchesSnapshot(
`@do
{
}
while (
    await GetATruthyValue(
        () => true,
        name: "Hello",
        new {
            Foo = false,
        }
));`);
        });

        it('Multi line body', async () => {
            await assertMatchesSnapshot(
`@do
{
    var x = 123;
    <div>
        @do {
            <p></p>
        } while(GetAnotherValue());
    </div>
}while(true);`);
        });
    });
}
