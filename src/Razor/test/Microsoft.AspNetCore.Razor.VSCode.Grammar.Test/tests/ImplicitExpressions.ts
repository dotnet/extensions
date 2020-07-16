/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import { assertMatchesSnapshot } from './infrastructure/TestUtilities';

// See GrammarTests.test.ts for details on exporting this test suite instead of running in place.

export function RunImplicitExpressionSuite() {
    describe('Implicit Expressions', () => {
        it('Email address, not implicit expression', async () => {
            await assertMatchesSnapshot('abc@DateTime.Now');
        });

        it('Parenthesis prefix', async () => {
            await assertMatchesSnapshot(')@DateTime.Now');
        });

        it('Punctuation prefix', async () => {
            await assertMatchesSnapshot('.@DateTime.Now');
        });

        it('Close curly prefix', async () => {
            await assertMatchesSnapshot('}@DateTime.Now');
        });

        it('Empty', async () => {
            await assertMatchesSnapshot('@');
        });

        it('Open curly suffix', async () => {
            await assertMatchesSnapshot('@DateTime.Now{');
        });

        it('Close curly suffix', async () => {
            await assertMatchesSnapshot('@DateTime.Now}');
        });

        it('Close parenthesis suffix', async () => {
            await assertMatchesSnapshot('@DateTime.Now)');
        });

        it('Close parenthesis suffix', async () => {
            await assertMatchesSnapshot('@DateTime.Now]');
        });

        it('Single line simple', async () => {
            await assertMatchesSnapshot('@DateTime.Now');
        });

        it('Awaited property', async () => {
            await assertMatchesSnapshot('@await AsyncProperty');
        });

        it('Awaited method invocation', async () => {
            await assertMatchesSnapshot('@await AsyncMethod()');
        });

        it('Single line complex', async () => {
            await assertMatchesSnapshot('@DateTime!.Now()[1234 + 5678](abc()!.Current, 1 + 2 + getValue())?.ToString[123](() => 456)');
        });

        it('Combined with HTML', async () => {
            await assertMatchesSnapshot('<p>@DateTime.Now</p>');
        });

        it('Multi line', async () => {
            await assertMatchesSnapshot(
                `@DateTime!.Now()[1234 +
5678](
abc()!.Current,
1 + 2 + getValue())?.ToString[123](
() =>
{
    var x = 123;
    var y = true;

    return y ? x : 457;
})`);
        });
    });
}
