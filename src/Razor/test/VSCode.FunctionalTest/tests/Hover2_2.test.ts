/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as assert from 'assert';
import { beforeEach } from 'mocha';
import * as path from 'path';
import * as vscode from 'vscode';
import { simpleMvc22Root } from './TestUtil';

let cshtmlDoc: vscode.TextDocument;
let editor: vscode.TextEditor;

suite('Hover 2.2', () => {
    beforeEach(async () => {
        const filePath = path.join(simpleMvc22Root, 'Views', 'Home', 'Index.cshtml');
        cshtmlDoc = await vscode.workspace.openTextDocument(filePath);
        editor = await vscode.window.showTextDocument(cshtmlDoc);
    });

    test('Hover over attribute value does not return TagHelper info', async () => {
        const firstLine = new vscode.Position(0, 0);
        await editor.edit(edit => edit.insert(firstLine, '<environment exclude="drain" />\n'));

        const hoverResult = await vscode.commands.executeCommand<vscode.Hover[]>(
            'vscode.executeHoverProvider',
            cshtmlDoc.uri,
            new vscode.Position(0, 24));

        assert.ok(hoverResult, 'Should have returned a result');
        assert.equal(hoverResult!.length, 0, 'Should only have one hover result since the markdown is presented as one.');
    });

    test('Hover over multiple attributes gives the correct one', async () => {
        const firstLine = new vscode.Position(0, 0);
        await editor.edit(edit => edit.insert(firstLine, '<environment exclude="drain" include="fountain" />\n'));

        let hoverResult = await vscode.commands.executeCommand<vscode.Hover[]>(
            'vscode.executeHoverProvider',
            cshtmlDoc.uri,
            new vscode.Position(0, 16));

        assert.ok(hoverResult, 'Should have returned a result');
        assert.equal(hoverResult!.length, 1, 'Should only have one hover result');

        let mdString = hoverResult![0].contents[0] as vscode.MarkdownString;
        assert.ok(mdString.value.includes('**Exclude**'), `Expected "Exclude" in ${mdString.value}`);
        assert.ok(!mdString.value.includes('**Include**'), `Expected 'Include' not to be in ${mdString.value}`);

        hoverResult = await vscode.commands.executeCommand<vscode.Hover[]>(
            'vscode.executeHoverProvider',
            cshtmlDoc.uri,
            new vscode.Position(0, 32));

        assert.ok(hoverResult, 'Should have returned a result');
        assert.equal(hoverResult!.length, 1, 'Should only have one hover result');

        mdString = hoverResult![0].contents[0] as vscode.MarkdownString;
        assert.ok(!mdString.value.includes('**Exclude**'), `Expected "Exclude" not to be in ${mdString.value}`);
        assert.ok(mdString.value.includes('**Include**'), `Expected 'Include' in ${mdString.value}`);
    });

    test('Hovers over tags with multiple possible TagHelpers should return both', async () => {
        const firstLine = new vscode.Position(0, 0);
        await editor.edit(edit => edit.insert(firstLine, '<environment exclude="d" />\n'));
        await editor.edit(edit => edit.insert(firstLine, '@addTagHelper *, SimpleMvc22\n'));
        let hoverResult = await vscode.commands.executeCommand<vscode.Hover[]>(
            'vscode.executeHoverProvider',
            cshtmlDoc.uri,
            new vscode.Position(1, 3));

        assert.ok(hoverResult, 'Should have returned a result');
        assert.equal(hoverResult!.length, 1, 'Should only have one hover result since the markdown is presented as one.');
        let mdString = hoverResult![0].contents[0] as vscode.MarkdownString;
        assert.ok(mdString.value.includes('elements that conditionally renders'));
        assert.ok(mdString.value.includes('I made it!'));

        hoverResult = await vscode.commands.executeCommand<vscode.Hover[]>(
            'vscode.executeHoverProvider',
            cshtmlDoc.uri,
            new vscode.Position(1, 15));

        assert.ok(hoverResult, 'Should have returned a result');
        assert.equal(hoverResult!.length, 1, 'Should have a hover result for both EnvironmentTagHelpers');
        mdString = hoverResult![0].contents[0] as vscode.MarkdownString;
        assert.ok(mdString.value.includes('A comma separated list of environment names in'));
        assert.ok(mdString.value.includes('Exclude it!'));
    });

    test('Can perform hovers on TagHelper Elements and Attribute', async () => {
        const firstLine = new vscode.Position(0, 0);
        await editor.edit(edit => edit.insert(firstLine, '<input class="someName" />\n'));
        let hoverResult = await vscode.commands.executeCommand<vscode.Hover[]>(
            'vscode.executeHoverProvider',
            cshtmlDoc.uri,
            new vscode.Position(0, 3));

        assert.ok(hoverResult, 'Should have returned a result');
        assert.equal(hoverResult!.length, 1, 'Should not have a hover result for InputTagHelper because it does not have the correct attrs yet.');

        await editor.edit(edit => edit.insert(firstLine, '<input asp-for="D" class="someName" />\n'));
        hoverResult = await vscode.commands.executeCommand<vscode.Hover[]>(
            'vscode.executeHoverProvider',
            cshtmlDoc.uri,
            new vscode.Position(0, 3));

        assert.ok(hoverResult, 'Should have a hover result for InputTagHelper.');
        if (!hoverResult) {
            // This can never happen
            return;
        }

        assert.equal(hoverResult.length, 2, 'Something else may be providing hover results');
        const envResult = hoverResult.find((hover, index, obj) => {
            return (hover.contents[0] as vscode.MarkdownString).value.includes('InputTagHelper');
        });

        if (!envResult) {
            assert.fail('Should have found a TagHelper');
        } else {
            let expectedRange = new vscode.Range(
                new vscode.Position(0, 1),
                new vscode.Position(0, 6));
            assert.deepEqual(envResult.range, expectedRange, 'TagHelper range should be <input>');
            let mStr = envResult.contents[0] as vscode.MarkdownString;
            assert.ok(mStr.value.includes('InputTagHelper'), `InputTagHelper not included in '${mStr.value}'`);

            hoverResult = await vscode.commands.executeCommand<vscode.Hover[]>(
                'vscode.executeHoverProvider',
                cshtmlDoc.uri,
                new vscode.Position(0, 8));

            assert.ok(hoverResult, 'Should have a hover result for asp-for');
            if (!hoverResult) {
                // This can never happen
                return;
            }

            assert.equal(hoverResult.length, 1, 'Something else may be providing hover results');

            const aspForResult = hoverResult[0];
            expectedRange = new vscode.Range(
                new vscode.Position(0, 7),
                new vscode.Position(0, 14));
            assert.deepEqual(aspForResult.range, expectedRange, 'asp-for should be selected');
            mStr = aspForResult.contents[0] as vscode.MarkdownString;
            assert.ok(mStr.value.includes('InputTagHelper.**For**'), `InputTagHelper.For not included in '${mStr.value}'`);

            hoverResult = await vscode.commands.executeCommand<vscode.Hover[]>(
                'vscode.executeHoverProvider',
                cshtmlDoc.uri,
                new vscode.Position(0, 19));

            assert.ok(hoverResult, 'Should have a hover result for class');
            if (!hoverResult) {
                // This can never happen
                return;
            }

            assert.equal(hoverResult.length, 1, 'Something else may be providing hover results');

            const result = hoverResult[0];
            expectedRange = new vscode.Range(
                new vscode.Position(0, 19),
                new vscode.Position(0, 24));
            assert.deepEqual(result.range, expectedRange, 'class should be selected');
            mStr = result.contents[0] as vscode.MarkdownString;
            assert.ok(mStr.value.includes('class'), `class not included in ${mStr.value}`);
        }
    });

    // MvcWithComponents doesn't find TagHelpers because of test setup foibles.
    test('Can perform hovers on TagHelpers', async () => {
        const firstLine = new vscode.Position(0, 0);
        await editor.edit(edit => edit.insert(firstLine, '<environment class="someName"></environment>\n'));
        const hoverResult = await vscode.commands.executeCommand<vscode.Hover[]>(
            'vscode.executeHoverProvider',
            cshtmlDoc.uri,
            new vscode.Position(0, 3));
        const expectedRange = new vscode.Range(
            new vscode.Position(0, 1),
            new vscode.Position(0, 12));

        assert.ok(hoverResult, 'Should have a hover result for EnvironmentTagHelper');
        if (!hoverResult) {
            // Not possible, but strict TypeScript doesn't know about assert.ok above.
            return;
        }

        assert.equal(hoverResult.length, 1, 'Something else may be providing hover results');

        const envResult = hoverResult[0];
        assert.deepEqual(envResult.range, expectedRange, 'TagHelper range should be <environment>');
        const mStr = envResult.contents[0] as vscode.MarkdownString;
        assert.ok(mStr.value.includes('**EnvironmentTagHelper**'), `EnvironmentTagHelper not included in '${mStr.value}'`);
    });
});
