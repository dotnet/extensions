// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { useReportContext } from './ReportContext';
import { OverviewView } from './OverviewView';
import { CasesView } from './CasesView';
import { HistoryView } from './HistoryView';
import { ComparisonView } from './ComparisonView';

export const ViewRouter = () => {
    const { view } = useReportContext();

    switch (view) {
        case 'cases':
            return <CasesView />;
        case 'history':
            return <HistoryView />;
        case 'comparison':
            return <ComparisonView />;
        case 'overview':
        default:
            return <OverviewView />;
    }
};
