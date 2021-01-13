/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import { assertMatchesSnapshot } from './infrastructure/TestUtilities';

// See GrammarTests.test.ts for details on exporting this test suite instead of running in place.

export function RunForeachStatementSuite() {
    describe('@foreach ( ... ) { ... }', () => {
        it('Incomplete await foreach statement, no condition or body', async () => {
            await assertMatchesSnapshot('@await foreach');
        });

        it('Incomplete foreach statement, no condition or body', async () => {
            await assertMatchesSnapshot('@foreach');
        });

        it('Incomplete foreach statement, no condition', async () => {
            await assertMatchesSnapshot('@foreach {}');
        });

        it('Incomplete await foreach statement, no condition', async () => {
            await assertMatchesSnapshot('@await foreach {}');
        });

        it('Single line', async () => {
            await assertMatchesSnapshot('@foreach (var value in Values)) { var x = 123;<p>Hello World @value</p> }');
        });

        it('Awaited foreach', async () => {
            await assertMatchesSnapshot('@await foreach (var value in Values)) { var x = 123;<p>Hello World @value</p> }');
        });

        it('Multi line condition', async () => {
            await assertMatchesSnapshot(
`@foreach (
    var value in await GetMaxIncrementCount(
        () => true,
        name: "The Good Value",
        new {
            Foo = false,
        })
){@value}`);
        });

        it('Multi line body', async () => {
            await assertMatchesSnapshot(
`@foreach (List<int> numbers in LotsOfNumbers)
{
    var x = 123;
    <div>
        @foreach (int i in numbers) {
            <p>@i</p>
        }
    </div>
}`);
        });
    });
}
