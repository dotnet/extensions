/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import { assertMatchesSnapshot } from './infrastructure/TestUtilities';

// See GrammarTests.test.ts for details on exporting this test suite instead of running in place.

export function RunUsingDirectiveSuite() {
    describe('@using directive', () => {
        it('Standard using, no namespace', async () => {
            await assertMatchesSnapshot('@using');
        });

        it('As C# local', async () => {
            await assertMatchesSnapshot('@using.method()');
        });

        it('Standard using, no namespace spaced', async () => {
            await assertMatchesSnapshot('@using              ');
        });

        it('Standard using', async () => {
            await assertMatchesSnapshot('@using System.IO');
        });

        it('Standard using spaced', async () => {
            await assertMatchesSnapshot('@using              System.IO         ');
        });

        it('Standard using, optional semicolon', async () => {
            await assertMatchesSnapshot('@using System.IO;');
        });

        it('Static using, no namespace', async () => {
            await assertMatchesSnapshot('@using static');
        });

        it('Static using, no namespace spaced', async () => {
            await assertMatchesSnapshot('@using        static      ');
        });

        it('Static using', async () => {
            await assertMatchesSnapshot('@using static System.Math');
        });

        it('Static using spaced', async () => {
            await assertMatchesSnapshot('@using    static          System.Math         ');
        });

        it('Static using, optional semicolon', async () => {
            await assertMatchesSnapshot('@using static System.Math;');
        });

        it('Using alias, no type', async () => {
            await assertMatchesSnapshot('@using Something =');
        });

        it('Using alias, no type spaced', async () => {
            await assertMatchesSnapshot('@using        Something   =    ');
        });

        it('Using alias, incomplete type, generic', async () => {
            await assertMatchesSnapshot('@using TheTime = System.Collections.Generic.List<string');
        });

        it('Using alias', async () => {
            await assertMatchesSnapshot('@using TheConsole = System.Console');
        });

        it('Using alias spaced', async () => {
            await assertMatchesSnapshot('@using     TheConsole     =    System.Console   ');
        });

        it('Using alias, optional semicolon', async () => {
            await assertMatchesSnapshot('@using TheConsole = System.Console;');
        });
    });
}
