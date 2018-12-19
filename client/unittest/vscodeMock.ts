/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as vscode from 'Microsoft.AspNetCore.Razor.VSCode/dist/vscodeAdapter';
import * as os from 'os';

export function createLogSink(api: vscode.api) {
    const sink: { [logIdentifier: string]: string[] } = {};
    api.window.createOutputChannel = (name) => {
        if (!sink[name]) {
            sink[name] = [];
        }
        const outputChannel: vscode.OutputChannel = {
            name,
            append: (message) => sink[name].push(message),
            appendLine: (message) => sink[name].push(`${message}${os.EOL}`),
            clear: () => sink[name].length = 0,
            dispose: Function,
            hide: Function,
            show: () => {
                // @ts-ignore
             },
        };

        return outputChannel;
    };

    return sink;
}

export function getFakeVsCodeApi(): vscode.api {
    return {
        commands: {
            executeCommand: <T>(command: string, ...rest: any[]) => {
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
                throw new Error('Not implemented');
            },
        },
        workspace: {
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
            getExtension: () => {
                throw new Error('Not Implemented');
            },
            all: [],
        },
        Uri: {
            parse: () => {
                throw new Error('Not Implemented');
            },
        },
        version: '',
    };
}
