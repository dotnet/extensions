// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { useLayoutEffect, useRef } from 'react';
import { resize } from 'azure-devops-extension-sdk';

export function useAdoResize(enabled: boolean): void {
    const rafRef = useRef<number | null>(null);

    // StrictMode double-invokes this effect; the first invocation's cleanup
    // cancels its rAF before it fires, so only one resize() call goes out.
    useLayoutEffect(() => {
        if (!enabled) return;

        rafRef.current = requestAnimationFrame(() => {
            rafRef.current = null;
            try {
                const height = document.documentElement.scrollHeight;
                resize(undefined, height);
            } catch { }
        });

        return () => {
            if (rafRef.current !== null) {
                cancelAnimationFrame(rafRef.current);
                rafRef.current = null;
            }
        };
    });
}
