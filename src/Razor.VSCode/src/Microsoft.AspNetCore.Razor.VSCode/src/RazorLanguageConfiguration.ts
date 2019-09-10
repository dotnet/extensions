/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as vscode from 'vscode';
import { RazorLanguage } from './RazorLanguage';

const VOID_ELEMENTS: string[] = [
    'area',
    'base',
    'br',
    'col',
    'embed',
    'hr',
    'img',
    'input',
    'keygen',
    'link',
    'menuitem',
    'meta',
    'param',
    'source',
    'track',
    'wbr',
];

export class RazorLanguageConfiguration {
    public register() {
        const configurationRegistration = vscode.languages.setLanguageConfiguration(RazorLanguage.id, {
            wordPattern: /(-?\d*\.\d\w*)|([^\`\~\!\@\$\^\&\*\(\)\-\=\+\[\{\]\}\\\|\;\:\'\"\,\.\<\>\/\s]+)/g,
            onEnterRules: [
                {
                    beforeText: new RegExp(
                        `<(?!(?:${VOID_ELEMENTS.join('|')}))([_:\\w][_:\\w-.\\d]*)([^/>]*(?!/)>)[^<]*$`, 'i'),
                    afterText: /^<\/([_:\w][_:\w-.\d]*)\s*>/i,
                    action: { indentAction: vscode.IndentAction.IndentOutdent },
                },
                {
                    beforeText: new RegExp(
                        `<(?!(?:${VOID_ELEMENTS.join('|')}))(\\w[\\w\\d]*)([^/>]*(?!/)>)[^<]*$`, 'i'),
                    action: { indentAction: vscode.IndentAction.Indent },
                },
            ],
        });

        return configurationRegistration;
    }
}
