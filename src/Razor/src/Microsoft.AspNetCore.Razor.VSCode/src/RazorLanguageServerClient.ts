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
import { RazorLogger } from './RazorLogger';
import { TelemetryReporter } from './TelemetryReporter';
import { Trace } from './Trace';

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

        this.logger.logMessage(`Razor language server path: ${options.serverPath}`);

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
        let reject: (reason: any) => void = Function;
        this.startHandle = new Promise<void>((resolver, rejecter) => {
            resolve = resolver;
            reject = rejecter;
        });

        // Workaround https://github.com/Microsoft/vscode-languageserver-node/issues/472 by tying into state
        // change events to detect when restarts are occuring and then properly reject the Language Server
        // start listeners.
        let restartCount = 0;
        let currentState = State.Starting;
        const didChangeStateDisposable = this.client.onDidChangeState((stateChangeEvent) => {
            currentState = stateChangeEvent.newState;

            if (stateChangeEvent.oldState === State.Starting && stateChangeEvent.newState === State.Stopped) {
                restartCount++;

                if (restartCount === 5) {
                    // Timeout, the built-in LanguageClient retries a hardcoded 5 times before giving up. We tie into that
                    // and then given up on starting the language server if we can't start by then.
                    reject('Server failed to start after retrying 5 times.');
                }
            } else if (stateChangeEvent.newState === State.Running) {
                restartCount = 0;
            }
        });

        try {
            this.logger.logMessage('Starting Razor Language Server...');
            const startDisposable = this.client.start();
            this.startDisposable = vscode.Disposable.from(startDisposable, didChangeStateDisposable);
            this.logger.logMessage('Server started, waiting for client to be ready...');
            this.client.onReady().then(async () => {
                if (currentState !== State.Running) {
                    // Unexpected scenario, if we fall into this scenario the above onDidChangeState
                    // handling will kill the start promise if we reach a certain retry threshold.
                    return;
                }
                this.isStarted = true;
                this.logger.logMessage('Server started and ready!');
                this.eventBus.emit(events.ServerStart);

                for (const listener of this.onStartedListeners) {
                    await listener();
                }

                // We don't want to track restart management after the server has been initially started,
                // the langauge client will handle that.
                didChangeStateDisposable.dispose();

                // Succesfully started, notify listeners.
                resolve();
            });
        } catch (error) {
            vscode.window.showErrorMessage(
                'Razor Language Server failed to start unexpectedly, ' +
                'please check the \'Razor Log\' and report an issue.');

            this.telemetryReporter.reportErrorOnServerStart(error);
            reject(error);
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
