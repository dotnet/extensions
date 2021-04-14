/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as fs from 'fs';
import * as razorExtensionPackage from 'microsoft.aspnetcore.razor.vscode';
import * as path from 'path';
import * as vscode from 'vscode';
import { ensureWorkspaceIsConfigured, registerRazorDevModeHelpers } from './RazorDevModeHelpers';

let activationResolver: (value?: any) => void;
export const extensionActivated = new Promise(resolve => {
    activationResolver = resolve;
});

export async function activate(context: vscode.ExtensionContext) {
    // Because this extension is only used for local development and tests in CI,
    // we know the Razor Language Server is at a specific path within this repo
    const config = process.env.config ? process.env.config : 'Debug';

    const languageServerDir = path.join(
        __dirname, '..', '..', '..', '..', '..', 'artifacts', 'bin', 'rzls', config, 'net5.0');

    if (!fs.existsSync(languageServerDir)) {
        vscode.window.showErrorMessage(`The Razor Language Server project has not yet been built - could not find ${languageServerDir}`);
        return;
    }

    const hostEventStream = {
        post: (event: any) => {
            // 1 corresponds to the telemetry event type from OmniSharp
            if (event.type === 1) {
                console.log(`Telemetry Event: ${event.eventName}.`);
                if (event.properties) {
                    const propertiesString = JSON.stringify(event.properties, null, 2);
                    console.log(propertiesString);
                }
            } else {
                console.log(`Unknown event: ${event.eventName}`);
            }
        },
    };

    vscode.commands.registerCommand('extension.razorActivated', () => extensionActivated);

    await registerRazorDevModeHelpers(context);
    const workspaceConfigured = ensureWorkspaceIsConfigured();

    if (workspaceConfigured) {
        await razorExtensionPackage.activate(
            vscode,
            context,
            languageServerDir,
            hostEventStream,
            /* enabledProposedApis */true);
    } else {
        console.log('Razor workspace was not configured, extension activation skipped.');
        console.log('To configure your workspace run the following command (ctrl+shift+p) in the experimental instance "Razor: Configure workspace for Razor extension development"');
    }

    activationResolver();
}
