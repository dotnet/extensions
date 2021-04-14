/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

// Bits in this file are contracts defined in https://github.com/omnisharp/omnisharp-vscode

export interface HostEventStream {
    post(event: BaseEvent): void;
}

export function createTelemetryEvent(
    eventName: string,
    properties?: { [key: string]: string },
    measures?: { [key: string]: number }): TelemetryEvent {

    return {
        type: EventType.TelemetryEvent,
        eventName,
        properties,
        measures,
    };
}

export function createTelemetryErrorEvent(
    eventName: string,
    properties?: { [key: string]: string },
    measures?: { [key: string]: number },
    errorProps?: string[]): TelemetryErrorEvent {

    return {
        type: EventType.TelemetryErrorEvent,
        eventName,
        properties,
        measures,
        errorProps,
    };
}

interface TelemetryEvent extends BaseEvent {
    type: EventType.TelemetryEvent;

    eventName: string;
    properties?: { [key: string]: string };
    measures?: { [key: string]: number };
}

interface TelemetryErrorEvent extends BaseEvent {
    type: EventType.TelemetryErrorEvent;

    eventName: string;
    properties?: { [key: string]: string };
    measures?: { [key: string]: number };
    errorProps?: string[];
}

interface BaseEvent {
    type: any;
}

// This is a sub-copied portion of OmniSharp's EventType class.
enum EventType {
    TelemetryEvent = 1,
    TelemetryErrorEvent = 78,
}
