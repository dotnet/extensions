/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as net from 'net';

export function getAvailablePort(initialPort: number) {
    function getNextAvailablePort(currentPort: number, cb: (port: number) => void) {
        const server = net.createServer();
        server.listen(currentPort, () => {
            server.once('close', () => {
                cb(currentPort);
            });
            server.close();
        });
        server.on('error', () => {
            if (currentPort <= 65535 /* total number of ports available */) {
                getNextAvailablePort(++currentPort, cb);
            }
        });
    }

    return new Promise<number>(resolve => {
        getNextAvailablePort(initialPort, resolve);
    });
}
