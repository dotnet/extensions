/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

// import * as vscode from 'vscode';

// import { LanguageClient } from 'vscode-languageclient/lib/main';
// import { RazorLanguage } from './RazorLanguage';

// class RazorDocumentTracker implements vscode.Disposable {
//     private _client: LanguageClient;

//     constructor(client: LanguageClient) {
//         this._client = client;

//     }

//     public dispose(): void {
//     }

//     private async trackCurrentDocumentsAsync() {
//         for (let i = 0; i < vscode.workspace.textDocuments.length; i++) {
//             let document = vscode.workspace.textDocuments[i];

//             if (this.isRazorDocument(document)) {

//             }
//         }
//     }

//     private trackFutureDocuments() {
//     }

//     private isRazorDocument(document: vscode.TextDocument): boolean {
//         if (document.languageId === RazorLanguage.id) {
//             return true;
//         }

//         return false;
//     }
// }

// export function trackRazorDocuments(client: LanguageClient): vscode.Disposable {
//     let documentTracker = new RazorDocumentTracker(client);

//     return documentTracker;
// }



        // var projectUris = await vscode.workspace.findFiles("**/*.csproj");

        // for (let i = 0; i < projectUris.length; i++) {
        //     let request = new AddProjectRequest(projectUris[i].fsPath, "MVC-2.0");
        //     await client.sendRequest<AddProjectRequest>("projects/addProject", request);
        // }