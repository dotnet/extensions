/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

// Bits in this file are contracts defined in https://github.com/omnisharp/omnisharp-vscode

export interface HostEventStream {
    post(event: BaseEvent): void;
}

export class TelemetryEvent implements BaseEvent {
    constructor(
        public eventName: string,
        public properties?: { [key: string]: string },
        public measures?: { [key: string]: number }) {
    }
}

// tslint:disable-next-line:no-empty-interface
interface BaseEvent {
}
