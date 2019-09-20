/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as fs from 'fs';
import * as path from 'path';
import * as vscode from 'vscode';

export async function registerRazorDevModeHelpers(context: vscode.ExtensionContext) {
    const razorConfiguration = vscode.workspace.getConfiguration('razor');

    const unconfigureSubscription = vscode.commands.registerCommand('extension.resetRazorDevModeConfiguration', async () => {
        await razorConfiguration.update('devmode', undefined);

        const pluginConfiguration = vscode.workspace.getConfiguration('razor.plugin');
        await pluginConfiguration.update('path', undefined);

        // Settings have been updated, lets reload the window.
        await vscode.commands.executeCommand('workbench.action.reloadWindow');
    });
    context.subscriptions.push(unconfigureSubscription);

    const configureSubscription = vscode.commands.registerCommand('extension.configureRazorDevMode', async () => {
        await razorConfiguration.update('devmode', true);

        const pluginPath = path.join(
            __dirname, '..', '..', '..', '..', '..', 'artifacts', 'bin', 'Microsoft.AspNetCore.Razor.OmniSharpPlugin', 'Debug', 'net472', 'Microsoft.AspNetCore.Razor.OmniSharpPlugin.dll');

        if (!fs.existsSync(pluginPath)) {
            vscode.window.showErrorMessage(`The Razor Language Server O# plugin has not yet been built - could not find ${pluginPath}`);
            return;
        }

        const pluginConfiguration = vscode.workspace.getConfiguration('razor.plugin');
        await pluginConfiguration.update('path', pluginPath);

        // Settings have been updated, lets reload the window.
        await vscode.commands.executeCommand('workbench.action.reloadWindow');
    });
    context.subscriptions.push(configureSubscription);

    if (!razorConfiguration.get('devmode')) {
        // Running in a workspace without devmode enabled. We should prompt the user to configure the workspace.
        vscode.window.showWarningMessage(
            'This workspace is not configured to use the local Razor extension.',
            'Configure and Reload', 'Cancel').then(async (reloadResponse) => {
                if (reloadResponse === 'Configure and reload?') {
                    await vscode.commands.executeCommand('extension.configureRazorDevMode');
                }
            });
    }
}
