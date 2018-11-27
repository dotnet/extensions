/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import { EventEmitter } from 'events';
import * as vscode from 'vscode';
import { Trace } from 'vscode-jsonrpc';
import {
    GenericRequestHandler,
    LanguageClient,
    LanguageClientOptions,
    ServerOptions,
} from 'vscode-languageclient/lib/main';
import { RazorLanguageServerOptions } from './RazorLanguageServerOptions';
import { RazorLogger } from './RazorLogger';
import { TelemetryReporter } from './TelemetryReporter';

const events = {
    ServerStart: 'ServerStart',
    ServerStop: 'ServerStop',
};

export class RazorLanguageServerClient implements vscode.Disposable {
    private clientOptions: LanguageClientOptions;
    private serverOptions: ServerOptions;
    private client: LanguageClient;
    private startDisposable: vscode.Disposable | undefined;
    private onStartedListeners: Array<() => Promise<any>> = [];
    private eventBus: EventEmitter;
    private isStarted: boolean;
    private startHandle: Promise<void> | undefined;

    constructor(
        options: RazorLanguageServerOptions,
        private readonly telemetryReporter: TelemetryReporter,
        private readonly logger: RazorLogger) {
        this.isStarted = false;
        this.clientOptions = {
            outputChannel: options.outputChannel,
        };

        const args: string[] = [];
        let command = options.serverPath;
        if (options.serverPath.endsWith('.dll')) {
            this.logger.logMessage('Razor Language Server path is an assembly. ' +
                'Using \'dotnet\' from the current path to start the server.');

            command = 'dotnet';
            args.push(options.serverPath);
        }

        args.push('-lsp');
        args.push('--logLevel');
        const logLevelString = this.getLogLevelString(options.trace);
        this.telemetryReporter.reportTraceLevel(options.trace);

        args.push(logLevelString);

        if (options.debug) {
            this.telemetryReporter.reportDebugLanguageServer();

            this.logger.logMessage('Debug flag set for Razor Language Server.');
            args.push('--debug');
        }

        this.serverOptions = {
            run: { command, args },
            debug: { command, args },
        };

        this.client = new LanguageClient(
            'razorLanguageServer', 'Razor Language Server', this.serverOptions, this.clientOptions);

        this.eventBus = new EventEmitter();
    }

    public onStarted(listener: () => Promise<any>) {
        this.onStartedListeners.push(listener);
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
        if (this.startHandle) {
            return this.startHandle;
        }

        let resolve: () => void = Function;
        let reject: () => void = Function;
        this.startHandle = new Promise<void>((resolver, rejecter) => {
            resolve = resolver;
            reject = rejecter;
        });

        try {
            this.logger.logMessage('Starting Razor Language Server...');
            this.startDisposable = await this.client.start();
            this.logger.logMessage('Server started, waiting for client to be ready...');
            await this.client.onReady();

            this.isStarted = true;
            this.logger.logMessage('Server started and ready!');
            this.eventBus.emit(events.ServerStart);

            for (const listener of this.onStartedListeners) {
                await listener();
            }

            resolve();
        } catch (error) {
            vscode.window.showErrorMessage(
                'Razor Language Server failed to start unexpectedly, ' +
                'please check the \'Razor Log\' and report an issue.');

            this.telemetryReporter.reportErrorOnServerStart(error);
            reject();
        }

        return this.startHandle;
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
        this.logger.logMessage('Stopping Razor Language Server.');

        if (this.startDisposable) {
            this.startDisposable.dispose();
        }

        this.isStarted = false;
        this.startHandle = undefined;
        this.eventBus.emit(events.ServerStop);
    }

    private getLogLevelString(trace: Trace) {
        switch (trace) {
            case Trace.Off:
                return 'None';
            case Trace.Messages:
                return 'Information';
            case Trace.Verbose:
                return 'Trace';
        }

        throw new Error(`Unexpected trace value: '${Trace[trace]}'`);
    }
}
