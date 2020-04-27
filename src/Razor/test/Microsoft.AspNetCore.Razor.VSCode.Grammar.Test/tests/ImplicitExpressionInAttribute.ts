/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import { assertMatchesSnapshot } from './infrastructure/TestUtilities';

// See GrammarTests.test.ts for details on exporting this test suite instead of running in place.

export function RunImplicitExpressionInAttributeSuite() {
    describe('Implicit Expressions In Attributes', () => {
        it('Double Quotes Class implicit expression', async () => {
            await assertMatchesSnapshot('<div class="@NavMenuCssClass"></div>');
        });

        it('Double Quotes Onclick implicit expression', async () => {
            await assertMatchesSnapshot('<button @onclick="@ToggleNavMenu()"></button>');
        });

        it('Double Quotes Onclick function name', async () => {
            await assertMatchesSnapshot('<button @onclick="ToggleNavMenu"></button>');
        });

        it('Double Quotes Email address, not implicit expression', async () => {
            await assertMatchesSnapshot('<button @onclick="abc@DateTime.Now"></button>');
        });

        it('Double Quotes Parenthesis prefix', async () => {
            await assertMatchesSnapshot('<button @onclick=")@DateTime.Now"></button>');
        });

        it('Double Quotes Punctuation prefix', async () => {
            await assertMatchesSnapshot('<button @onclick=".@DateTime.Now"></button>');
        });

        it('Double Quotes Close curly prefix', async () => {
            await assertMatchesSnapshot('<button @onclick="}@DateTime.Now"></button>');
        });

        it('Double Quotes Empty', async () => {
            await assertMatchesSnapshot('<button @onclick="@"></button>');
        });

        it('Double Quotes Open curly suffix', async () => {
            await assertMatchesSnapshot('<button @onclick="@DateTime.Now{"></button>');
        });

        it('Double Quotes Close curly suffix', async () => {
            await assertMatchesSnapshot('<button @onclick="@DateTime.Now}"></button>');
        });

        it('Double Quotes Close parenthesis suffix', async () => {
            await assertMatchesSnapshot('<button @onclick="@DateTime.Now)"></button>');
        });

        it('Double Quotes Close parenthesis suffix', async () => {
            await assertMatchesSnapshot('<button @onclick="@DateTime.Now]"></button>');
        });

        it('Double Quotes Single line simple', async () => {
            await assertMatchesSnapshot('<button @onclick="@DateTime.Now"></button>');
        });

        it('Double Quotes Awaited property', async () => {
            await assertMatchesSnapshot('<button @onclick="@await AsyncProperty"></button>');
        });

        it('Double Quotes Awaited method invocation', async () => {
            await assertMatchesSnapshot('<button @onclick="@await AsyncMethod()"></button>');
        });

        it('Double Quotes Single line complex', async () => {
            await assertMatchesSnapshot('<button @onclick="@DateTime!.Now()[1234 + 5678](abc()!.Current, 1 + 2 + getValue())?.ToString[123](() => 456)"></button>');
        });

        it('Double Quotes Combined with HTML', async () => {
            await assertMatchesSnapshot('<button @onclick="<p>@DateTime.Now</p>"></button>');
        });

        it('Double Quotes Multi line', async () => {
            await assertMatchesSnapshot(
                `<button @onclick="@DateTime!.Now()[1234 +
5678](
abc()!.Current,
1 + 2 + getValue())?.ToString[123](
() =>
{
    var x = 123;
    var y = true;

    return y ? x : 457;
})"></button>`);
        });

        it('Single Quotes Class implicit expression', async () => {
            await assertMatchesSnapshot('<div class=\'@NavMenuCssClass\'></div>');
        });

        it('Single Quotes Onclick implicit expression', async () => {
            await assertMatchesSnapshot('<button @onclick=\'@ToggleNavMenu()\'></button>');
        });

        it('Single Quotes Onclick function name', async () => {
            await assertMatchesSnapshot('<button @onclick=\'ToggleNavMenu\'></button>');
        });

        it('Single Quotes Email address, not implicit expression', async () => {
            await assertMatchesSnapshot('<button @onclick=\'abc@DateTime.Now\'></button>');
        });

        it('Single Quotes Parenthesis prefix', async () => {
            await assertMatchesSnapshot('<button @onclick=\')@DateTime.Now\'></button>');
        });

        it('Single Quotes Punctuation prefix', async () => {
            await assertMatchesSnapshot('<button @onclick=\'.@DateTime.Now\'></button>');
        });

        it('Single Quotes Close curly prefix', async () => {
            await assertMatchesSnapshot('<button @onclick=\'}@DateTime.Now\'></button>');
        });

        it('Single Quotes Empty', async () => {
            await assertMatchesSnapshot('<button @onclick=\'@\'></button>');
        });

        it('Single Quotes Open curly suffix', async () => {
            await assertMatchesSnapshot('<button @onclick=\'@DateTime.Now{\'></button>');
        });

        it('Single Quotes Close curly suffix', async () => {
            await assertMatchesSnapshot('<button @onclick=\'@DateTime.Now}\'></button>');
        });

        it('Single Quotes Close parenthesis suffix', async () => {
            await assertMatchesSnapshot('<button @onclick=\'@DateTime.Now)\'></button>');
        });

        it('Single Quotes Close parenthesis suffix', async () => {
            await assertMatchesSnapshot('<button @onclick=\'@DateTime.Now]\'></button>');
        });

        it('Single Quotes Single line simple', async () => {
            await assertMatchesSnapshot('<button @onclick=\'@DateTime.Now\'></button>');
        });

        it('Single Quotes Awaited property', async () => {
            await assertMatchesSnapshot('<button @onclick=\'@await AsyncProperty\'></button>');
        });

        it('Single Quotes Awaited method invocation', async () => {
            await assertMatchesSnapshot('<button @onclick=\'@await AsyncMethod()\'></button>');
        });

        it('Single Quotes Single line complex', async () => {
            await assertMatchesSnapshot('<button @onclick=\'@DateTime!.Now()[1234 + 5678](abc()!.Current, 1 + 2 + getValue())?.ToString[123](() => 456)\'></button>');
        });

        it('Single Quotes Combined with HTML', async () => {
            await assertMatchesSnapshot('<button @onclick=\'<p>@DateTime.Now</p>\'></button>');
        });

        it('Single Quotes Multi line', async () => {
            await assertMatchesSnapshot(
                `<button @onclick=\'@DateTime!.Now()[1234 +
5678](
abc()!.Current,
1 + 2 + getValue())?.ToString[123](
() =>
{
    var x = 123;
    var y = true;

    return y ? x : 457;
})\'></button>`);
        });
    });
}
