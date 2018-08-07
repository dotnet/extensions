/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as vscode from 'vscode';
import { LanguageClient, LanguageClientOptions, ServerOptions } from 'vscode-languageclient/lib/main';
import { RazorLanguage } from './RazorLanguage';
import { RazorLanguageServerOptions } from './RazorLanguageServerOptions';
import { EventEmitter } from 'events';

export class RazorLanguageServerClient implements vscode.Disposable {
    private static Events = 
    {
        ServerStart: "ServerStart",
        ServerStop: "ServerStop"
    };

    private _clientOptions: LanguageClientOptions;
    private _serverOptions: ServerOptions;
    private _client: LanguageClient;
    private _startDisposable: vscode.Disposable | undefined;
    private _eventBus: EventEmitter;
    private _isStarted: boolean;

    constructor(options: RazorLanguageServerOptions) {
        this._isStarted = false;
        this._clientOptions = {
            documentSelector: <any>RazorLanguage.documentSelector, // No idea why I need to cast here.
            outputChannel: options.outputChannel
        };

        const args = ['-lsp'];
        let args = ['-lsp'];

        if (options.debug) {
            args[2] = "--debug";
        }

        this._serverOptions = {
            run: { command: options.serverDllPath, args },
            debug: { command: options.serverDllPath, args },
        };

        this._client = new LanguageClient('razorLanguageServer', 'Razor Language Server', this._serverOptions, this._clientOptions);
        if (options.trace) {
            this._client.trace = options.trace;
        }
        
        this._eventBus = new EventEmitter();
    }

    public get isStarted(): boolean {
        return this._isStarted;
    }

    public onStart(listener: () => any): vscode.Disposable {
        this._eventBus.addListener(RazorLanguageServerClient.Events.ServerStart, listener);

        let disposable = new vscode.Disposable(() => this._eventBus.removeListener(RazorLanguageServerClient.Events.ServerStart, listener));
        return disposable;
    }

    public onStop(listener: () => any): vscode.Disposable {
        this._eventBus.addListener(RazorLanguageServerClient.Events.ServerStop, listener);

        let disposable = new vscode.Disposable(() => this._eventBus.removeListener(RazorLanguageServerClient.Events.ServerStop, listener));
        return disposable;
    }

    public async start(): Promise<void> {
        this._startDisposable = await this._client.start();
        await this._client.onReady();

        this._isStarted = true;
        this._eventBus.emit(RazorLanguageServerClient.Events.ServerStart);
    }

    public async sendRequest<TResponseType>(method: string, param: any): Promise<TResponseType> {
        if (!this._isStarted) {
            throw new Error("Tried to send requests while server is not started.");
        }

        return this._client.sendRequest<TResponseType>(method, param);
    }

    public dispose(): void {
        if (this._startDisposable) {
            this._startDisposable.dispose();
        }

        this._isStarted = false;
        this._eventBus.emit(RazorLanguageServerClient.Events.ServerStop);
    }
}