// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Public API surface for the AI Evaluation Report component library.
// Pure re-export barrel. Internal files MUST NOT import from here.

// shell
export { App } from './shell/App';
export type { HeightStrategy, ThemeSource } from './shell/AppShell';
export { detectHostDarkMode } from './shell/theme';

// core
export { ReportContextProvider, useReportContext } from './core/ReportContext';
export type { ReportView, ScenarioSort, ReportContextType } from './core/ReportContext';
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
export * from './core/viewModels';

// feature views (consumed by tests today; keep public)
export { CasesView } from './cases/CasesView';
export { HistoryView } from './history/HistoryView';
export { ComparisonView } from './history/ComparisonView';
export { TranscriptBlock } from './cases/TranscriptBlock';
