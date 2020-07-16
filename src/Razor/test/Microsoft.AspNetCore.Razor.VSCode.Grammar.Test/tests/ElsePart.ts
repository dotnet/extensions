/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import { assertMatchesSnapshot } from './infrastructure/TestUtilities';

// See GrammarTests.test.ts for details on exporting this test suite instead of running in place.

export function RunElsePartSuite() {
    describe('else [if ( ... )] { ... }', () => {
        it('Incomplete else, no condition or body', async () => {
            await assertMatchesSnapshot('else');
        });

        it('Invalid else suffix', async () => {
            await assertMatchesSnapshot('else notValid');
        });

        it('Incomplete else if part, no condition', async () => {
            await assertMatchesSnapshot('else if {}');
        });

        it('Unspaced else if part', async () => {
            await assertMatchesSnapshot('elseif (true) { /* The elseif should not be control scoped */ }');
        });

        it('Invalid else part', async () => {
            await assertMatchesSnapshot('<p> else if (shouldNotBeElse) {}</p>');
        });

        it('Single line', async () => {
            await assertMatchesSnapshot('else { var x = 123;<p>Hello World</p> }');
        });

        it('Single line, condition', async () => {
            await assertMatchesSnapshot('else if (true) { var x = 123;<p>Hello World</p> }');
        });

        it('Multi line condition', async () => {
            await assertMatchesSnapshot(
                `else if (
    await GetTrueValue(
        () => true,
        name: "The Good Identifier",
        new {
            Foo = false,
        }
)){}`);
        });

        it('Multi line body', async () => {
            await assertMatchesSnapshot(
                `else if (1 + 1 == 2)
{
    var x = 123;
    <div>
        @if ( AnotherCondition() ) {
            <p></p>
        }
    </div>
}`);
        });

        it('Else integrates with @if, complex', async () => {
            await assertMatchesSnapshot(
                `@if (1 + 1 == 2)
{
    <p>If else!</p>
} else
{
    var x = 123;
    <div>
        @if (true) {} else if (false) {
            <strong> Woa! </strong>
            var y = 456;
        }
    </div>
}`);
        });
    });
}
