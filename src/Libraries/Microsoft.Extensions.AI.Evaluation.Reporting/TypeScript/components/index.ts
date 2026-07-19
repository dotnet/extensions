// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

export { App } from './shell/App';
export type { HeightStrategy, ThemeSource } from './shell/AppShell';
export { detectHostDarkMode } from './shell/theme';

export { ReportContextProvider, useReportContext } from './core/ReportContext';
export type { ReportView, ReportContextType } from './core/ReportContext';
export {
    ScoreNode,
    ScoreNodeType,
    ReverseTextIndex,
    createScoreSummary,
    getScoreHistory,
    getConversationDisplay,
    isTextContent,
    isImageContent,
} from './core/Summary';
export type { ScoreSummary, ConversationDisplay, ChatMessageDisplay } from './core/Summary';
export {
    bucketMetrics,
    passRateByScenarioGroup,
    scenariosForExecution,
    kpiCountsFromNode,
} from './core/viewModels';

export { CasesView } from './cases/CasesView';
export { TranscriptBlock } from './cases/TranscriptBlock';
export { HistoryView } from './history/HistoryView';
export { ComparisonView } from './history/ComparisonView';
