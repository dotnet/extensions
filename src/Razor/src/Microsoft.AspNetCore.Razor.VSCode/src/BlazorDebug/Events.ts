/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import { ChildProcess } from 'child_process';
import * as psList from 'ps-list';
import { DebugSession } from 'vscode';

import { RazorLogger } from '../RazorLogger';

export async function onDidTerminateDebugSession(
  event: DebugSession,
  logger: RazorLogger,
  devserver: ChildProcess,
) {
  logger.logVerbose('Terminating debugging session...');
  try {
    const processes = await psList();
    if (devserver) {
      try {
        process.kill(devserver.pid);
        processes.map((proc) => {
          if (proc.ppid === devserver.pid) {
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
