/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as cp from 'child_process';
import * as path from 'path';
import * as vscode from 'vscode';

export const repoRoot = path.join(__dirname, '..', '..', '..');
export const basicRazorApp30Root = path.join(repoRoot, 'test', 'testapps', 'BasicRazorApp3_0');
export const basicRazorApp21Root = path.join(repoRoot, 'test', 'testapps', 'BasicRazorApp2_1');
export const basicRazorApp10Root = path.join(repoRoot, 'test', 'testapps', 'BasicRazorApp1_0');

export async function pollUntil(fn: () => boolean, timeoutMs: number) {
    const pollInterval = 50;
    let timeWaited = 0;
    while (!fn()) {
        if (timeWaited >= timeoutMs) {
            throw new Error(`Timed out after ${timeoutMs}ms.`);
        }

        await new Promise(r => setTimeout(r, pollInterval));
        timeWaited += pollInterval;
    }
}

export async function ensureNoChangesFor(documentUri: vscode.Uri, durationMs: number) {
    let changeOccured = false;
    const registration = vscode.workspace.onDidChangeTextDocument(args => {
        if (documentUri === args.document.uri) {
            changeOccured = true;
        }
    });

    await new Promise(r => setTimeout(r, durationMs));

    registration.dispose();

    if (changeOccured) {
        throw new Error('Change occured while ensuring no changes.');
    }
}

// In tests when we edit a document if our test expects to evaluate the output of that document
// after an edit then we'll need to wait for all those edits to flush through the system. Otherwise
// the edits remain in a cached version of the document resulting in our calls to `getText` failing.
export async function waitForDocumentUpdate(
    documentUri: vscode.Uri,
    isUpdated: (document: vscode.TextDocument) => boolean) {
    const updatedDocument = await vscode.workspace.openTextDocument(documentUri);
    let updateError: any;
    let documentUpdated = false;
    const checkUpdated = (document: vscode.TextDocument) => {
        try {
            documentUpdated = isUpdated(document);
        } catch (error) {
            updateError = error;
        }
    };

    checkUpdated(updatedDocument);

    const registration = vscode.workspace.onDidChangeTextDocument(args => {
        if (documentUri === args.document.uri) {
            checkUpdated(args.document);
        }
    });

    try {
        await pollUntil(() => updateError !== undefined || documentUpdated === true, 3000);
    } finally {
        registration.dispose();
    }

    if (updateError) {
        throw updateError;
    }

    return updatedDocument;
}

export async function dotnetRestore(cwd: string): Promise<void> {
    return new Promise<void>((resolve, reject) => {
        const dotnet = cp.spawn('dotnet', ['restore'], { cwd, env: process.env });

        dotnet.stdout.on('data', (data: any) => {
            console.log(data.toString());
        });

        dotnet.stderr.on('err', (error: any) => {
            console.log(`Error: ${error}`);
        });

        dotnet.on('close', (exitCode) => {
            console.log(`Done: ${exitCode}.`);
            resolve();
        });

        dotnet.on('error', error => {
            console.log(`Error: ${error}`);
            reject(error);
        });
    });
}

export async function extensionActivated<T>(identifier: string) {
    const extension = vscode.extensions.getExtension<T>(identifier);

    if (!extension) {
        throw new Error(`Could not find extension '${identifier}'`);
    }

    if (!extension.isActive) {
        await extension.activate();
    }

    return extension;
}

export async function csharpExtensionReady() {
    const csharpExtension = await extensionActivated<CSharpExtensionExports>('ms-vscode.csharp');

    try {
        await csharpExtension.exports.initializationFinished();
        console.log('C# extension activated');
    } catch (error) {
        console.log(JSON.stringify(error));
    }
}

export async function htmlLanguageFeaturesExtensionReady() {
    await extensionActivated<any>('vscode.html-language-features');
}

interface CSharpExtensionExports {
    initializationFinished: () => Promise<void>;
}
