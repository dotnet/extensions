/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as vscode from './vscodeAdapter';

export class RazorLanguage {
    public static id = 'aspnetcorerazor';
    public static fileExtensions = [ 'cshtml', 'razor' ];
    public static globbingPattern = `**/*.{${RazorLanguage.fileExtensions.join(',')}}`;
    public static documentSelector: vscode.DocumentSelector = { pattern: RazorLanguage.globbingPattern };
}
