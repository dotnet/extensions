// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import './App.css';
import { AppShell, type HeightStrategy, type ThemeSource } from './AppShell';
import { ViewRouter } from './ViewRouter';

// App is the thin composition root shared by both consumers. It threads the
// host-shape props (heightStrategy/themeSource) down to AppShell and renders the
// shared ViewRouter as the shell content. AppShell owns the FluentProvider (it
// reads darkMode from context), so each main.tsx now mounts ReportContextProvider
// ABOVE App. Defaults match the standalone consumer for zero-config use.
export const App = ({
    heightStrategy = 'fill-viewport',
    themeSource = 'toggle',
}: {
    heightStrategy?: HeightStrategy;
    themeSource?: ThemeSource;
} = {}) => {
    return (
        <AppShell heightStrategy={heightStrategy} themeSource={themeSource}>
            <ViewRouter />
        </AppShell>
    );
};
