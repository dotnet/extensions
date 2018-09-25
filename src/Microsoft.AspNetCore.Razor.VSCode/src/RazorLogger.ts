/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as vscode from 'vscode';
import { Trace } from 'vscode-jsonrpc';

export class RazorLogger implements vscode.Disposable {
    public readonly verboseEnabled: boolean;
    public readonly messageEnabled: boolean;
    public readonly outputChannel: vscode.OutputChannel;

    private readonly trace: Trace;

    constructor(trace: Trace) {
        this.trace = trace;
        this.verboseEnabled = this.trace >= Trace.Verbose;
        this.messageEnabled = this.trace >= Trace.Messages;

        this.outputChannel = vscode.window.createOutputChannel('Razor Log');

        this.logRazorInformation();
    }

    public logMessage(message: string) {
        if (this.messageEnabled) {
            this.logWithmarker(message);
        }
    }

    public logVerbose(message: string) {
        if (this.verboseEnabled) {
            this.logWithmarker(message);
        }
    }

    public dispose() {
        this.outputChannel.dispose();
    }

    private logWithmarker(message: string) {
        const date = new Date();
        const markedMessage = `[Client - ${date.getHours()}:${date.getMinutes()}:${date.getSeconds()}] ${message}`;

        this.log(markedMessage);
    }

    private log(message: string) {
        this.outputChannel.appendLine(message);
    }

    private logRazorInformation() {
        this.log(
            '-----------------------------------------------------------------------' +
            '------------------------------------------------------');
        this.log(`Razor's trace level is currently set to '${Trace[this.trace]}'`);
        this.log(
            ' - To log issues with the Razor experience in VSCode you can file issues ' +
            'at https://github.com/aspnet/Razor.VSCode');
        this.log(
            ' - To change Razor\'s trace level set \'razor.languageServer.trace\' to ' +
            '\'Off\', \'Messages\' or \'Verbose\' and then restart VSCode.');

        this.log(
            '-----------------------------------------------------------------------' +
            '------------------------------------------------------');
        this.log('');
    }
}
