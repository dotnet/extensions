/* --------------------------------------------------------------------------------------------
* Copyright (c) Microsoft Corporation. All rights reserved.
* Licensed under the MIT License. See License.txt in the project root for license information.
* ------------------------------------------------------------------------------------------ */

import * as vscode from 'microsoft.aspnetcore.razor.vscode/dist/vscodeAdapter';

export class TestEventEmitter<T> implements vscode.EventEmitter<T> {

    /**
     * The event listeners can subscribe to.
     */
    public readonly event: vscode.Event<T>;

    private readonly listeners: Array<(e: T) => any> = [];

    constructor() {
        this.event = (listener: (e: T) => any, thisArgs?: any, disposables?: vscode.Disposable[]) => {
            this.listeners.push(listener);
            return {
                dispose: Function,
            };
        };
    }

    /**
     * Notify all subscribers of the [event](EventEmitter#event). Failure
     * of one or more listener will not fail this function call.
     *
     * @param data The event object.
     */
    public fire(data?: T) {
        for (const listener of this.listeners) {
            if (data) {
                listener(data);
            } else {
                throw new Error('Event emitters do not implement firing events without data.');
            }
        }
    }

    /**
     * Dispose this object and free resources.
     */
    public dispose() {
        // @ts-ignore
    }
}
