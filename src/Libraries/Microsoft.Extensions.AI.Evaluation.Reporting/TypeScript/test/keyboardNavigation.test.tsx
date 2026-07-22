// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { afterAll, beforeAll, describe, expect, it, vi } from 'vitest';
import { fireEvent, render, screen } from '@testing-library/react';
import { createScoreSummary, ReportContextProvider, useReportContext } from '../components';
import { AppShell } from '../components/shell/AppShell';
import { SidebarTree } from '../components/shell/SidebarTree';
import { twoExecutionDataset } from './fixtures/richDataset';

class ResizeObserverMock {
    observe() {}
    unobserve() {}
    disconnect() {}
}

const renderWithContext = (children: React.ReactNode) => {
    const scoreSummary = createScoreSummary(twoExecutionDataset);
    return render(
        <ReportContextProvider dataset={twoExecutionDataset} scoreSummary={scoreSummary}>
            {children}
        </ReportContextProvider>,
    );
};

const ProgrammaticCaseSwitch = () => {
    const { setView } = useReportContext();
    return <button onClick={() => setView('cases')}>open cases</button>;
};

beforeAll(() => {
    vi.stubGlobal('ResizeObserver', ResizeObserverMock);
    vi.stubGlobal('matchMedia', vi.fn().mockReturnValue({
        matches: false,
        addEventListener: vi.fn(),
        removeEventListener: vi.fn(),
    }));
});

afterAll(() => {
    vi.unstubAllGlobals();
});

describe('AppShell tab focus defaults', () => {
    it('tracks the selected view instead of memorizing a previously focused tab', () => {
        renderWithContext(
            <AppShell heightStrategy="fill-viewport" themeSource="toggle">
                <ProgrammaticCaseSwitch />
            </AppShell>,
        );

        const tablist = screen.getByRole('tablist', { name: 'Report views' });
        const mover = JSON.parse(tablist.getAttribute('data-tabster') ?? '{}').mover;
        const overview = screen.getByRole('tab', { name: 'Overview' });

        expect(mover).toMatchObject({ memorizeCurrent: false, hasDefault: true });
        expect(JSON.parse(overview.getAttribute('data-tabster') ?? '{}')).toEqual({
            focusable: { isDefault: true },
        });

        overview.focus();
        fireEvent.click(screen.getByRole('button', { name: 'open cases' }));

        const cases = screen.getByRole('tab', { name: /Cases/ });
        expect(cases).toHaveAttribute('aria-selected', 'true');
        expect(JSON.parse(cases.getAttribute('data-tabster') ?? '{}')).toEqual({
            focusable: { isDefault: true },
        });
        expect(overview).not.toHaveAttribute('data-tabster');
    });
});

describe('Sidebar tree boundaries', () => {
    it('does not configure vertical arrow navigation to wrap', () => {
        renderWithContext(<SidebarTree labelledBy="scenario-label" />);

        const tree = screen.getByRole('tree');
        const mover = JSON.parse(tree.getAttribute('data-tabster') ?? '{}').mover;

        expect(mover.cyclic).toBe(false);
    });
});
