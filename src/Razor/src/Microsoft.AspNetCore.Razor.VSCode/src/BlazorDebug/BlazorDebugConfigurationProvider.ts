/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as vscode from 'vscode';

import { RazorLogger } from '../RazorLogger';
import { HOSTED_APP_NAME, JS_DEBUG_NAME } from './Constants';
import { onDidTerminateDebugSession } from './TerminateDebugHandler';

export class BlazorDebugConfigurationProvider implements vscode.DebugConfigurationProvider {

    constructor(private readonly logger: RazorLogger, private readonly vscodeType: typeof vscode) { }

    public async resolveDebugConfiguration(folder: vscode.WorkspaceFolder | undefined, configuration: vscode.DebugConfiguration): Promise<vscode.DebugConfiguration | undefined> {
        /**
         * The Blazor WebAssembly app should only be launched if the
         * launch configuration is a launch request. Attach requests will
         * only launch the browser.
         */
        if (configuration.request === 'launch') {
            /**
             * If the user is debugging a hosted Blazor WebAssembly application,
             * then the application server needs to be launched via VS Code's debug
             * workflow to allow debugging on the server-side logic. If the app is
             * not hosted, then it can be spun as a standalone process to avoid additional overhead.
             */
            if (configuration.hosted) {
                this.launchHostedApp(folder, configuration);
            } else {
                this.launchStandaloneApp(folder, configuration);
            }
        }

        await this.launchBrowser(folder, configuration);

        /**
         * If `resolveDebugConfiguration` returns undefined, then the debugger
         * launch is canceled. Here, we opt to manually launch the browser
         * configruation using `startDebugging` above instead of returning
         * the configuration to avoid a bug where VS Code is unable to resolve
         * the debug adapter for the browser debugger.
         */
        return undefined;
    }

    private launchHostedApp(folder: vscode.WorkspaceFolder | undefined, configuration: vscode.DebugConfiguration) {
        if (!configuration.program && !configuration.cwd) {
            const message = `Must provide 'program' and 'cwd' properties in launch configuration for hosted Blazor WebAssembly apps.`;
            this.vscodeType.window.showErrorMessage(message);
        }

        const app = {
            name: HOSTED_APP_NAME,
            type: 'coreclr',
            request: 'launch',
            preLaunchTask: 'build',
            program: configuration.program,
            args: [],
            cwd: configuration.cwd,
            env: {
                ...configuration.env,
            },
            logging: configuration.logging,
        };

        this.vscodeType.debug.startDebugging(folder, app).then((appStartFulfilled: boolean) => {
            this.logger.logVerbose('[DEBUGGER] Launching hosted Blazor WebAssembly app...');
            if (process.platform !== 'win32') {
                const terminate = this.vscodeType.debug.onDidTerminateDebugSession(async event => {
                    await onDidTerminateDebugSession(event, this.logger, app.program);
                    terminate.dispose();
                });
            }
        }, (error: Error) => {
            this.logger.logError('[DEBUGGER] Error when launching application: ', error);
        });
    }

    private launchStandaloneApp(folder: vscode.WorkspaceFolder | undefined, configuration: vscode.DebugConfiguration) {
        const shellPath = process.platform === 'win32' ? 'cmd.exe' : 'dotnet';
        const shellArgs = process.platform === 'win32' ? ['/c', 'chcp 65001 >NUL & dotnet run'] : ['run'];
        const spawnOptions = {
            cwd: configuration.cwd || (folder && folder.uri && folder.uri.fsPath),
        };

        const output = this.vscodeType.window.createTerminal({
            name: 'Blazor WebAssembly App',
            shellPath,
            shellArgs,
            ...spawnOptions,
        });

        /**
         * We need to terminate the Blazor dev server.
         */
        const terminate = this.vscodeType.debug.onDidTerminateDebugSession(async event => {
            await onDidTerminateDebugSession(event, this.logger, await output.processId);
            terminate.dispose();
        });

        output.show(/*preserveFocus*/true);
    }

    private async launchBrowser(folder: vscode.WorkspaceFolder | undefined, configuration: vscode.DebugConfiguration) {
        const browser = {
            name: JS_DEBUG_NAME,
            type: configuration.browser === 'edge' ? 'pwa-msedge' : 'pwa-chrome',
            request: 'launch',
            timeout: configuration.timeout || 30000,
            url: configuration.url || 'https://localhost:5001',
            webRoot: configuration.webRoot || '${workspaceFolder}',
            inspectUri: '{wsProtocol}://{url.hostname}:{url.port}/_framework/debug/ws-proxy?browser={browserInspectUri}',
            trace: configuration.trace || false,
            noDebug: configuration.noDebug || false,
        };

        try {
            /**
             * The browser debugger will immediately launch after the
             * application process is started. It waits a `timeout`
             * interval before crashing after being unable to find the launched
             * process.
             *
             * We do this to provide immediate visual feedback to the user
             * that their debugger session has started.
             */
            await this.vscodeType.debug.startDebugging(folder, browser);
            this.logger.logVerbose('[DEBUGGER] Launching browser debugger...');
        } catch (error) {
            this.logger.logError(
                '[DEBUGGER] Error when launching browser debugger: ',
                error,
            );
            const message = `There was an unexpected error while launching your debugging session. Check the console for helpful logs and visit the debugging docs for more info.`;
            this.vscodeType.window.showErrorMessage(message, `View Debug Docs`, `Ignore`).then(async result => {
                if (result === 'View Debug Docs') {
                    const debugDocsUri = 'https://aka.ms/blazorwasmcodedebug';
                    await this.vscodeType.commands.executeCommand(`vcode.open`, debugDocsUri);
                }
            });
        }
    }
}
