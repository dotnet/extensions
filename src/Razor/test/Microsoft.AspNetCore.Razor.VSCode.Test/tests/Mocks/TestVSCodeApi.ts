/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import { RazorLogger } from 'microsoft.aspnetcore.razor.vscode/dist/RazorLogger';
import * as vscode from 'microsoft.aspnetcore.razor.vscode/dist/vscodeAdapter';
import * as os from 'os';
import { TestUri } from './TestUri';

export interface TestVSCodeApi extends vscode.api {
    getOutputChannelSink(): { [logIdentifier: string]: string[] };
    getRazorOutputChannel(): string[];
    setWorkspaceDocuments(...workspaceDocuments: vscode.TextDocument[]): void;
    setExtensions(...extensions: Array<vscode.Extension<any>>): void;
}

export function createTestVSCodeApi(): TestVSCodeApi {
    const workspaceDocuments: vscode.TextDocument[] = [];
    const extensions: Array<vscode.Extension<any>> = [];
    const outputChannelSink: { [logIdentifier: string]: string[] } = {};
    return {
        // Non-VSCode APIs, for tests only

        getOutputChannelSink: () => outputChannelSink,
        getRazorOutputChannel: () => {
            let razorOutputChannel = outputChannelSink[RazorLogger.logName];
            if (!razorOutputChannel) {
                razorOutputChannel = [];
                outputChannelSink[RazorLogger.logName] = razorOutputChannel;
            }

            return razorOutputChannel;
        },
        setWorkspaceDocuments: (...documents) => {
            workspaceDocuments.length = 0;
            workspaceDocuments.push(...documents);
        },
        setExtensions: (...exts: Array<vscode.Extension<any>>) => {
            extensions.length = 0;
            extensions.push(...exts);
        },

        // VSCode APIs

        commands: {
            executeCommand: <T>(command: string, ...rest: any[]) => {
                throw new Error('Not Implemented');
            },
            registerCommand: (command: string, callback: (...args: any[]) => any, thisArg?: any) => {
                throw new Error('Not Implemented');
            },
        },
        languages: {
            match: (selector: vscode.DocumentSelector, document: vscode.TextDocument) => {
                throw new Error('Not Implemented');
            },
        },
        window: {
            activeTextEditor: undefined,
            showInformationMessage: <T extends vscode.MessageItem>(message: string, ...items: T[]) => {
                throw new Error('Not Implemented');
            },
            showWarningMessage: <T extends vscode.MessageItem>(message: string, ...items: T[]) => {
                throw new Error('Not Implemented');
            },
            showErrorMessage: (message: string, ...items: string[]) => {
                throw new Error('Not Implemented');
            },
            createOutputChannel: (name: string) => {
                if (!outputChannelSink[name]) {
                    outputChannelSink[name] = [];
                }
                const outputChannel: vscode.OutputChannel = {
                    name,
                    append: (message) => outputChannelSink[name].push(message),
                    appendLine: (message) => outputChannelSink[name].push(`${message}${os.EOL}`),
                    clear: () => outputChannelSink[name].length = 0,
                    dispose: Function,
                    hide: Function,
                    show: () => {
                        // @ts-ignore
                    },
                };

                return outputChannel;
            },
            registerWebviewPanelSerializer: (viewType: string, serializer: vscode.WebviewPanelSerializer) => {
                throw new Error('Not implemented');
            },
        },
        workspace: {
            openTextDocument: (uri: vscode.Uri) => {
                return new Promise((resolve) => {
                    for (const document of workspaceDocuments) {
                        if (document.uri === uri) {
                            resolve(document);
                        }
                    }
                    resolve(undefined);
                });
            },
            getConfiguration: (section?: string, resource?: vscode.Uri) => {
                throw new Error('Not Implemented');
            },
            asRelativePath: (pathOrUri: string | vscode.Uri, includeWorkspaceFolder?: boolean) => {
                throw new Error('Not Implemented');
            },
            createFileSystemWatcher: (globPattern: vscode.GlobPattern, ignoreCreateEvents?: boolean, ignoreChangeEvents?: boolean, ignoreDeleteEvents?: boolean) => {
                throw new Error('Not Implemented');
            },
            onDidChangeConfiguration: (listener: (e: vscode.ConfigurationChangeEvent) => any, thisArgs?: any, disposables?: vscode.Disposable[]): vscode.Disposable => {
                throw new Error('Not Implemented');
            },
        },
        extensions: {
            getExtension: (id) => {
                for (const extension of extensions) {
                    if (extension.id === id) {
                        return extension;
                    }
                }
            },
            all: extensions,
        },
        Uri: {
            parse: (path) => new TestUri(path),
        },
        Disposable: {
            from: (...disposableLikes: Array<{ dispose: () => any }>) => {
                throw new Error('Not Implemented');
            },
        },
        version: '',
    };
}
