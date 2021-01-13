/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as assert from 'assert';
import * as cp from 'child_process';
import * as fs from 'fs';
import * as path from 'path';
import * as rimraf from 'rimraf';
import * as vscode from 'vscode';

export const razorRoot = path.join(__dirname, '..', '..', '..');
export const testAppsRoot = path.join(razorRoot, 'test', 'testapps');
export const componentRoot = path.join(testAppsRoot, 'ComponentApp');
export const mvcWithComponentsRoot = path.join(testAppsRoot, 'MvcWithComponents');
export const simpleMvc11Root = path.join(testAppsRoot, 'SimpleMvc11');
export const simpleMvc21Root = path.join(testAppsRoot, 'SimpleMvc21');
export const simpleMvc22Root = path.join(testAppsRoot, 'SimpleMvc22');
const projectConfigFile = 'project.razor.json';

export async function pollUntil(fn: () => (boolean | Promise<boolean>), timeoutMs: number, pollInterval?: number, suppressError?: boolean, errorMessage?: string) {
    const resolvedPollInterval = pollInterval ? pollInterval : 50;

    let timeWaited = 0;
    let fnEval;

    do {
        fnEval = fn();
        if (timeWaited >= timeoutMs) {
            if (errorMessage) {
                console.log(errorMessage);
            }
            if (suppressError) {
                return;
            } else {
                let message = `Timed out after ${timeoutMs}ms.`;
                if (errorMessage) {
                    message += `\n{errorMessage}`;
                }
                throw new Error(message);
            }
        }

        await new Promise(r => setTimeout(r, resolvedPollInterval));
        timeWaited += resolvedPollInterval;
    }
    while (!fnEval);
}

export function assertHasNoCompletion(completions: vscode.CompletionList | undefined, name: string) {
    const ok = completions!.items.some(item => item.label === name);
    assert.ok(!ok, `Should not have had completion "${name}"`);
}

export function assertHasCompletion(completions: vscode.CompletionList | undefined, name: string) {
    const ok = completions!.items.some(item => item.label === name);
    assert.ok(ok, `Should have had completion "${name}"`);
}

export async function ensureNoChangesFor(documentUri: vscode.Uri, durationMs: number) {
    let changeOccurred = false;
    const registration = vscode.workspace.onDidChangeTextDocument(args => {
        if (documentUri === args.document.uri) {
            changeOccurred = true;
        }
    });

    await new Promise(r => setTimeout(r, durationMs));

    registration.dispose();

    if (changeOccurred) {
        throw new Error('Change occurred while ensuring no changes.');
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

    // Add a slight delay before checking for the first time.
    await new Promise(r => setTimeout(r, 500));
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

export async function dotnetRestore(directories: string[]): Promise<void> {
    const promiseList = new Array<Promise<void>>();

    for (const cwd of directories) {
        const promise = new Promise<void>((resolve, reject) => {
            const dotnet = cp.spawn('dotnet', ['restore'], { cwd, env: process.env });

            // Terminate dotnet restores that are hanging and optimistically continue test execution.
            let exited = false;
            setTimeout(() => {
                if (!exited) {
                    console.log(`Restore of ${cwd} running for too long. Terminating.`);
                    dotnet.kill();
                }
            }, 180000);

            dotnet.stdout.on('data', (data: any) => {
                console.log(data.toString());
            });

            dotnet.stderr.on('err', (error: any) => {
                console.log(`Error: ${error}`);
                reject(error);
            });

            dotnet.on('close', (exitCode) => {
                exited = true;
                console.log(`Done: ${exitCode}.`);
                resolve();
            });

            dotnet.on('error', error => {
                console.log(`Error: ${error}`);
                reject(error);
            });
        });
        promiseList.push(promise);
    }

    // Sometimes restores take a long time before failing. We don't want to block the whole time.
    // So, let's wait until either all the restores complete or the timeout fires, whichever happens first.
    const restoresPromise = Promise.all(promiseList);
    const timeoutPromise = new Promise(r => setTimeout(r, 60000));
    await Promise.race([restoresPromise, timeoutPromise]);
}

export async function waitForProjectsReady(...directories: string[]) {
    console.log('Getting test projects ready');
    await removeOldProjectRazorJsons();

    if (process.env.norestore !== 'true') {
        console.log('Cleaning Bin and Obj');
        await cleanBinAndObj(directories);
    }

    console.log('Activating C# extension');
    await csharpExtensionReady();

    console.log('Activating HTML language features extension');
    await htmlLanguageFeaturesExtensionReady();

    if (process.env.norestore !== 'true') {
        console.log('Running dotnet restore on test apps');
        await dotnetRestore(directories);
    }

    console.log('Restarting OmniSharp');
    await restartOmniSharp();

    console.log('Activating Razor extension');
    await razorExtensionReady();

    console.log('Ensuring Obj directory in test apps');
    await waitForObjDirectories(directories);

    console.log('Test projects ready');
}

export async function waitForObjDirectories(directories: string[]) {
    for (const directory of directories) {
        if (!fs.existsSync(directory)) {
            throw new Error(`Project does not exist: ${directory}`);
        }

        const objDirectory = path.join(directory, 'obj');
        await pollUntil(() => {
            if (findInDir(objDirectory, projectConfigFile)) {
                return true;
            }

            return false;
        }, /* timeout */ 10000, /* pollInterval */ 250, /* suppressError */ true, `Couldn't find obj directory in ${directory}.`);
    }
}

async function removeOldProjectRazorJsons() {
    const folders = fs.readdirSync(testAppsRoot);
    for (const folder of folders) {
        const objDir = path.join(testAppsRoot, folder, 'obj');
        if (findInDir(objDir, projectConfigFile)) {
            const projFile = findInDir(objDir, projectConfigFile) as string;
            fs.unlinkSync(projFile);
        }
    }
}

export async function restartOmniSharp() {
    try {
        await vscode.commands.executeCommand('o.restart');
        console.log('OmniSharp restarted successfully.');
        await new Promise(r => setTimeout(r, 30000));
    } catch (error) {
        console.log(`OmniSharp restart failed with ${error}.`);
    }
}

export async function cleanBinAndObj(directories: string[]): Promise<void> {
    const promiseList = Array<Promise<void>>();

    for (const directory of directories) {
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

        promiseList.push(cleanBinPromise);
        promiseList.push(cleanObjPromise);
    }

    for (const promise of promiseList) {
        await promise;
    }
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
    const csharpExtension = await extensionActivated<CSharpExtensionExports>('ms-dotnettools.csharp');
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
    try {
        await vscode.commands.executeCommand('extension.razorActivated');
        console.log('Razor activated successfully.');
    } catch (error) {
        console.log(`Razor activation failed with ${error}.`);
    }
}

function findInDir(directoryPath: string, fileQuery: string): string | undefined {
    if (!fs.existsSync(directoryPath)) {
        return;
    }

    const files = fs.readdirSync(directoryPath);
    for (const filename of files) {
        const fullPath = path.join(directoryPath, filename);

        if (fs.lstatSync(fullPath).isDirectory()) {
            const result = findInDir(fullPath, fileQuery);
            if (result) {
                return result;
            }
        } else if (fullPath.indexOf(fileQuery) >= 0) {
            return fullPath;
        }
    }
}

interface CSharpExtensionExports {
    initializationFinished: () => Promise<void>;
}
