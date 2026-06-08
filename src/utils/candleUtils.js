export const r2 = (n) => Math.round(n * 100) / 100;
export const body = (c) => Math.abs(c.close - c.open);
export const totalRange = (c) => c.high - c.low || 0.0001;
export const upperWick = (c) => c.high - Math.max(c.open, c.close);
export const lowerWick = (c) => Math.min(c.open, c.close) - c.low;
export const isGreen = (c) => c.close >= c.open;
export const isRed = (c) => c.close < c.open;

export function trendSlope(candles, n = 5) {
  if (candles.length < n) return 0;
  const sl = candles.slice(-n);
  return (sl[sl.length - 1].close - sl[0].close) / sl[0].close;
}

export function inDowntrend(candles, n = 5, threshold = -0.015) {
  return trendSlope(candles, n) < threshold;
}

export function inUptrend(candles, n = 5, threshold = 0.015) {
  return trendSlope(candles, n) > threshold;
}

// Parse Yahoo Finance v8 JSON into OHLCV array
export function parseYahoo(data) {
  try {
    const result = data.chart.result[0];
    const ts = result.timestamp;
    const q = result.indicators.quote[0];
    return ts
      .map((t, i) => ({
        time: t,
        open: q.open[i],
        high: q.high[i],
        low: q.low[i],
        close: q.close[i],
      }))
      .filter(
        (c) =>
          c.open != null && c.high != null && c.low != null && c.close != null &&
          c.high >= Math.max(c.open, c.close) &&
          c.low <= Math.min(c.open, c.close) &&
          c.high > 0
      )
      .map((c) => ({ ...c, open: r2(c.open), high: r2(c.high), low: r2(c.low), close: r2(c.close) }));
  } catch {
    return [];
  }
}
