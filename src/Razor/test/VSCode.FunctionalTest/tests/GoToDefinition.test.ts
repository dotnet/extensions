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

 suite('Definition', () => {
    beforeEach(async () => {
        cshtmlPath = path.join(mvcWithComponentsRoot, 'Views', 'Home', 'Index.cshtml');
        cshtmlDoc = await vscode.workspace.openTextDocument(cshtmlPath);
        editor = await vscode.window.showTextDocument(cshtmlDoc);
    });

    test('Definition of injection gives nothing', async () => {
        const firstLine = new vscode.Position(0, 0);
        await editor.edit(edit => edit.insert(firstLine, '@inject DateTime SecondTime\n'));
        await editor.edit(edit => edit.insert(firstLine, '@SecondTime\n'));
        const definitions = await vscode.commands.executeCommand<vscode.Location[]>(
            'vscode.executeDefinitionProvider',
            cshtmlDoc.uri,
            new vscode.Position(0, 18));

        assert.equal(definitions!.length, 0, 'Should have had no results');
    });

    test('Definition inside file works', async () => {
        const firstLine = new vscode.Position(0, 0);
        await editor.edit(edit => edit.insert(firstLine, '@functions{\n void Action()\n{\n}\n}\n'));
        await editor.edit(edit => edit.insert(firstLine, '@{\nAction();\n}\n'));
        const definitions = await vscode.commands.executeCommand<vscode.Location[]>(
            'vscode.executeDefinitionProvider',
            cshtmlDoc.uri,
            new vscode.Position(1, 2));

        assert.equal(definitions!.length, 1, 'Should have had exactly one result');
        const definition = definitions![0];
        assert.ok(definition.uri.path.endsWith('Index.cshtml'));
        assert.equal(definition.range.start.line, 4);
    });

    test('Definition outside file works', async () => {
        const firstLine = new vscode.Position(0, 0);
        await editor.edit(edit => edit.insert(firstLine, '@{\nvar x = typeof(Program);\n}\n'));

        const definitions = await vscode.commands.executeCommand<vscode.Location[]>(
            'vscode.executeDefinitionProvider',
            cshtmlDoc.uri,
            new vscode.Position(1, 17));

        assert.equal(definitions!.length, 1, 'Should have had exactly one result');
        const definition = definitions![0];
        assert.ok(definition.uri.path.endsWith('Program.cs'), `Expected def to point to "Program.cs", but it pointed to ${definition.uri.path}`);
        assert.equal(definition.range.start.line, 3);
    });

    test('Definition of javascript works in cshtml', async () => {
        const firstLine = new vscode.Position(0, 0);
        await editor.edit(edit => edit.insert(firstLine, `<script>
    var abc = 1;
    abc.toString();
</script>
`));
        const definitions = await vscode.commands.executeCommand<vscode.Location[]>(
            'vscode.executeDefinitionProvider',
            cshtmlDoc.uri,
            new vscode.Position(2, 5));

        assert.equal(definitions!.length, 1, 'Should have had exactly one result');
        const definition = definitions![0];
        assert.ok(definition.uri.path.endsWith('Index.cshtml'), `Expected 'Index.cshtml', but got ${definition.uri.path}`);
        assert.equal(definition.range.start.line, 1);
    });

    test('Definition of javascript works in razor', async () => {
        const firstLine = new vscode.Position(0, 0);
        const razorPath = path.join(mvcWithComponentsRoot, 'Components', 'Counter.razor');
        const razorDoc = await vscode.workspace.openTextDocument(razorPath);
        const razorEditor = await vscode.window.showTextDocument(razorDoc);
        await razorEditor.edit(edit => edit.insert(firstLine, `<script>
    var abc = 1;
    abc.toString();
</script>
`));
        const definitions = await vscode.commands.executeCommand<vscode.Location[]>(
            'vscode.executeDefinitionProvider',
            razorDoc.uri,
            new vscode.Position(2, 5));

        assert.equal(definitions!.length, 1, 'Should have had exactly one result');
        const definition = definitions![0];
        assert.ok(definition.uri.path.endsWith('Counter.razor'), `Expected 'Counter.razor', but got ${definition.uri.path}`);
        assert.equal(definition.range.start.line, 1);
    });
 });
