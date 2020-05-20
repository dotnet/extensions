/* --------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License. See License.txt in the project root for license information.
 * ------------------------------------------------------------------------------------------ */

import * as psList from 'ps-list';
import { DebugSession } from 'vscode';

import { RazorLogger } from '../RazorLogger';

import { HOSTED_APP_NAME, JS_DEBUG_NAME } from './Constants';

export async function onDidTerminateDebugSession(
  event: DebugSession,
  logger: RazorLogger,
  targetPid: number | undefined,
  targetProcess: string | boolean = false,
) {
  // Ignore debug sessions that are not applicable to us
  const VALID_EVENT_NAMES = [HOSTED_APP_NAME, JS_DEBUG_NAME];
  if (!VALID_EVENT_NAMES.includes(event.name)) {
    return;
  }

  // If there is no targetPid passed and the user has not requested
  // one be resolved, then exit early
  if (!targetPid && !targetProcess) {
    return;
  }

  let processes: psList.ProcessDescriptor[] = [];
  try {
    processes = await psList();
  } catch (error) {
    logger.logError(`Error retrieving processes to clean-up: `, error);
  }

  // If the user has requested resolving the PID for the devserver process,
  // we do that here
  if (typeof targetProcess === 'string') {
    const devserver = processes.find(
      (process: psList.ProcessDescriptor) => !!(process && process.cmd && process.cmd.includes(targetProcess)));
    targetPid = devserver ? devserver.pid : undefined;
  }

  // If no PID was resolved for the dev server, then exit early.
  if (!targetPid) {
    return;
  }

  logger.logVerbose(`[DEBUGGER] Terminating debugging session with PID ${targetPid}...`);

  try {
    process.kill(targetPid);
    processes.map((proc) => {
      if (proc.ppid === targetPid) {
        logger.logVerbose(`[DEBUGGER] Terminating child process with PID ${proc.pid}...`);
        process.kill(proc.pid);
      }
    });
    logger.logVerbose(`[DEBUGGER] Debug process clean-up of PID ${targetPid} complete.`);
  } catch (error) {
    logger.logError(`[DEBUGGER] Error terminating debug processes with PID ${targetPid}: `, error);
  }
}
