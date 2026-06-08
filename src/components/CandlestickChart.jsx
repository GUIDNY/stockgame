import { useEffect, useRef } from 'react';
import { createChart } from 'lightweight-charts';

export default function CandlestickChart({
  candles,
  revealCandles = [],
  isRevealing = false,
  onRevealComplete,
}) {
  const containerRef = useRef(null);
  const chartRef     = useRef(null);
  const seriesRef    = useRef(null);
  const timersRef    = useRef([]);
  const mountedRef   = useRef(true);

  /* ── Create chart ─────────────────────────────────────────── */
  useEffect(() => {
    mountedRef.current = true;
    const container = containerRef.current;
    if (!container) return;

    const chart = createChart(container, {
      autoSize: true,
      layout: {
        background: { type: 'solid', color: '#0d1117' },
        textColor: '#8b949e',
      },
      grid: {
        vertLines: { color: '#161b22' },
        horzLines: { color: '#161b22' },
      },
      crosshair:       { mode: 1 },
      rightPriceScale: { borderColor: '#30363d' },
      timeScale:       { borderColor: '#30363d', timeVisible: false, fixLeftEdge: true },
    });

    const series = chart.addCandlestickSeries({
      upColor:        '#3fb950',
      downColor:      '#f85149',
      borderUpColor:  '#3fb950',
      borderDownColor:'#f85149',
      wickUpColor:    '#3fb950',
      wickDownColor:  '#f85149',
    });

    chartRef.current  = chart;
    seriesRef.current = series;

    return () => {
      mountedRef.current = false;
      timersRef.current.forEach(clearTimeout);
      timersRef.current = [];
      try { chart.remove(); } catch (_) {}
      chartRef.current  = null;
      seriesRef.current = null;
    };
  }, []);

  /* ── Load data ────────────────────────────────────────────── */
  useEffect(() => {
    if (!seriesRef.current || !chartRef.current) return;
    if (!candles || candles.length === 0) return;
    timersRef.current.forEach(clearTimeout);
    timersRef.current = [];
    try {
      seriesRef.current.setData(candles);
      chartRef.current.timeScale().setVisibleLogicalRange({
        from: 0,
        to: candles.length + 4,
      });
    } catch (_) {}
  }, [candles]);

  /* ── Reveal animation ─────────────────────────────────────── */
  useEffect(() => {
    if (!isRevealing) return;
    if (!revealCandles || revealCandles.length === 0) {
      // Nothing to reveal — call completion immediately
      const t = setTimeout(() => { if (mountedRef.current) onRevealComplete?.(); }, 300);
      return () => clearTimeout(t);
    }

    timersRef.current.forEach(clearTimeout);
    timersRef.current = [];

    revealCandles.forEach((candle, i) => {
      const t = setTimeout(() => {
        if (!mountedRef.current || !seriesRef.current) return;
        try {
          seriesRef.current.update(candle);
          chartRef.current?.timeScale().setVisibleLogicalRange({
            from: 0,
            to: (candles?.length ?? 0) + revealCandles.length + 3,
          });
        } catch (_) {}

        if (i === revealCandles.length - 1) {
          setTimeout(() => { if (mountedRef.current) onRevealComplete?.(); }, 400);
        }
      }, i * 350);
      timersRef.current.push(t);
    });

    return () => timersRef.current.forEach(clearTimeout);
  }, [isRevealing]); // eslint-disable-line react-hooks/exhaustive-deps

  return <div ref={containerRef} style={{ width: '100%', height: '100%' }} />;
}
