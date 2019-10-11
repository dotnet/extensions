/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as cp from 'child_process';
import * as fs from 'fs';
import * as path from 'path';
import * as rimraf from 'rimraf';
import * as vscode from 'vscode';

export const razorRoot = path.join(__dirname, '..', '..', '..');
export const testAppsRoot = path.join(razorRoot, 'test', 'testapps');
export const mvcWithComponentsRoot = path.join(testAppsRoot, 'MvcWithComponents');
export const simpleMvc21Root = path.join(testAppsRoot, 'SimpleMvc21');
export const simpleMvc11Root = path.join(testAppsRoot, 'SimpleMvc11');

export async function pollUntil(fn: () => boolean, timeoutMs: number, pollInterval?: number) {
    const resolvedPollInterval = pollInterval ? pollInterval : 50;

    let timeWaited = 0;
    while (!fn()) {
        if (timeWaited >= timeoutMs) {
            throw new Error(`Timed out after ${timeoutMs}ms.`);
        }

        await new Promise(r => setTimeout(r, resolvedPollInterval));
        timeWaited += resolvedPollInterval;
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

export async function waitForProjectReady(directory: string) {
    await cleanBinAndObj(directory);
    await csharpExtensionReady();
    await htmlLanguageFeaturesExtensionReady();
    await dotnetRestore(directory);
    await razorExtensionReady();
    await waitForProjectConfigured(directory);
}

export async function waitForProjectConfigured(directory: string) {
    const projectConfigFile = 'project.razor.json';

    if (!fs.existsSync(directory)) {
        throw new Error(`Project does not exist: ${directory}`);
    }

    const objDirectory = path.join(directory, 'obj');
    await pollUntil(() => {
        if (findInDir(objDirectory, projectConfigFile)) {
            return true;
        }

        return false;
    }, /* timeout */ 60000, /* pollInterval */ 250);
}

export async function cleanBinAndObj(directory: string): Promise<void> {
    const binDirectory = path.join(directory, 'bin');
    const cleanBinPromise = new Promise<void>((resolve, reject) => {
        if (!fs.existsSync(binDirectory)) {
            // Already clean;
            resolve();
            return;
        }

        rimraf(binDirectory, (error) => {
            if (error) {
                reject(error);
            } else {
                resolve();
            }
        });
    });

    const objDirectory = path.join(directory, 'obj');
    const cleanObjPromise = new Promise<void>((resolve, reject) => {
        if (!fs.existsSync(objDirectory)) {
            // Already clean;
            resolve();
            return;
        }

        rimraf(objDirectory, (error) => {
            if (error) {
                reject(error);
            } else {
                resolve();
            }
        });
    });

    await cleanBinPromise;
    await cleanObjPromise;
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

async function razorExtensionReady() {
    await vscode.commands.executeCommand('extension.razorActivated');
}

function findInDir(directoryPath: string, fileQuery: string): string | undefined {
    if (!fs.existsSync(directoryPath)) {
        return;
    }

    const files = fs.readdirSync(directoryPath);
    for (const filename of files) {
        const fullpath = path.join(directoryPath, filename);

        if (fs.lstatSync(fullpath).isDirectory()) {
            const result = findInDir(fullpath, fileQuery);
            if (result) {
                return result;
            }
        } else if (fullpath.indexOf(fileQuery) >= 0) {
            return fullpath;
        }
    }
}

interface CSharpExtensionExports {
    initializationFinished: () => Promise<void>;
}
