/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import { assertMatchesSnapshot } from './infrastructure/TestUtilities';

// See GrammarTests.test.ts for details on exporting this test suite instead of running in place.

export function RunCodeBlockSuite() {
    describe('Razor code blocks @{ ... }', () => {
        it('Malformed code block', async () => {
            await assertMatchesSnapshot('@ {}');
        });

        it('Incomplete code block', async () => {
            await assertMatchesSnapshot('@{');
        });

        it('Empty code block', async () => {
            await assertMatchesSnapshot('@{}');
        });

        it('Single line local function', async () => {
            await assertMatchesSnapshot('@{ void Foo() {} }');
        });

        it('Top level text tag', async () => {
            await assertMatchesSnapshot('@{ <text>Hello</text> }');
        });

        it('Nested text text tag', async () => {
            await assertMatchesSnapshot('@{ <text><text>Hello</text></text> }');
        });

        it('Nested text tag', async () => {
            await assertMatchesSnapshot('@{ <p><text>Hello</text></p> }');
        });

        it('Single line markup simple', async () => {
            await assertMatchesSnapshot(
`@{
    @: <p> Incomplete
}`);
        });

        it('Single line markup complex', async () => {
            await assertMatchesSnapshot(
`@{
    @:@DateTime.Now <text>Nope</text>
}`);
        });

        it('Complex HTML tag structures', async () => {
            await assertMatchesSnapshot('@{<p><input      /><strong>Hello <hr/> <br> World</strong></p>}');
        });

        it('Pure C#', async () => {
            await assertMatchesSnapshot('@{var x = true; Console.WriteLine("Hello World");}');
        });

        it('Multi line complex', async () => {
            await assertMatchesSnapshot(
                `@{
    var x = true;
    <text>
        @{
            @DateTime.Now
            @{
                @{

                }
            }
        }
    </text>

    <p></p>
<p class="hello <world></p>" @DateTime.Now> Foo<strong @{ <text> can't believe this works </text>}>Bar</strong> Baz
        <p class="hello world">
            Below is an incomplete tag
            </strong>
        </p>

        <text>This is not a special transition tag</text>
        Hello World
    </p>
    @: <strong> <-- This is incomplete @DateTime.Now

    <input class="hello world">
    <p>aHello</p>

    if (true) {
        <p>alksdjfl</p>
    }
}`);
        });
    });
}
