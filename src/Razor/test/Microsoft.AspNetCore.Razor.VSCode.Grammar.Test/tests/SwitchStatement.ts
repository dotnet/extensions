/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import { assertMatchesSnapshot } from './infrastructure/TestUtilities';

// See GrammarTests.test.ts for details on exporting this test suite instead of running in place.

export function RunSwitchStatementSuite() {
    describe('@switch ( ... ) { ... }', () => {
        it('Incomplete switch statement, no condition or body', async () => {
            await assertMatchesSnapshot('@switch');
        });

        it('Incomplete switch statement, no condition', async () => {
            await assertMatchesSnapshot('@switch {}');
        });

        it('Incomplete switch statement, no cases', async () => {
            await assertMatchesSnapshot('@switch (condition) {Console.WriteLine("Invalid?")}');
        });

        it('Single line', async () => {
            await assertMatchesSnapshot('@switch (   value  ) { case 123: var x = 123;<p>Hello World</p>break; }');
        });

        it('Multi line condition', async () => {
            await assertMatchesSnapshot(
`@switch (
    await GetAValue(
        () => true,
        name: "Hello",
        new {
            Foo = false,
        }
)){}`);
        });

        it('Multi line body', async () => {
            await assertMatchesSnapshot(
`@switch (SomeProperty)
{
    case 123:
        var x = 123;
        <div>
            @switch (GetAnotherValue()) {
                case: 456
                    <p></p>
                break;
            }
        </div>
        break;
}`);
        });
    });
}
