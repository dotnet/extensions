/* --------------------------------------------------------------------------------------------
* Copyright (c) Microsoft Corporation. All rights reserved.
* Licensed under the MIT License. See License.txt in the project root for license information.
* ------------------------------------------------------------------------------------------ */

import { EventEmitter } from './vscodeAdapter';

export interface IEventEmitterFactory {
    create: <T>() => EventEmitter<T>;
}
