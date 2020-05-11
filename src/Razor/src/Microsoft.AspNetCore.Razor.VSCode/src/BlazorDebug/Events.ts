/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import { exec } from 'child_process';
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
        (process: psList.ProcessDescriptor) =>
          !!(process && process.cmd && process.cmd.includes('blazor-devserver')),
      );
      if (devserver) {
        const command = `kill ${devserver.pid}`;
        exec(command, (error) => {
          if (error) {
            logger.logError(
              '[DEBUGGER] Error during debug process clean-up: ',
              error,
            );
          }
          return logger.logVerbose(
            '[DEBUGGER] Debug process clean-up complete.',
          );
        });
      }
    } catch (error) {
      logger.logError('Error retrieving processes to clean-up: ', error);
    }
  }
}
