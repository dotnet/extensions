// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Shared dumbbell geometry constants used by both ComparisonView and
// HistoryView. Dot diameter, hollow-baseline ring width, and connector
// thickness live here in ONE place so the two dumbbell builders (string-CSS in
// ComparisonView, CSSProperties in HistoryView) render identical geometry.
// Dots use box-sizing:border-box so the ring is drawn inside DUMBBELL_D (an 8px
// dot stays 8px, not 8 + 2*ring).
export const DUMBBELL_D = 8;
export const DUMBBELL_RING = 1.5;
export const DUMBBELL_CONN = 1.5;
