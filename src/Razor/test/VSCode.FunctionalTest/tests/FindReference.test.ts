/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as assert from 'assert';
import { beforeEach } from 'mocha';
import * as path from 'path';
import * as vscode from 'vscode';
import { mvcWithComponentsRoot } from './TestUtil';

let cshtmlDoc: vscode.TextDocument;
let editor: vscode.TextEditor;
let cshtmlPath: string;

suite('References', () => {
    beforeEach(async () => {
        cshtmlPath = path.join(mvcWithComponentsRoot, 'Views', 'Home', 'Index.cshtml');
        cshtmlDoc = await vscode.workspace.openTextDocument(cshtmlPath);
        editor = await vscode.window.showTextDocument(cshtmlDoc);
    });

    test('Reference for javascript', async () => {
        const firstLine = new vscode.Position(0, 0);
        const razorPath = path.join(mvcWithComponentsRoot, 'Components', 'Counter.razor');
        const razorDoc = await vscode.workspace.openTextDocument(razorPath);
        const razorEditor = await vscode.window.showTextDocument(razorDoc);
        await razorEditor.edit(edit => edit.insert(firstLine, `<script>
    var abc = 1;
    abc.toString();
</script>
`));
        const references = await vscode.commands.executeCommand<vscode.Location[]>(
            'vscode.executeReferenceProvider',
            razorDoc.uri,
            new vscode.Position(1, 10));

        assert.equal(references!.length, 2, 'Should have had exactly two results');
        const definition = references![1];
        assert.ok(definition.uri.path.endsWith('Counter.razor'), `Expected 'Counter.razor', but got ${definition.uri.path}`);
        assert.equal(definition.range.start.line, 2);
    });

    test('Reference outside file works', async () => {
        const programLine = new vscode.Position(7, 0);
        const programPath = path.join(mvcWithComponentsRoot, 'Program.cs');
        const programDoc = await vscode.workspace.openTextDocument(programPath);
        const programEditor = await vscode.window.showTextDocument(programDoc);
        await programEditor.edit(edit => edit.insert(programLine, `var x = typeof(Program);`));

        const firstLine = new vscode.Position(0, 0);
        cshtmlDoc = await vscode.workspace.openTextDocument(cshtmlPath);
        editor = await vscode.window.showTextDocument(cshtmlDoc);
        await editor.edit(edit => edit.insert(firstLine, '@{\nvar x = typeof(Program);\n}\n'));

        const references = await vscode.commands.executeCommand<vscode.Location[]>(
            'vscode.executeReferenceProvider',
            cshtmlDoc.uri,
            new vscode.Position(1, 17));

        assert.equal(references!.length, 2 , 'Should have had exactly 2 results');
        references!.sort((a, b) => a.uri.path > b.uri.path ? 1 : -1);
        const programRef = references![0];
        assert.ok(programRef.uri.path.endsWith('Program.cs'), `Expected ref to point to "Program.cs" but got ${references![0].uri.path}`);
        assert.equal(programRef.range.start.line, 7);

        const cshtmlRef = references![1];
        assert.ok(cshtmlRef.uri.path.endsWith('Index.cshtml'), `Expected ref to point to "Index.cshtml" but got ${references![1].uri.path}`);
        assert.equal(cshtmlRef.range.start.line, 1);

        await vscode.commands.executeCommand('workbench.action.revertAndCloseActiveEditor');
    });

    test('Reference inside file works', async () => {
        const firstLine = new vscode.Position(0, 0);
        await editor.edit(edit => edit.insert(firstLine, '@{\nTester();\n}\n'));
        await editor.edit(edit => edit.insert(firstLine, '@functions{\nvoid Tester()\n{\n}}\n'));
        const references = await vscode.commands.executeCommand<vscode.Location[]>(
            'vscode.executeReferenceProvider',
            cshtmlDoc.uri,
            new vscode.Position(1, 6));

        assert.equal(references!.length, 1, 'Should have had exactly one result');
        const reference = references![0];
        assert.ok(reference.uri.path.endsWith(''), `Expected ref to point to "${cshtmlDoc.uri}", but it pointed to ${reference.uri.path}`);
        assert.equal(reference.range.start.line, 5);
    });
});
