/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import { assertMatchesSnapshot } from './infrastructure/TestUtilities';

// See GrammarTests.test.ts for details on exporting this test suite instead of running in place.

export function RunRazorTemplateSuite() {
    describe('Razor templates @<p>....</p>', () => {
        it('Malformed', async () => {
            await assertMatchesSnapshot('@ <p></p>');
        });

        it('Malformed attributes', async () => {
            await assertMatchesSnapshot('@<p class="></p>');
        });

        it('Incomplete', async () => {
            await assertMatchesSnapshot('@<strong>');
        });

        it('Empty', async () => {
            await assertMatchesSnapshot('@<p></p>');
        });

        it('Void', async () => {
            await assertMatchesSnapshot('@<hr>');
        });

        it('Self-closing', async () => {
            await assertMatchesSnapshot('@<hr class="stuff" />');
        });

        it('Single line local variable', async () => {
            await assertMatchesSnapshot('@{ Action<object> abc = @<div>Hello World</p>; }');
        });

        it('Nested template', async () => {
            await assertMatchesSnapshot('@{ Action<object> abc = @<div>@{ Action<object> def = @<p class="john" onclick=\'someMethod\'>Hello World</p>; }</div>; }');
        });

        it('Complex HTML tag structure non-template', async () => {
            await assertMatchesSnapshot('@{@<p><input      /><strong>Hello <hr/> <br> World</strong></p>}');
        });

        it('Nested template in @code directive', async () => {
            await assertMatchesSnapshot(
                `@code {
    public Action<int> GetTemplate()
    {
        Action<int> template = @<p>@item</p>;
        return template;
    }
}`);
        });

        it('Nested template in @functions directive', async () => {
            await assertMatchesSnapshot(
                `@functions {
    public Action<int> GetTemplate()
    {
        Action<int> template = @<p>@item</p>;
        return template;
    }
}`);
        });

        it('Nested template in implicit expression', async () => {
            await assertMatchesSnapshot(
                `@CallSomeMethod(@<p style="margin:1px;">
    The Template
</p>)`);
        });

        it('Nested template in explicit expression', async () => {
            await assertMatchesSnapshot(
                `@(CallSomeMethod(@<p>
    The Template
</p>))`);
        });

        it('Nested for statement with templates', async () => {
            await assertMatchesSnapshot(
                `@{
    for (var i = 0; i % 2 == 0; i++)
    {
        Action<int> template = @<p>@item</p>;
        @template(i)
    }
}`);
        });

        it('Nested foreach statement with template', async () => {
            await assertMatchesSnapshot(
                `@{
    foreach (var person in people)
    {
        Action<int> template = @<p>@item</p>;
        @template(i)
    }
}`);
        });

        it('Nested do while statement with template', async () => {
            await assertMatchesSnapshot(
                `@{
    var i = 0;
    do
    {
        Action<int> template = @<p>@item</p>;
        @template(i)
    } while (i++ != 10);
}`);
        });

        it('Nested while statement with template', async () => {
            await assertMatchesSnapshot(
                `@{
    var i = 0;
    while (i++ != 10)
    {
        Action<int> template = @<p>@item</p>;
        @template(i)
    }
}`);
        });

        it('Nested lock statement with template', async () => {
            await assertMatchesSnapshot(
                `@{
    lock (someObject)
    {
        Action<string> template = @<p class="world">@item</p>;
        @template(someObject.ToString())
    }
}`);
        });

        it('Nested using statement with template', async () => {
            await assertMatchesSnapshot(
                `@{
    using (someDisposable)
    {
        Action<int> template = @<p>@item</p>;
    }
}`);
        });

        it('Nested try statement with template', async () => {
            await assertMatchesSnapshot(
                `@{
    try
    {
        Action<int> template = @<p>@item</p>;
    } catch (Exception ex){}
}`);
        });

        it('Nested switch statement with template', async () => {
            await assertMatchesSnapshot(
                `@{
    switch (something)
    {
        case true:
            Action<int> template = @<p>@item</p>;
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
                Action<int> template = @<p>
                    @item

                    @{
                        Action anotherTemplate = @<strong class='really loud!'>LOUD</strong>;
                    }
                    </p>;
            }
        }
        </div>
    }
}`);
        });
    });
}
