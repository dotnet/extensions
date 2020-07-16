/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import { assertMatchesSnapshot } from './infrastructure/TestUtilities';

// See GrammarTests.test.ts for details on exporting this test suite instead of running in place.

export function RunTryStatementSuite() {
    describe('@try { ... } catch/finally { ... }', () => {
        it('Incomplete try statement, no body', async () => {
            await assertMatchesSnapshot('@try');
        });

        it('Incomplete try statement, no catch or finally', async () => {
            await assertMatchesSnapshot('@try {}');
        });

        it('Single line', async () => {
            await assertMatchesSnapshot('@try { var x = 123;<p>Hello World</p> } catch (Exception ex) {@DateTime.Now}finally{<section></section>var y = 456;}');
        });

        it('Multi line catch', async () => {
            await assertMatchesSnapshot(
`@try
{
}
catch (
    InvalidOperationException
    ex){}`);
        });

        it('Multi line complex', async () => {
            await assertMatchesSnapshot(
`@try
{
    Console.WriteLine("Invoking!");
    <div>Invoked: @SomeMethod()</div>
} catch (InvalidOperationExeption ex) when (ex != null)
{
    var x = 123;
    <div>
        @try {
            <p>Error occurred</p>
            throw;
        } catch(Exception ex) {

        } finally { <strong>In the finally</strong> }
    </div>
}finally{}`);
        });
    });
}
