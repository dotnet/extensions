/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as psList from 'ps-list';
import { DebugSession } from 'vscode';

import { RazorLogger } from '../RazorLogger';

export async function onDidTerminateDebugSession(
  event: DebugSession,
  logger: RazorLogger,
) {
  logger.logVerbose('Terminating debugging session...');
  if (process.platform !== 'win32') {
    try {
      const processes = await psList();
      const devserver = processes.find(
        (proc: psList.ProcessDescriptor) =>
          !!(proc && proc.cmd && proc.cmd.includes('blazor-devserver')),
      );
      if (devserver) {
        try {
          process.kill(devserver.pid);
          processes.map((proc) => {
            if (process.ppid === devserver.pid) {
              process.kill(proc.pid);
            }
          });
          logger.logVerbose('[DEBUGGER] Debug process clean-up complete.');
        } catch (error) {
          logger.logError('Error terminating debug processes: ', error);
        }
      }
    } catch (error) {
      logger.logError('Error retrieving processes to clean-up: ', error);
    }
  }
}
