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
  targetPid: number | undefined,
) {
  if (event.type !== 'pwa-chrome' && event.type !== 'pwa-msedge') {
    return;
  }
  if (!targetPid) {
    return;
  }

  logger.logVerbose(`[DEBUGGER] Terminating debugging session with PID ${targetPid}...`);

  let processes: psList.ProcessDescriptor[] = [];
  try {
    processes = await psList();
  } catch (error) {
    logger.logError(`Error retrieving processes under PID ${targetPid} to clean-up: `, error);
  }

  try {
    process.kill(targetPid);
    processes.map((proc) => {
      if (proc.ppid === targetPid) {
        process.kill(proc.pid);
      }
    });
    logger.logVerbose(`[DEBUGGER] Debug process clean-up of PID ${targetPid} complete.`);
  } catch (error) {
    logger.logError(`[DEBUGGER] Error terminating debug processes with PID ${targetPid}: `, error);
  }
}
