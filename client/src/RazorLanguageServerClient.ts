/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import { EventEmitter } from 'events';
import * as vscode from 'vscode';
import {
    GenericRequestHandler,
    LanguageClient,
    LanguageClientOptions,
    ServerOptions,
    State,
} from 'vscode-languageclient/lib/main';
import { RazorLanguageServerOptions } from './RazorLanguageServerOptions';

const events = {
    ServerStart: 'ServerStart',
    ServerStop: 'ServerStop',
};

export class RazorLanguageServerClient implements vscode.Disposable {
    private clientOptions: LanguageClientOptions;
    private serverOptions: ServerOptions;
    private client: LanguageClient;
    private startDisposable: vscode.Disposable | undefined;
    private eventBus: EventEmitter;
    private isStarted: boolean;

    constructor(options: RazorLanguageServerOptions) {
        this.isStarted = false;
        this.clientOptions = {
            outputChannel: options.outputChannel,
        };

        const args = ['-lsp'];

        if (options.debug) {
            args.push('--debug');
        }

        this.serverOptions = {
            run: { command: options.serverDllPath, args },
            debug: { command: options.serverDllPath, args },
        };

        this.client = new LanguageClient(
            'razorLanguageServer', 'Razor Language Server', this.serverOptions, this.clientOptions);

        if (options.trace) {
            this.client.trace = options.trace;
        }

        this.eventBus = new EventEmitter();
    }

    public onStart(listener: () => any) {
        this.eventBus.addListener(events.ServerStart, listener);

        const disposable = new vscode.Disposable(() =>
            this.eventBus.removeListener(events.ServerStart, listener));
        return disposable;
    }

    public onStop(listener: () => any) {
        this.eventBus.addListener(events.ServerStop, listener);

        const disposable = new vscode.Disposable(() =>
            this.eventBus.removeListener(events.ServerStop, listener));
        return disposable;
    }

    public async start() {
        this.startDisposable = await this.client.start();
        await this.client.onReady();

        this.isStarted = true;
        this.eventBus.emit(events.ServerStart);
    }

    public async sendRequest<TResponseType>(method: string, param: any) {
        if (!this.isStarted) {
            throw new Error('Tried to send requests while server is not started.');
        }

        return this.client.sendRequest<TResponseType>(method, param);
    }

    public async onRequest<TRequest, TReturn>(method: string, handler: GenericRequestHandler<TRequest, TReturn>) {
        if (!this.isStarted) {
            throw new Error('Tried to bind on request logic while server is not started.');
        }

        this.client.onRequest(method, handler);
    }

    public dispose() {
        if (this.startDisposable) {
            this.startDisposable.dispose();
        }

        this.isStarted = false;
        this.eventBus.emit(events.ServerStop);
    }
}
