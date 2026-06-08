import { useState, useEffect } from 'react';
import { parseYahoo } from '../utils/candleUtils';
import { detectPatterns } from '../utils/patternDetector';
import patternDefinitions from '../data/patternDefinitions';

const TICKERS = ['SPY', 'AAPL', 'TSLA', 'MSFT', 'NVDA'];

// Yahoo Finance supports these interval/range combos:
//   60m  → max 60d of data
//   1d   → multi-year
//   1wk  → multi-year
export const TIMEFRAMES = {
  '1H': { interval: '60m', range: '60d',  label: '1 שעה',   sublabel: '60 ימים / שעתי' },
  '1D': { interval: '1d',  range: '2y',   label: 'יומי',     sublabel: '2 שנים / יומי'  },
  '1W': { interval: '1wk', range: '5y',   label: 'שבועי',   sublabel: '5 שנים / שבועי' },
};

async function fetchTicker(symbol, interval, range) {
  const url = `/api/finance/${symbol}?interval=${interval}&range=${range}&includePrePost=false`;
  const res = await fetch(url, { headers: { Accept: 'application/json' } });
  if (!res.ok) throw new Error(`HTTP ${res.status}`);
  const json = await res.json();
  return parseYahoo(json);
}

export function useMarketData(timeframeKey = '1D') {
  const [state, setState] = useState({
    status: 'idle',
    realPatterns: [],
    tickerData: {},
    error: null,
  });

  useEffect(() => {
    let cancelled = false;
    const tf = TIMEFRAMES[timeframeKey] || TIMEFRAMES['1D'];

    setState({ status: 'loading', realPatterns: [], tickerData: {}, error: null });

    Promise.allSettled(
      TICKERS.map((sym) =>
        fetchTicker(sym, tf.interval, tf.range).then((c) => ({ sym, candles: c }))
      )
    ).then((results) => {
      if (cancelled) return;

      const tickerData = {};
      const allPatterns = [];

      for (const r of results) {
        if (r.status === 'fulfilled' && r.value.candles.length > 40) {
          const { sym, candles } = r.value;
          tickerData[sym] = candles;
          const found = detectPatterns(candles, patternDefinitions, sym);
          // Tag each pattern with the timeframe
          found.forEach((p) => (p.timeframe = timeframeKey));
          allPatterns.push(...found);
        }
      }

      // Shuffle
      for (let i = allPatterns.length - 1; i > 0; i--) {
        const j = Math.floor(Math.random() * (i + 1));
        [allPatterns[i], allPatterns[j]] = [allPatterns[j], allPatterns[i]];
      }

      setState({ status: 'ready', realPatterns: allPatterns, tickerData, error: null });
    }).catch((err) => {
      if (!cancelled) setState((s) => ({ ...s, status: 'error', error: err.message }));
    });

    return () => { cancelled = true; };
  }, [timeframeKey]);

  return state;
}
