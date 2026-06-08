import { body, totalRange, upperWick, lowerWick, isGreen, isRed, inDowntrend, inUptrend } from './candleUtils';

// Each detector: (candles, endIdx) → patternLength | null
// "endIdx" is the last candle of the pattern

const detectors = {
  hammer(candles, i) {
    if (i < 8) return null;
    const c = candles[i];
    const b = body(c), r = totalRange(c);
    const lw = lowerWick(c), uw = upperWick(c);
    if (r < 0.5 || b / r > 0.4 || lw / r < 0.5 || uw / r > 0.15) return null;
    if (!inDowntrend(candles.slice(0, i), 7)) return null;
    return 1;
  },

  shooting_star(candles, i) {
    if (i < 8) return null;
    const c = candles[i];
    const b = body(c), r = totalRange(c);
    const lw = lowerWick(c), uw = upperWick(c);
    if (r < 0.5 || b / r > 0.4 || uw / r < 0.5 || lw / r > 0.15) return null;
    if (!inUptrend(candles.slice(0, i), 7)) return null;
    return 1;
  },

  bullish_engulfing(candles, i) {
    if (i < 9) return null;
    const prev = candles[i - 1], curr = candles[i];
    if (!isRed(prev) || !isGreen(curr)) return null;
    if (curr.open >= prev.close || curr.close <= prev.open) return null;
    if (body(curr) < body(prev) * 1.3) return null;
    if (body(prev) / prev.close < 0.005) return null;
    if (!inDowntrend(candles.slice(0, i - 1), 7)) return null;
    return 2;
  },

  bearish_engulfing(candles, i) {
    if (i < 9) return null;
    const prev = candles[i - 1], curr = candles[i];
    if (!isGreen(prev) || !isRed(curr)) return null;
    if (curr.open <= prev.close || curr.close >= prev.open) return null;
    if (body(curr) < body(prev) * 1.3) return null;
    if (body(prev) / prev.close < 0.005) return null;
    if (!inUptrend(candles.slice(0, i - 1), 7)) return null;
    return 2;
  },

  morning_star(candles, i) {
    if (i < 10) return null;
    const c1 = candles[i - 2], c2 = candles[i - 1], c3 = candles[i];
    if (!isRed(c1) || !isGreen(c3)) return null;
    if (body(c1) / c1.close < 0.01) return null;
    if (body(c2) / c2.close > 0.005) return null; // doji middle
    if (body(c3) / c3.close < 0.008) return null;
    if (c3.close < (c1.open + c1.close) / 2) return null; // closes into c1 body
    if (!inDowntrend(candles.slice(0, i - 2), 7)) return null;
    return 3;
  },

  evening_star(candles, i) {
    if (i < 10) return null;
    const c1 = candles[i - 2], c2 = candles[i - 1], c3 = candles[i];
    if (!isGreen(c1) || !isRed(c3)) return null;
    if (body(c1) / c1.close < 0.01) return null;
    if (body(c2) / c2.close > 0.005) return null;
    if (body(c3) / c3.close < 0.008) return null;
    if (c3.close > (c1.open + c1.close) / 2) return null;
    if (!inUptrend(candles.slice(0, i - 2), 7)) return null;
    return 3;
  },

  doji_support(candles, i) {
    if (i < 8) return null;
    const c = candles[i];
    const b = body(c), r = totalRange(c);
    if (r / c.close < 0.005) return null;
    if (b / r > 0.07) return null; // very small body
    if (!inDowntrend(candles.slice(0, i), 7)) return null;
    return 1;
  },

  three_white_soldiers(candles, i) {
    if (i < 10) return null;
    const c1 = candles[i - 2], c2 = candles[i - 1], c3 = candles[i];
    if (!isGreen(c1) || !isGreen(c2) || !isGreen(c3)) return null;
    if (body(c1) / c1.close < 0.007 || body(c2) / c2.close < 0.007 || body(c3) / c3.close < 0.007) return null;
    if (c2.open < c1.open || c2.open > c1.close) return null; // opens within c1 body
    if (c3.open < c2.open || c3.open > c2.close) return null;
    if (c2.close <= c1.close || c3.close <= c2.close) return null;
    return 3;
  },

  three_black_crows(candles, i) {
    if (i < 10) return null;
    const c1 = candles[i - 2], c2 = candles[i - 1], c3 = candles[i];
    if (!isRed(c1) || !isRed(c2) || !isRed(c3)) return null;
    if (body(c1) / c1.close < 0.007 || body(c2) / c2.close < 0.007 || body(c3) / c3.close < 0.007) return null;
    if (c2.open > c1.open || c2.open < c1.close) return null;
    if (c3.open > c2.open || c3.open < c2.close) return null;
    if (c2.close >= c1.close || c3.close >= c2.close) return null;
    return 3;
  },

  inverted_hammer(candles, i) {
    if (i < 8) return null;
    const c = candles[i];
    const b = body(c), r = totalRange(c);
    const lw = lowerWick(c), uw = upperWick(c);
    if (r < 0.5 || b / r > 0.4 || uw / r < 0.5 || lw / r > 0.15) return null;
    if (!inDowntrend(candles.slice(0, i), 7)) return null;
    return 1;
  },

  hanging_man(candles, i) {
    if (i < 8) return null;
    const c = candles[i];
    const b = body(c), r = totalRange(c);
    const lw = lowerWick(c), uw = upperWick(c);
    if (r < 0.5 || b / r > 0.4 || lw / r < 0.5 || uw / r > 0.15) return null;
    if (!inUptrend(candles.slice(0, i), 7)) return null;
    return 1;
  },

  bullish_marubozu(candles, i) {
    if (i < 5) return null;
    const c = candles[i];
    const b = body(c), r = totalRange(c);
    if (!isGreen(c)) return null;
    if (b / r < 0.88) return null; // almost no wicks
    if (b / c.close < 0.012) return null; // meaningful move
    return 1;
  },
};

const CONTEXT_LEN = 20;
const REVEAL_LEN = 5;
const MIN_REVEAL_MOVE = 0.008; // 0.8% to confirm pattern worked

export function detectPatterns(candles, patternDefs, ticker) {
  const found = [];
  const usedIndices = new Set();

  for (let i = CONTEXT_LEN + 3; i < candles.length - REVEAL_LEN - 1; i++) {
    for (const def of patternDefs) {
      if (usedIndices.has(i)) continue;

      const detect = detectors[def.id];
      if (!detect) continue;

      const len = detect(candles, i);
      if (!len) continue;

      const patternStart = i - len + 1;
      const contextCandles = candles.slice(Math.max(0, patternStart - CONTEXT_LEN), patternStart);
      const patternCandles = candles.slice(patternStart, i + 1);
      const revealCandles = candles.slice(i + 1, i + 1 + REVEAL_LEN);

      if (revealCandles.length < REVEAL_LEN) continue;

      const patternClose = patternCandles[patternCandles.length - 1].close;
      const futureClose = revealCandles[REVEAL_LEN - 1].close;
      const move = (futureClose - patternClose) / patternClose;

      const succeeded =
        def.direction === 'bullish' ? move > MIN_REVEAL_MOVE : move < -MIN_REVEAL_MOVE;
      if (!succeeded) continue;

      // Mark surrounding indices as used to avoid overlapping patterns
      for (let j = patternStart - 5; j <= i + REVEAL_LEN; j++) usedIndices.add(j);

      found.push({
        id: `${def.id}_${ticker}_${i}`,
        patternId: def.id,
        name: def.name,
        english: def.english,
        direction: def.direction,
        difficulty: def.difficulty,
        description: def.description,
        tip: def.tip,
        explanation: def.explanation,
        questionCandles: [...contextCandles, ...patternCandles],
        revealCandles,
        answer: def.direction === 'bullish' ? 'up' : 'down',
        ticker,
        isReal: true,
      });
    }
  }

  return found;
}
