// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { makeStyles, tokens } from '@fluentui/react-components';
import type { MetricHistorySeries } from './viewModels';

const useStyles = makeStyles({
    root: {
        display: 'flex',
        flexDirection: 'column',
        gap: '0.75rem',
    },
    svgWrapper: {
        width: '100%',
        overflowX: 'auto',
    },
    legend: {
        display: 'flex',
        flexWrap: 'wrap',
        gap: '0.75rem 1.25rem',
        fontSize: tokens.fontSizeBase200,
        color: tokens.colorNeutralForeground2,
    },
    legendItem: {
        display: 'flex',
        alignItems: 'center',
        gap: '0.375rem',
    },
    legendDot: {
        width: '8px',
        height: '8px',
        borderRadius: '50%',
        flex: 'none',
    },
    legendLineSwatch: {
        display: 'flex',
        alignItems: 'center',
        gap: '0.375rem',
    },
});

const SERIES_COLORS = [
    '#2563eb',
    '#16a34a',
    '#dc2626',
    '#d97706',
    '#7c3aed',
    '#0891b2',
    '#db2777',
    '#65a30d',
];

const seriesColor = (idx: number): string => SERIES_COLORS[idx % SERIES_COLORS.length];

const PAD_TOP = 12;
const PAD_BOTTOM = 28;
const PAD_LEFT = 44;
const PAD_RIGHT = 12;
const CHART_HEIGHT = 180;

export type TrendChartProps = {
    series: MetricHistorySeries[];
    ariaLabel: string;
    showLegend?: boolean;
};

export const TrendChart = ({ series, ariaLabel, showLegend = true }: TrendChartProps) => {
    const classes = useStyles();

    if (series.length === 0) return null;

    const execSet = new Set<string>();
    for (const s of series) {
        for (const p of s.points) execSet.add(p.executionName);
    }
    const executions = [...execSet];
    const n = executions.length;

    if (n < 2) return null;

    let globalMin = Infinity;
    let globalMax = -Infinity;
    for (const s of series) {
        for (const p of s.points) {
            if (p.value < globalMin) globalMin = p.value;
            if (p.value > globalMax) globalMax = p.value;
        }
    }
    if (globalMax === globalMin) { globalMin -= 0.5; globalMax += 0.5; }

    const minWidth = 420;
    const perExec = Math.max(64, Math.floor(minWidth / n));
    const innerW = perExec * (n - 1);
    const svgW = innerW + PAD_LEFT + PAD_RIGHT;
    const innerH = CHART_HEIGHT - PAD_TOP - PAD_BOTTOM;

    const xFor = (i: number) => PAD_LEFT + i * perExec;
    const yFor = (v: number) => PAD_TOP + innerH - ((v - globalMin) / (globalMax - globalMin)) * innerH;

    const yTicks = 5;
    const yTickVals: number[] = [];
    for (let i = 0; i <= yTicks; i++) {
        yTickVals.push(globalMin + (i / yTicks) * (globalMax - globalMin));
    }

    const shortExec = (name: string) => name.length > 14 ? '…' + name.slice(-13) : name;

    const seriesPaths = series.map((s, idx) => {
        const color = seriesColor(idx);
        const pts = s.points.map((p) => {
            const xi = executions.indexOf(p.executionName);
            return { x: xFor(xi), y: yFor(p.value), value: p.value };
        }).filter((p) => p.x >= 0);

        if (pts.length < 2) return null;

        const lineD = pts.map((p, i) => `${i === 0 ? 'M' : 'L'}${p.x},${p.y}`).join(' ');
        const areaD =
            lineD +
            ` L${pts[pts.length - 1].x},${PAD_TOP + innerH}` +
            ` L${pts[0].x},${PAD_TOP + innerH} Z`;

        return { color, lineD, areaD, pts, name: s.metricName };
    });

    return (
        <div className={classes.root}>
            <div className={classes.svgWrapper}>
                <svg
                    role="img"
                    aria-label={ariaLabel}
                    width={svgW}
                    height={CHART_HEIGHT}
                    viewBox={`0 0 ${svgW} ${CHART_HEIGHT}`}
                    style={{ display: 'block', minWidth: `${minWidth}px` }}
                >
                    {yTickVals.map((v, i) => {
                        const y = yFor(v);
                        return (
                            <g key={i}>
                                <line
                                    x1={PAD_LEFT} y1={y}
                                    x2={PAD_LEFT + innerW} y2={y}
                                    stroke="currentColor"
                                    strokeOpacity={0.1}
                                    strokeWidth={1}
                                />
                                <text
                                    x={PAD_LEFT - 6} y={y}
                                    textAnchor="end"
                                    dominantBaseline="middle"
                                    fontSize={9}
                                    fill="currentColor"
                                    fillOpacity={0.5}
                                >
                                    {v % 1 === 0 ? v.toFixed(0) : v.toFixed(1)}
                                </text>
                            </g>
                        );
                    })}

                    {seriesPaths.map((sp, idx) =>
                        sp ? (
                            <path
                                key={`area-${idx}`}
                                d={sp.areaD}
                                fill={sp.color}
                                fillOpacity={series.length === 1 ? 0.1 : 0.05}
                            />
                        ) : null,
                    )}

                    {seriesPaths.map((sp, idx) =>
                        sp ? (
                            <path
                                key={`line-${idx}`}
                                d={sp.lineD}
                                fill="none"
                                stroke={sp.color}
                                strokeWidth={2}
                                strokeLinejoin="round"
                                strokeLinecap="round"
                            />
                        ) : null,
                    )}

                    {seriesPaths.map((sp, idx) =>
                        sp
                            ? sp.pts.map((pt, pi) => (
                                  <circle
                                      key={`pt-${idx}-${pi}`}
                                      cx={pt.x}
                                      cy={pt.y}
                                      r={3.5}
                                      fill="white"
                                      stroke={sp.color}
                                      strokeWidth={2}
                                  >
                                      <title>{`${sp.name}: ${pt.value}`}</title>
                                  </circle>
                              ))
                            : null,
                    )}

                    {executions.map((exec, i) => (
                        <text
                            key={exec}
                            x={xFor(i)}
                            y={CHART_HEIGHT - 6}
                            textAnchor="middle"
                            fontSize={9}
                            fill="currentColor"
                            fillOpacity={0.55}
                        >
                            {shortExec(exec)}
                        </text>
                    ))}

                    {executions.map((_exec, i) => (
                        <text
                            key={`rx-${i}`}
                            x={xFor(i)}
                            y={CHART_HEIGHT - 17}
                            textAnchor="middle"
                            fontSize={8}
                            fill="currentColor"
                            fillOpacity={0.35}
                        >
                            {`R${i + 1}`}
                        </text>
                    ))}
                </svg>
            </div>

            {showLegend && series.length > 0 && (
                <div className={classes.legend}>
                    {series.map((s, idx) => (
                        <div key={s.metricName} className={classes.legendItem}>
                            <svg width={28} height={12} aria-hidden="true">
                                <line x1={0} y1={6} x2={28} y2={6} stroke={seriesColor(idx)} strokeWidth={2} />
                                <circle cx={14} cy={6} r={3.5} fill="white" stroke={seriesColor(idx)} strokeWidth={2} />
                            </svg>
                            <span>{s.metricName}</span>
                        </div>
                    ))}
                    {series.length === 1 && (
                        <div className={classes.legendItem}>
                            <svg width={28} height={12} aria-hidden="true">
                                <rect x={0} y={2} width={28} height={8} fill={seriesColor(0)} fillOpacity={0.15} rx={2} />
                            </svg>
                            <span>Min–max spread across cases</span>
                        </div>
                    )}
                </div>
            )}
        </div>
    );
};
