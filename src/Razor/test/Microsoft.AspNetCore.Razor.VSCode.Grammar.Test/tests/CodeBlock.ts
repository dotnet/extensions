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

    void SomeMethod(int value)
    {
        Action<int> template = @<strong>Value: @item</strong>;
        <section>
            This section is rendered when called: @template(1337)
        </section>
    }

    <p>aHello</p>

    if (true) {
        <p>alksdjfl</p>
    }
}`);
        });

        it('Nested local function with markup', async () => {
            await assertMatchesSnapshot(
                `@{
    void SomeMethod() { <p class="priority"><strong>Hello World</strong></p> }
}`);
        });

        it('Nested if statement with markup', async () => {
            await assertMatchesSnapshot(
                `@{
    if (true)
    {
        <p>Hello World!</p>
    }
}`);
        });

        it('Nested for statement with markup', async () => {
            await assertMatchesSnapshot(
                `@{
    for (var i = 0; i % 2 == 0; i++)
    {
        <p>@i</p>
    }
}`);
        });

        it('Nested foreach statement with markup', async () => {
            await assertMatchesSnapshot(
                `@{
    foreach (var person in people)
    {
        <p>@person</p>
    }
}`);
        });

        it('Nested do while statement with markup', async () => {
            await assertMatchesSnapshot(
                `@{
    var i = 0;
    do
    {
        <p>@i</p>
    } while (i++ != 10);
}`);
        });

        it('Nested while statement with markup', async () => {
            await assertMatchesSnapshot(
                `@{
    var i = 0;
    while (i++ != 10)
    {
        <p>@i</p>
    }
}`);
        });

        it('Nested lock statement with markup', async () => {
            await assertMatchesSnapshot(
                `@{
    lock (someObject)
    {
        <p>Hello World</p>
    }
}`);
        });

        it('Nested using statement with markup', async () => {
            await assertMatchesSnapshot(
                `@{
    using (someDisposable)
    {
        <p>Hello World</p>
    }
}`);
        });

        it('Nested try statement with markup', async () => {
            await assertMatchesSnapshot(
                `@{
    try
    {
        <p>Hello World</p>
    } catch (Exception ex){}
}`);
        });

        it('Nested switch statement with markup', async () => {
            await assertMatchesSnapshot(
                `@{
    switch (something)
    {
        case true:
            <p>Hello World</p>
            break;
    }
}`);
        });

        it('Complex with various nested statements', async () => {
            await assertMatchesSnapshot(
                `@{
    if (true) {
        <div>
        @using (someDisposable) {
            <p>Foo!</p>
            var x = 123;
            while (i++ % 2 == 0)
            {
                <strong>Bar</strong>
            }
        }
        </div>
    }
}`);
        });
    });
}
