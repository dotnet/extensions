// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { act, cleanup, fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import { createScoreSummary, ReportContextProvider } from '../components';
import { AppShell } from '../components/shell/AppShell';
import { twoExecutionDataset } from './fixtures/richDataset';

class ResizeObserverMock {
    observe() {}
    unobserve() {}
    disconnect() {}
}

type MediaListener = (event: MediaQueryListEvent) => void;

const createMediaQuery = (media: string, initialMatches: boolean) => {
    const listeners = new Set<MediaListener>();
    const query = {
        matches: initialMatches,
        media,
        onchange: null,
        addListener: vi.fn(),
        removeListener: vi.fn(),
        addEventListener: vi.fn((_type: string, listener: MediaListener) => listeners.add(listener)),
        removeEventListener: vi.fn((_type: string, listener: MediaListener) => listeners.delete(listener)),
        dispatchEvent: vi.fn(() => true),
        setMatches(matches: boolean) {
            query.matches = matches;
            listeners.forEach((listener) => listener({ matches, media } as MediaQueryListEvent));
        },
    };
    return query;
};

let desktopQuery: ReturnType<typeof createMediaQuery>;

const renderShell = () => {
    const scoreSummary = createScoreSummary(twoExecutionDataset);
    return render(
        <ReportContextProvider dataset={twoExecutionDataset} scoreSummary={scoreSummary}>
            <AppShell heightStrategy="fill-viewport" themeSource="toggle">
                <div>Report body</div>
            </AppShell>
        </ReportContextProvider>,
    );
};

const scopeTrigger = (scope = 'All scenarios') => screen.getByRole('button', {
    name: `Change report scope. Current scenario: ${scope}.`,
});

const homeButton = () => screen.getByRole('button', {
    name: 'AI Evaluation Report — go to Overview, all scenarios',
});

beforeEach(() => {
    desktopQuery = createMediaQuery('(min-width: 901px)', false);
    vi.stubGlobal('ResizeObserver', ResizeObserverMock);
    vi.stubGlobal('matchMedia', vi.fn((media: string) =>
        media === desktopQuery.media
            ? desktopQuery
            : createMediaQuery(media, media === '(prefers-reduced-motion: reduce)')));
});

afterEach(() => {
    cleanup();
    vi.unstubAllGlobals();
});

describe('mobile report scope drawer', () => {
    it('shows the live scope name and keeps the drawer open while selections change', async () => {
        renderShell();
        const trigger = scopeTrigger();
        expect(trigger).toHaveTextContent('All scenarios');
        expect(homeButton()).not.toHaveAccessibleName(trigger.getAttribute('aria-label')!);

        fireEvent.click(trigger);
        const dialog = await screen.findByRole('dialog', { name: 'Report scope' });
        const drawer = within(dialog);
        expect(dialog).toHaveAccessibleName('Report scope');
        expect(drawer.queryByRole('heading', { name: 'Report scope' })).not.toBeInTheDocument();
        expect(drawer.queryByText('Report scope')).not.toBeInTheDocument();

        const closeButton = await drawer.findByRole('button', { name: 'Close report scope' });
        await waitFor(() => expect(closeButton).toHaveFocus());
        expect(screen.getAllByRole('navigation', { name: 'Scenarios', hidden: true })).toHaveLength(2);
        expect(drawer.getByRole('navigation', { name: 'Scenarios' })).toContainElement(drawer.getByRole('tree'));
        expect(drawer.getByRole('tree')).toHaveAccessibleName('Scenarios');

        fireEvent.click(drawer.getByRole('treeitem', { name: /^TextSummary/ }));
        expect(dialog).toBeInTheDocument();
        expect(scopeTrigger('Comparison.TextSummary')).toHaveTextContent('Comparison.TextSummary');

        fireEvent.click(drawer.getByRole('combobox', { name: 'Execution' }));
        fireEvent.click(await screen.findByRole('option', { name: 'exec-v2' }));

        expect(dialog).toBeInTheDocument();
        expect(scopeTrigger('Comparison.TextSummary')).toHaveTextContent('Comparison.TextSummary');
        expect(drawer.getByRole('combobox', { name: 'Execution' })).toHaveValue('exec-v2');

        const ids = [...document.querySelectorAll<HTMLElement>('[id]')].map((element) => element.id);
        expect(new Set(ids).size).toBe(ids.length);
    });

    it('keeps report state on scope open and preserves the AI button home behavior', async () => {
        renderShell();
        fireEvent.click(scopeTrigger());
        const dialog = await screen.findByRole('dialog', { name: 'Report scope' });
        fireEvent.click(within(dialog).getByRole('treeitem', { name: /^TextSummary/ }));
        fireEvent.click(within(dialog).getByRole('button', { name: 'Close report scope' }));
        await waitFor(() => expect(scopeTrigger('Comparison.TextSummary')).toHaveFocus());

        fireEvent.click(screen.getByRole('tab', { name: 'History' }));
        expect(screen.getByRole('tab', { name: 'History' })).toHaveAttribute('aria-selected', 'true');

        fireEvent.click(scopeTrigger('Comparison.TextSummary'));
        expect(await screen.findByRole('dialog', { name: 'Report scope' })).toBeInTheDocument();
        expect(screen.getByRole('tab', { name: 'History' })).toHaveAttribute('aria-selected', 'true');
        expect(scopeTrigger('Comparison.TextSummary')).toBeInTheDocument();

        fireEvent.click(screen.getByRole('button', { name: 'Close report scope' }));
        await waitFor(() => expect(scopeTrigger('Comparison.TextSummary')).toHaveFocus());
        fireEvent.click(homeButton());
        expect(screen.getByRole('tab', { name: 'Overview' })).toHaveAttribute('aria-selected', 'true');
        expect(scopeTrigger()).toHaveTextContent('All scenarios');
    });

    it.each(['button', 'escape', 'backdrop'] as const)('restores focus to the trigger after %s dismissal', async (close) => {
        renderShell();
        const trigger = scopeTrigger();

        fireEvent.click(trigger);
        const dialog = await screen.findByRole('dialog', { name: 'Report scope' });

        if (close === 'button') {
            fireEvent.click(within(dialog).getByRole('button', { name: 'Close report scope' }));
        } else if (close === 'escape') {
            fireEvent.keyDown(dialog, { key: 'Escape' });
        } else {
            const backdrop = document.querySelector<HTMLElement>('.fui-OverlayDrawer__backdrop');
            expect(backdrop).not.toBeNull();
            fireEvent.click(backdrop!);
        }

        await waitFor(() => expect(trigger).toHaveFocus());
        await waitFor(() => expect(screen.queryByRole('dialog', { name: 'Report scope' })).not.toBeInTheDocument());
    });

    it('closes on the desktop breakpoint, focuses the selected desktop item, and removes its listener', async () => {
        const view = renderShell();
        fireEvent.click(scopeTrigger());
        const dialog = await screen.findByRole('dialog', { name: 'Report scope' });
        fireEvent.click(within(dialog).getByRole('treeitem', { name: /^TextSummary/ }));

        act(() => desktopQuery.setMatches(true));

        await waitFor(() => expect(screen.queryByRole('dialog', { name: 'Report scope' })).not.toBeInTheDocument());
        await waitFor(() => {
            const selected = document.querySelector<HTMLElement>('.eval-sidebar [role="treeitem"][aria-selected="true"]');
            expect(selected).toHaveFocus();
        });

        view.unmount();
        expect(desktopQuery.removeEventListener).toHaveBeenCalled();
    });
});
