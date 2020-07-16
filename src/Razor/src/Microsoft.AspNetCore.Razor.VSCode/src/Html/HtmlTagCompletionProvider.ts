/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as vscode from 'vscode';
import {
    getLanguageService as getHtmlLanguageService,
    LanguageService as HtmlLanguageService,
} from 'vscode-html-languageservice';
import { TextDocument as ServiceTextDocument } from 'vscode-languageserver-types';
import { IRazorDocumentManager } from '../IRazorDocumentManager';
import { RazorLanguage } from '../RazorLanguage';
import { RazorLanguageServiceClient } from '../RazorLanguageServiceClient';
import { LanguageKind } from '../RPC/LanguageKind';

export class HtmlTagCompletionProvider {
    private timeout: NodeJS.Timer | undefined = void 0;
    private enabled = false;
    private htmlLanguageService: HtmlLanguageService | undefined;

    constructor(private readonly documentManager: IRazorDocumentManager,
                private readonly serviceClient: RazorLanguageServiceClient) {
    }

    public register() {
        this.htmlLanguageService = getHtmlLanguageService();

        const onChangeRegistration = vscode.workspace.onDidChangeTextDocument(
            args => this.onDidChangeTextDocument(args.document, args.contentChanges));

        const onActiveTextEditorChange = vscode.window.onDidChangeActiveTextEditor(() => this.checkIfEnabled());

        this.checkIfEnabled();

        return vscode.Disposable.from(onChangeRegistration, onActiveTextEditorChange);
    }

    private checkIfEnabled() {
        this.enabled = false;

        const editor = vscode.window.activeTextEditor;
        if (!editor) {
            return;
        }

        const document = editor.document;
        if (document.languageId !== RazorLanguage.id) {
            return;
        }

        if (!vscode.workspace.getConfiguration(void 0, document.uri).get<boolean>('html.autoClosingTags')) {
            return;
        }

        this.enabled = true;
    }

    private async onDidChangeTextDocument(
        document: vscode.TextDocument,
        changes: ReadonlyArray<vscode.TextDocumentContentChangeEvent>) {
        if (!this.enabled) {
            return;
        }

        if (changes.length === 0) {
            return;
        }

        if (!vscode.window.activeTextEditor || vscode.window.activeTextEditor.document !== document) {
            // Don't trigger for virtual documents
            return;
        }

        // At this point we're guarunteed to be looking at the correct document.
        if (this.timeout !== undefined) {
            clearTimeout(this.timeout);
        }

        const lastChange = changes[changes.length - 1];
        if (lastChange.rangeLength > 0) {
            // Don't auto-complete self-closing tags when replacing.
            return;
        }

        const lastCharacter = lastChange.text[lastChange.text.length - 1];
        if (lastCharacter !== '>') {
            // We only want to operate on open tags
            return;
        }

        const rangeStart = lastChange.range.start;

        if (rangeStart.character < 2) {
            // Only operate when we're working with a semi-usable tag such as '<O>' where O is some sort of identifier.
            return;
        }

        const changeOffset = document.offsetAt(lastChange.range.start);
        let documentContent = document.getText();
        const potentialSelfClosingCharacter = documentContent.charAt(changeOffset - 1);
        if (potentialSelfClosingCharacter === '/' || potentialSelfClosingCharacter === '>') {
            // Tag was already closed or is incomplete no need to auto-complete.
            return;
        }

        if (this.atMarkupTransition(documentContent, changeOffset)) {
            // We're at a <text> tag, no need to check if we're operating in a non-HTML area
            // (we want to auto-complete <text>).
        } else {
            // Check language kind
            const languageResponse = await this.serviceClient.languageQuery(lastChange.range.start, document.uri);
            if (languageResponse.kind !== LanguageKind.Html) {
                // This prevents auto-completion of things like C# generics
                return;
            }
        }

        const version = document.version;

        // We set a timeout to allow for multi-changes or quick document switches (opening a document then
        // instantly swapping to another) to flow through the system. Basically, if content that would trigger
        // an auto-close occurs we allow a small amount of time for other edits to invalidate the current
        // auto-close task.
        this.timeout = setTimeout(async () => {
            if (!this.enabled) {
                return;
            }

            const activeEditor = vscode.window.activeTextEditor;
            if (!activeEditor) {
                return;
            }
            const activeDocument = activeEditor.document;
            if (document !== activeDocument || activeDocument.version !== version) {
                // User has already moved on or the current document was already edited.
                return;
            }

            const position = new vscode.Position(rangeStart.line, rangeStart.character + lastChange.text.length);
            if (!this.htmlLanguageService) {
                return;
            }

            const razorDoc = await this.documentManager.getActiveDocument();
            if (razorDoc) {
                // The document is guaranteed to be a Razor document
                documentContent = razorDoc.htmlDocument.getContent();
            }

            const serviceTextDocument = ServiceTextDocument.create(
                document.uri.fsPath,
                document.languageId,
                document.version,
                documentContent);
            const htmlDocument = this.htmlLanguageService.parseHTMLDocument(serviceTextDocument);
            const tagCompletion = this.htmlLanguageService.doTagComplete(serviceTextDocument, position, htmlDocument);

            if (!tagCompletion) {
                return;
            }

            const selections = activeEditor.selections;
            if (selections.length && selections.some(s => s.active.isEqual(position))) {
                activeEditor.insertSnippet(new vscode.SnippetString(tagCompletion), selections.map(s => s.active));
            } else {
                activeEditor.insertSnippet(new vscode.SnippetString(tagCompletion), position);
            }
        }, 75);
    }

    private atMarkupTransition(documentContent: string, changeOffset: number): boolean {
        return documentContent.substring(changeOffset - '<text'.length, changeOffset) === '<text';
    }
}
