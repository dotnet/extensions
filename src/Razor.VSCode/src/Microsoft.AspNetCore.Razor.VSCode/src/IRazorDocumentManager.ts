/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import { IRazorDocument } from './IRazorDocument';
import { IRazorDocumentChangeEvent } from './IRazorDocumentChangeEvent';
import * as vscode from './vscodeAdapter';

export interface IRazorDocumentManager {
    readonly onChange: vscode.Event<IRazorDocumentChangeEvent>;
    readonly documents: IRazorDocument[];
    getDocument(uri: vscode.Uri): Promise<IRazorDocument>;
    getActiveDocument(): Promise<IRazorDocument | null>;
    initialize(): Promise<void>;
    register(): vscode.Disposable;
}
