const r2 = n => Math.round(n * 100) / 100;
const D = 86400, T0 = 1704067200;

function lcg(seed) {
  let s = (seed ^ 0xdeadbeef) >>> 0 || 1;
  return () => { s = (Math.imul(1664525, s) + 1013904223) >>> 0; return s / 4294967296; };
}
function buildCandles(steps, seedOff = 9999) {
  const rng = lcg(seedOff);
  return steps.map((s, i) => ({
    time: T0 + i * D,
    open: r2(s.o), close: r2(s.c),
    high: r2(Math.max(s.o, s.c) + (s.wu ?? rng() * 0.9)),
    low:  r2(Math.min(s.o, s.c) - (s.wd ?? rng() * 0.7)),
  }));
}
function computeMA(candles, period) {
  return candles.map((_, i) => {
    if (i < period - 1) return null;
    return r2(candles.slice(i - period + 1, i + 1).reduce((s, c) => s + c.close, 0) / period);
  });
}

// ─── helper trend generator ──────────────────────────────────────────────────
function genTrend(startPrice, count, bias, seed) {
  const rng = lcg(seed);
  const steps = [];
  let p = startPrice;
  for (let i = 0; i < count; i++) {
    const c = p + bias + (rng() - 0.5) * 1.2;
    steps.push({ o: p, c, wu: rng() * 0.7, wd: rng() * 0.6 });
    p = c;
  }
  return steps;
}

// ═══ LESSON 1: נרות יפניים ═══════════════════════════════════════════════════
const candleBasic = buildCandles([
  { o:90,c:94,wu:1.5,wd:1.2 },{ o:94,c:91,wu:1.0,wd:1.8 },
  { o:91,c:96,wu:0.8,wd:0.5 },{ o:96,c:92,wu:2.0,wd:1.0 },
  { o:92,c:97,wu:1.2,wd:0.9 },
]);
const dojiExample = buildCandles([
  { o:90,c:93 },{ o:93,c:88 },{ o:88,c:90 },
  { o:90,c:90.1,wu:3.5,wd:3.5 },
  { o:90.1,c:93 },{ o:93,c:95 },
]);

// ═══ LESSON 2: טיים-פריים ════════════════════════════════════════════════════
const noisy1H  = buildCandles(Array.from({length:16},(_,i)=>({ o:100+Math.sin(i*1.3)*3, c:100+Math.sin((i+1)*1.3)*3+(i%3===0?-1:0.8), wu:0.6,wd:0.6 })));
const clean1D  = buildCandles(genTrend(85, 20, 0.4, 2001));
const weekly1W = buildCandles(Array.from({length:12},(_,i)=>({ o:82+i*1.4, c:82+(i+0.8)*1.4, wu:1.5,wd:1.2 })));

// ═══ LESSON 3: תמיכה והתנגדות ════════════════════════════════════════════════
const SUPPORT = 85, RESISTANCE = 95, FLIP_LEVEL = 88;
const supportChart = buildCandles([
  {o:92,c:91},{o:91,c:89.5},{o:89.5,c:88},{o:88,c:86.5},{o:86.5,c:85.2},{o:85.2,c:85.0,wu:0.3,wd:0.2},
  {o:85.0,c:86.5},{o:86.5,c:88},{o:88,c:89.5},{o:89.5,c:90},
  {o:90,c:88.5},{o:88.5,c:87},{o:87,c:86},{o:86,c:85.1,wu:0.2,wd:0.3},
  {o:85.1,c:86.5},{o:86.5,c:87.8},{o:87.8,c:88.5},
  {o:88.5,c:87.2},{o:87.2,c:86},{o:86,c:85.0,wu:0.2,wd:0.2},
  {o:85.0,c:86.8},{o:86.8,c:88.5},{o:88.5,c:90},
]);
const resistanceChart = buildCandles([
  {o:86,c:87.5},{o:87.5,c:89},{o:89,c:91},{o:91,c:93},{o:93,c:94.5},{o:94.5,c:94.8,wu:1.5,wd:0.3},
  {o:94.8,c:93},{o:93,c:91.5},{o:91.5,c:90.5},
  {o:90.5,c:92},{o:92,c:93.5},{o:93.5,c:94.8},{o:94.8,c:94.9,wu:1.8,wd:0.3},
  {o:94.9,c:93.2},{o:93.2,c:92},{o:92,c:91},
  {o:91,c:92.5},{o:92.5,c:94},{o:94,c:94.7},{o:94.7,c:94.6,wu:1.6,wd:0.3},
  {o:94.6,c:93},{o:93,c:91.5},
]);
const flipChart = buildCandles([
  {o:94,c:92},{o:92,c:90},{o:90,c:88.3},{o:88.3,c:88.0,wu:0.3,wd:0.2},
  {o:88.0,c:89.5},{o:89.5,c:91},{o:91,c:90},
  {o:90,c:88.5},{o:88.5,c:87.2},{o:87.2,c:85.5},{o:85.5,c:83.8},{o:83.8,c:83.0},
  {o:83.0,c:85},{o:85,c:86.8},{o:86.8,c:87.9},{o:87.9,c:87.8,wu:1.0,wd:0.3},
  {o:87.8,c:86},{o:86,c:84.5},{o:84.5,c:83.2},
]);

// ═══ LESSON 4: כוס וידית ═════════════════════════════════════════════════════
const RIM = 100, BOTTOM = 78;
const cupHandleCandles = (() => {
  const rng = lcg(7001);
  const steps = [];
  const add = (o, c, wu = null, wd = null) =>
    steps.push({ o, c, wu: wu ?? rng() * 0.9, wd: wd ?? rng() * 0.7 });
  for (let i = 0; i < 8; i++) { const p = 88 + i * 1.5; add(p, p + 1.4); }
  for (let i = 0; i < 15; i++) { const p = RIM - ((RIM - BOTTOM) / 14) * i; add(p, p - 1.5, rng() * 0.5, rng() * 1.2); }
  for (let i = 0; i < 10; i++) { const arch = Math.sin(Math.PI * i / 9) * 2; add(BOTTOM + arch - 0.5, BOTTOM + arch + 0.5); }
  for (let i = 0; i < 15; i++) { const p = BOTTOM + ((RIM - BOTTOM) / 14) * i; add(p, p + 1.5, rng() * 0.5, rng() * 0.5); }
  for (let i = 0; i < 7; i++) { const dip = Math.sin(Math.PI * (i + 1) / 8) * 5; add(RIM - dip - 1, RIM - dip, rng() * 0.5, rng() * 0.6); }
  return buildCandles(steps);
})();
const cupHandleReveal = buildCandles([
  {o:93,c:96},{o:96,c:99.5},{o:99.5,c:102},{o:102,c:104},{o:104,c:106.5},
]);

// ═══ LESSON 5: קווי מגמה ════════════════════════════════════════════════════
const uptrendCandles = buildCandles([
  {o:80,c:81.5},{o:81.5,c:80.2,wu:0.5,wd:0.6},{o:80.2,c:82.5},{o:82.5,c:83.8},{o:83.8,c:82.5,wu:0.5,wd:0.7},
  {o:82.5,c:85},{o:85,c:86.5},{o:86.5,c:85.1,wu:0.5,wd:0.7},{o:85.1,c:87.5},{o:87.5,c:89},
  {o:89,c:88,wu:0.5,wd:0.6},{o:88,c:90.5},{o:90.5,c:92},{o:92,c:91,wu:0.5,wd:0.6},{o:91,c:93.5},{o:93.5,c:95},
]);
const downtrendCandles = buildCandles([
  {o:95,c:93.5},{o:93.5,c:94.5,wu:0.9,wd:0.5},{o:94.5,c:92},{o:92,c:90.5},
  {o:90.5,c:91.5,wu:0.8,wd:0.5},{o:91.5,c:89},{o:89,c:87.5},
  {o:87.5,c:88.8,wu:0.9,wd:0.5},{o:88.8,c:86.5},{o:86.5,c:85},
  {o:85,c:86,wu:0.7,wd:0.5},{o:86,c:83.5},{o:83.5,c:82},
  {o:82,c:83.2,wu:0.8,wd:0.5},{o:83.2,c:80.5},{o:80.5,c:79},
]);
const trendBreakCandles = buildCandles([
  {o:96,c:94},{o:94,c:95,wu:0.9,wd:0.5},{o:95,c:93},{o:93,c:91},{o:91,c:92,wu:0.8,wd:0.5},
  {o:92,c:90},{o:90,c:88},{o:88,c:89,wu:0.7,wd:0.5},
  {o:89,c:91.5,wu:1.2,wd:0.6},{o:91.5,c:93.5},{o:93.5,c:95},
  {o:95,c:94,wu:0.7,wd:0.5},{o:94,c:96.5},{o:96.5,c:98},
]);

// ═══ LESSON 6: ממוצעים נעים ══════════════════════════════════════════════════
const maTrendCandles = buildCandles([
  {o:80,c:81.5},{o:81.5,c:80.5,wu:0.5,wd:0.7},{o:80.5,c:82},{o:82,c:83.5},{o:83.5,c:82.5,wu:0.5,wd:0.7},
  {o:82.5,c:84},{o:84,c:85.5},{o:85.5,c:84.5,wu:0.5,wd:0.6},{o:84.5,c:86},{o:86,c:87.5},
  {o:87.5,c:86.5,wu:0.5,wd:0.7},{o:86.5,c:88},{o:88,c:89.5},{o:89.5,c:88.5,wu:0.5,wd:0.6},{o:88.5,c:90},
  {o:90,c:91.5},{o:91.5,c:90.5,wu:0.5,wd:0.6},{o:90.5,c:92},{o:92,c:93.5},{o:93.5,c:92.5},
], 6001);

// Golden Cross: decline then recovery where short MA crosses above long MA
const goldenCrossCandles = buildCandles([
  // Downtrend (short MA below long)
  {o:96,c:94},{o:94,c:92},{o:92,c:90},{o:90,c:88},{o:88,c:87},{o:87,c:86},{o:86,c:85},{o:85,c:84},
  // Bottom and recovery
  {o:84,c:84.5},{o:84.5,c:85},{o:85,c:86.5},{o:86.5,c:88},{o:88,c:90},{o:90,c:92},{o:92,c:93.5},
  {o:93.5,c:95},{o:95,c:96.5},{o:96.5,c:98},{o:98,c:99.5},{o:99.5,c:101},
], 6002);
const ma5gc  = computeMA(goldenCrossCandles, 5);
const ma12gc = computeMA(goldenCrossCandles, 12);
const ma5  = computeMA(maTrendCandles, 5);
const ma12 = computeMA(maTrendCandles, 12);

// ═══ LESSON 7: נפח מסחר ══════════════════════════════════════════════════════
// Simulated: big candles = high volume, small = low volume
const volumeConfirmCandles = buildCandles([
  // Low volume consolidation (small candles)
  {o:88,c:88.5,wu:0.4,wd:0.4},{o:88.5,c:88.2,wu:0.3,wd:0.4},{o:88.2,c:88.7,wu:0.4,wd:0.3},
  {o:88.7,c:88.4,wu:0.3,wd:0.3},{o:88.4,c:88.8,wu:0.4,wd:0.3},{o:88.8,c:88.5,wu:0.3,wd:0.4},
  // High volume breakout (big candles)
  {o:88.5,c:91.5,wu:1.0,wd:0.5},{o:91.5,c:94,wu:0.8,wd:0.6},{o:94,c:96.5,wu:0.7,wd:0.5},
  {o:96.5,c:99,wu:0.9,wd:0.6},{o:99,c:101,wu:0.8,wd:0.5},
], 7001);
const volumeDivCandles = buildCandles([
  // Price rising but with smaller candles (divergence)
  {o:82,c:86,wu:1.2,wd:0.8},{o:86,c:89,wu:1.0,wd:0.7},{o:89,c:91,wu:0.8,wd:0.6},
  {o:91,c:92.5,wu:0.6,wd:0.5},{o:92.5,c:93.5,wu:0.4,wd:0.4},{o:93.5,c:94.2,wu:0.3,wd:0.3},
  {o:94.2,c:94.6,wu:0.2,wd:0.3},{o:94.6,c:94.8,wu:0.2,wd:0.2},
  // Reversal
  {o:94.8,c:92},{o:92,c:89},{o:89,c:86},{o:86,c:83},
], 7002);

// ═══ LESSON 8: ראש וכתפיים ════════════════════════════════════════════════════
const NECKLINE = 86;
const hsCandles = buildCandles([
  {o:82,c:84},{o:84,c:86},{o:86,c:89},{o:89,c:92,wu:0.5,wd:0.4},  // left shoulder peak
  {o:92,c:89},{o:89,c:87},{o:87,c:86.0,wu:0.4,wd:0.3},             // pullback to neckline
  {o:86.0,c:89},{o:89,c:93},{o:93,c:96},{o:96,c:98,wu:0.5,wd:0.4}, // head peak
  {o:98,c:95},{o:95,c:91},{o:91,c:87},{o:87,c:86.0,wu:0.4,wd:0.3}, // back to neckline
  {o:86.0,c:88},{o:88,c:90},{o:90,c:91.5,wu:0.5,wd:0.4},           // right shoulder peak (lower)
  {o:91.5,c:89},{o:89,c:87},{o:87,c:86.1,wu:0.4,wd:0.3},           // back to neckline
  {o:86.1,c:84},{o:84,c:82},{o:82,c:80},{o:80,c:78},                // BREAK
], 8001);
const hsReveal = buildCandles([{o:78,c:76},{o:76,c:74},{o:74,c:73},{o:73,c:74.5},{o:74.5,c:73}], 8002);

// ═══ LESSON 9: שיא כפול / תחתית כפולה ════════════════════════════════════════
const DTOP_NECK = 88, DBOT_NECK = 92;
const doubleTopCandles = buildCandles([
  {o:80,c:83},{o:83,c:86},{o:86,c:89},{o:89,c:92},{o:92,c:95,wu:0.5,wd:0.4},
  {o:95,c:92},{o:92,c:89},{o:89,c:88,wu:0.4,wd:0.3},
  {o:88,c:90},{o:90,c:93},{o:93,c:95,wu:0.5,wd:0.4},
  {o:95,c:92},{o:92,c:89},{o:89,c:87},{o:87,c:85},{o:85,c:83},{o:83,c:81},{o:81,c:79},
], 9001);
const doubleBottomCandles = buildCandles([
  {o:98,c:96},{o:96,c:93},{o:93,c:90},{o:90,c:87},{o:87,c:84,wu:0.4,wd:0.5},
  {o:84,c:87},{o:87,c:89},{o:89,c:91},{o:91,c:92,wu:0.5,wd:0.4},
  {o:92,c:90},{o:90,c:87},{o:87,c:84,wu:0.4,wd:0.5},
  {o:84,c:87},{o:87,c:90},{o:90,c:93},{o:93,c:95},{o:95,c:97},{o:97,c:99},
], 9002);

// ═══ LESSON 10: דגלים ════════════════════════════════════════════════════════
const flagCandles = buildCandles([
  // Flagpole: strong up
  {o:78,c:81,wu:0.5,wd:0.4},{o:81,c:84},{o:84,c:87},{o:87,c:90},{o:90,c:93},{o:93,c:96,wu:0.4,wd:0.5},
  // Flag: slight down channel (consolidation)
  {o:96,c:94,wu:0.6,wd:0.4},{o:94,c:93.2,wu:0.5,wd:0.5},{o:93.2,c:94,wu:0.4,wd:0.5},
  {o:94,c:92.5,wu:0.5,wd:0.4},{o:92.5,c:93,wu:0.6,wd:0.4},{o:93,c:91.5,wu:0.5,wd:0.5},
  // Breakout
  {o:91.5,c:94,wu:0.7,wd:0.4},{o:94,c:97},{o:97,c:99},{o:99,c:101},{o:101,c:103},
], 10001);

// ═══ LESSON 11: משולשים ═════════════════════════════════════════════════════
const ascTriangleCandles = buildCandles([
  {o:83,c:86},{o:86,c:88},{o:88,c:91},{o:91,c:93},{o:93,c:95,wu:0.3,wd:0.4},
  {o:95,c:92},{o:92,c:90},{o:90,c:88.5,wu:0.4,wd:0.3},
  {o:88.5,c:90},{o:90,c:92},{o:92,c:94},{o:94,c:95.2,wu:0.3,wd:0.4},
  {o:95.2,c:93},{o:93,c:91},{o:91,c:90,wu:0.4,wd:0.3},
  {o:90,c:92},{o:92,c:94},{o:94,c:96.5,wu:0.4,wd:0.4},{o:96.5,c:98.5},{o:98.5,c:100.5},
], 11001);
const descTriangleCandles = buildCandles([
  {o:97,c:95},{o:95,c:92},{o:92,c:90},{o:90,c:88,wu:0.3,wd:0.5},
  {o:88,c:90.5},{o:90.5,c:91,wu:0.7,wd:0.3},
  {o:91,c:89},{o:89,c:87},{o:87,c:85,wu:0.3,wd:0.5},
  {o:85,c:87.5},{o:87.5,c:88,wu:0.5,wd:0.3},
  {o:88,c:86},{o:86,c:84},{o:84,c:82.5,wu:0.3,wd:0.5},  // breakdown!
  {o:82.5,c:80},{o:80,c:78},{o:78,c:76},
], 11002);

// ═══ LESSON 12: פיבונאצ'י ════════════════════════════════════════════════════
const fibHigh = 100, fibLow = 80;
const fib236 = r2(fibHigh - (fibHigh - fibLow) * 0.236);  // 95.3
const fib382 = r2(fibHigh - (fibHigh - fibLow) * 0.382);  // 92.4
const fib50  = r2(fibHigh - (fibHigh - fibLow) * 0.5);    // 90
const fib618 = r2(fibHigh - (fibHigh - fibLow) * 0.618);  // 87.6

const fibCandles = buildCandles([
  {o:80,c:83},{o:83,c:86},{o:86,c:89},{o:89,c:92},{o:92,c:95},{o:95,c:98},{o:98,c:100,wu:0.5,wd:0.4},
  {o:100,c:97},{o:97,c:94},{o:94,c:91},{o:91,c:89},{o:89,c:88},{o:88,c:87.8,wu:0.4,wd:0.5},
  {o:87.8,c:90},{o:90,c:93},{o:93,c:96},{o:96,c:99},{o:99,c:101},{o:101,c:103},
], 12001);

// ═══ LESSON 13: פסיכולוגיית שוק ══════════════════════════════════════════════
const psychoCandles = buildCandles([
  // צבירה
  {o:80,c:80.5,wu:0.3,wd:0.3},{o:80.5,c:81,wu:0.3,wd:0.3},{o:81,c:81.5,wu:0.3,wd:0.3},
  {o:81.5,c:82,wu:0.3,wd:0.3},{o:82,c:82.5,wu:0.3,wd:0.3},
  // תקווה
  {o:82.5,c:84,wu:0.7,wd:0.5},{o:84,c:86},{o:86,c:88},
  // תאוות בצע / FOMO
  {o:88,c:91,wu:1.0,wd:0.5},{o:91,c:94},{o:94,c:97},{o:97,c:100},{o:100,c:103,wu:1.2,wd:0.5},
  // שיא ותחילת ירידה
  {o:103,c:101},{o:101,c:98},{o:98,c:95},
  // פחד ופאניקה
  {o:95,c:91},{o:91,c:87},{o:87,c:83,wu:0.4,wd:1.2},{o:83,c:80},
  // יאוש
  {o:80,c:79.5,wu:0.3,wd:0.5},{o:79.5,c:79.8,wu:0.3,wd:0.3},{o:79.8,c:80.2,wu:0.3,wd:0.3},
], 13001);

// ═══ EXPORT ══════════════════════════════════════════════════════════════════
const lessons = [
  // ── 1 ──────────────────────────────────────────────────────────────────────
  {
    id:'candles', icon:'🕯️', title:'קריאת נרות יפניים', color:'#388bfd', duration:'5 דקות',
    description:'הבסיס של ניתוח טכני — כיצד קוראים נר, מה המשמעות של גוף ולהבים',
    slides:[
      { title:'מהו נר יפני?', body:'נר יפני מסכם 4 מחירים לתקופה: פתיחה, סגירה, שיא ושפל.\n\nנר ירוק = סגירה גבוהה מהפתיחה. נר אדום = סגירה נמוכה מהפתיחה.',
        keyPoints:['פתיחה (Open) — מחיר הפתיחה','סגירה (Close) — מחיר הסגירה','שיא (High) — הגבוה ביותר','שפל (Low) — הנמוך ביותר'],
        visual:{ candles:candleBasic, overlays:[], caption:'נרות ירוקים ואדומים — כל נר מסכם תקופת מסחר' }},
      { title:'גוף הנר (Body)', body:'הגוף הוא המרחק בין הפתיחה לסגירה.\n\nגוף גדול = תנועה חזקה. גוף קטן = אי-וודאות. נר ירוק עם גוף גדול מראה שהקונים שלטו לאורך כל התקופה.',
        keyPoints:['גוף גדול ירוק = קנייה חזקה','גוף גדול אדום = מכירה חזקה','גוף קטן = שוק מהסס'],
        visual:{ candles:candleBasic, overlays:[], caption:'שים לב לגדלי הגופים — הם מספרים את עוצמת המסחר' }},
      { title:'הלהבים (Wicks)', body:'הקווים הדקים מעל ומתחת לגוף הם הלהבים. הם מראים לאן הגיע המחיר אך לא הצליח להישאר.\n\nלהב עליון ארוך = מוכרים חזקים בשיא. להב תחתון ארוך = קונים חזקים בשפל.',
        keyPoints:['להב = ניסיון שנכשל','להב עליון ארוך → מוכרים חזקים','להב תחתון ארוך → קונים חזקים','ללא להבים = שליטה מוחלטת'],
        visual:{ candles:candleBasic, overlays:[], caption:'הלהבים חושפים את הקרב בין קונים למוכרים' }},
      { title:"הדוג'י — נר של היסוס", body:"דוג'י נוצר כאשר הפתיחה והסגירה כמעט זהות. גוף מאוד קטן עם להבים ארוכים.\n\nמשמעות: שוק מהסס. לאחר מגמה חזקה — סימן לשינוי אפשרי.",
        keyPoints:["פתיחה ≈ סגירה (גוף קטן מאוד)","להבים ארוכים משני הצדדים","בסוף מגמה = אזהרה לשינוי","חכה לנר האישור שאחריו"],
        visual:{ candles:dojiExample, overlays:[{type:'arrow',idx:3,text:"דוג'י",color:'#d29922',position:'top'}], caption:"הנר הרביעי הוא דוג'י — פתיחה וסגירה כמעט זהות" }},
    ],
  },
  // ── 2 ──────────────────────────────────────────────────────────────────────
  {
    id:'timeframes', icon:'⏰', title:'טיים-פריים ותבניות', color:'#d29922', duration:'7 דקות',
    description:'איזה תבנית הכי אמינה על גרף שעתי, יומי ושבועי — ולמה',
    slides:[
      { title:'מהו טיים-פריים?', body:'טיים-פריים קובע כמה זמן מסחר מסכם כל נר. 1H = שעה, 1D = יום, 1W = שבוע.\n\nאותה מניה באותו רגע — נראית שונה לגמרי על כל גרף.',
        keyPoints:['1H = שעה (אינטרה-דיי)','1D = יום (swing trading)','1W = שבוע (מגמה ראשית)','כל טיים-פריים מתאים לסגנון שונה'],
        visual:{ candles:noisy1H, overlays:[], caption:'גרף שעתי — הרבה רעש, שינויים תכופים' }},
      { title:'1H — גרף שעתי', body:'כל נר = שעת מסחר. הרבה רעש ותנועות קטנות. תבניות עובדות אך עם דיוק נמוך יותר.\n\nמתאים ל: מסחר יומי, כניסות מדויקות, סיכון קצר.',
        keyPoints:['✓ פטיש, בליעה, כוכב נופל — עובדים','⚠️ הרבה אותות שגויים','⚠️ דורש מעקב רציף','✓ Stop loss קצר'],
        visual:{ candles:noisy1H, overlays:[], caption:'גרף שעתי — הרבה תנועה, פחות אמינות' }},
      { title:'1D — גרף יומי', body:'הטיים-פריים הפופולרי ביותר. כל נר = יום מסחר שלם. הרבה יותר "נקי" משעתי. רוב התבניות הקלאסיות פותחו על גרף זה.',
        keyPoints:['✓✓ כל התבניות — מיטבן כאן','✓ פחות רעש, יותר אמינות','✓ תוצאות תוך 5-10 ימים','מומלץ למתחילים'],
        visual:{ candles:clean1D, overlays:[], caption:'גרף יומי — תנועה ברורה, תבניות קלות לזיהוי' }},
      { title:'1W — גרף שבועי', body:'כל נר = שבוע מסחר. מסנן כמעט את כל הרעש. רק השינויים הגדולים נראים. תבניות על גרף זה — אמינות מאוד.\n\nמתאים ל: משקיעים לטווח בינוני-ארוך.',
        keyPoints:['✓✓✓ כוס וידית, ראש וכתפיים — הכי טוב','✓✓ כוכב הבוקר/ערב — אמינים מאוד','⚠️ תוצאות אחרי שבועות/חודשים','✓ הכסף המוסדי מסתכל כאן'],
        visual:{ candles:weekly1W, overlays:[], caption:'גרף שבועי — תמונה גדולה, פחות רעש, יותר משמעות' }},
      { title:'טבלת ההתאמה', body:'ככל שהטיים-פריים גבוה יותר — האות אמין יותר, אך ממתינים יותר לתוצאה.\n\nכלל הזהב: אשר תבנית 1H עם כיוון ה-1D לפחות.',
        keyPoints:['1H: פטיש, בליעה ← כניסה מהירה','1D: כל התבניות ← הבסיס הכי חזק','1W: כוס וידית, ראש וכתפיים ← היפוכים גדולים','אות חזק = אותה תבנית על כמה טיים-פריימים'],
        visual:{ candles:clean1D, overlays:[], caption:'אות חזק = תבנית על מספר טיים-פריימים בו-זמנית' }},
    ],
  },
  // ── 3 ──────────────────────────────────────────────────────────────────────
  {
    id:'support-resistance', icon:'📏', title:'תמיכה והתנגדות', color:'#3fb950', duration:'8 דקות',
    description:'הכלי הבסיסי ביותר בניתוח טכני — רמות שבהן הקנייה והמכירה מתנגשות',
    slides:[
      { title:'מהי תמיכה?', body:'תמיכה היא מחיר שבו הקונים נכנסים בעוצמה ומונעים ירידה נוספת.\n\nדמיין רצפה שהמחיר "יושב" עליה וקופץ חזרה. ככל שיותר נגיעות — הרמה חזקה יותר.',
        keyPoints:['מחיר שממנו "קופצים" חזרה למעלה','נוצר ממחזור קנייה חוזר','3 נגיעות = תמיכה חזקה','שבירה מתחת = אזהרה חמורה'],
        visual:{ candles:supportChart, overlays:[{type:'zone',priceHigh:SUPPORT+0.8,priceLow:SUPPORT-0.4,color:'#3fb950'},{type:'hline',price:SUPPORT,color:'#3fb950',label:'תמיכה'}], caption:'המחיר נגע בתמיכה 3 פעמים ועלה בכל פעם' }},
      { title:'מהי התנגדות?', body:'התנגדות היא מחיר שבו המוכרים נכנסים בעוצמה ומונעים עלייה נוספת.\n\nכמו תקרה — המחיר מנסה לעבור אותה אך "נדחף" חזרה. ככל שיותר ניסיונות — התקרה חזקה יותר.',
        keyPoints:['מחיר שממנו "נדחפים" חזרה למטה','נוצר ממחזור מכירה חוזר','3 נגיעות = התנגדות חזקה','פריצה מעל = אות קנייה חזק'],
        visual:{ candles:resistanceChart, overlays:[{type:'zone',priceHigh:RESISTANCE+0.5,priceLow:RESISTANCE-0.8,color:'#f85149'},{type:'hline',price:RESISTANCE,color:'#f85149',label:'התנגדות'}], caption:'המחיר הגיע להתנגדות 3 פעמים ויצא מטה בכל פעם' }},
      { title:'תמיכה הופכת להתנגדות', body:'עקרון מרכזי: רמה ששברה תפקיד — הופכת לתפקיד ההפוך.\n\n"The old floor becomes the new ceiling" — תמיכה שנשברת הופכת להתנגדות חדשה.',
        keyPoints:['שבירת תמיכה = התנגדות חדשה','שבירת התנגדות = תמיכה חדשה','פסיכולוגיה: מי שקנה בתמיכה מפסיד ורוצה לצאת','בדוק תמיד: האם הרמה "התהפכה"?'],
        visual:{ candles:flipChart, overlays:[{type:'hline',price:FLIP_LEVEL,color:'#d29922',label:'תמיכה ← התנגדות',dashed:true}], caption:'הרמה ב-88 הייתה תמיכה, נשברה, ועכשיו פועלת כהתנגדות' }},
      { title:'איך מזהים רמות?', body:'חפש מחירים שבהם קרה הרבה בעבר:\n\n1. שיאים ושפלים בולטים\n2. מחירים עגולים (100, 200, 500)\n3. ממוצעים נעים (MA200)',
        keyPoints:['שיאים ושפלים בולטים = הכי חשוב','מחירים עגולים = השוק זוכר','MA200 = מלך התמיכות הדינמיות','ריבוי רמות באותו מחיר = "אשכול תמיכה"'],
        visual:{ candles:supportChart, overlays:[{type:'hline',price:SUPPORT,color:'#3fb950',label:'תמיכה 85'},{type:'hline',price:90,color:'#388bfd',label:'התנגדות 90',dashed:true}], caption:'מספר רמות על אותו גרף — כל אחת סיפור משלה' }},
    ],
  },
  // ── 4 ──────────────────────────────────────────────────────────────────────
  {
    id:'cup-handle', icon:'🏆', title:'הכוס והידית', color:'#d29922', duration:'7 דקות',
    description:'תבנית פריצה קלאסית — U ארוך ואז ידית קטנה לפני ריצה גדולה',
    slides:[
      { title:'מהי תבנית הכוס והידית?', body:'הכוס והידית (Cup and Handle) היא תבנית המשך שורי. מתפתחת לאורך שבועות עד חודשים.\n\nצורת U (הכוס) ולאחריה ירידה קטנה (הידית) — בדיוק כמו כוס עם ידית.',
        keyPoints:['תבנית ארוכה — שבועות עד חודשים','מסמנת המשך עלייה אחרי פריצה','הכי אמינה על 1D ו-1W','פותחה על ידי William O\'Neil'],
        visual:{ candles:cupHandleCandles, overlays:[{type:'hline',price:RIM,color:'#d29922',label:'שפת הכוס',dashed:true}], caption:'צורת U ברורה + ידית קטנה לפני הפריצה' }},
      { title:'שלב 1: הכוס (Cup)', body:'הכוס נוצר אחרי עלייה: המחיר יורד בצורה מעוגלת (לא V חדה!), מגיע לתחתית, ואז מטפס חזרה לאותה רמה.\n\nעומק הכוס: בדרך כלל 15%-35% מהשיא.',
        keyPoints:['ירידה מעוגלת 15%-35%','תחתית U (לא V) = חיובי','זמן: 7 שבועות עד שנה','נפח יורד בירידה, עולה בעלייה'],
        visual:{ candles:cupHandleCandles, overlays:[{type:'phase',fromIdx:8,toIdx:47,color:'#388bfd',label:'הכוס'},{type:'hline',price:RIM,color:'#d29922',label:'שפת הכוס',dashed:true},{type:'hline',price:BOTTOM,color:'#388bfd',label:'תחתית',dashed:true}], caption:'הכוס: ירידה מעוגלת + חזרה לאותה רמה' }},
      { title:'שלב 2: הידית (Handle)', body:'אחרי החזרה לשפת הכוס — ירידה קטנה של 5%-15%: הידית.\n\nהמחיר מתאחד, יורד קצת, ומצמיד את המסחר. "נשימה אחרונה" לפני הפריצה.',
        keyPoints:['ירידה קטנה 5%-15%','הידית יוצרת איחוד','נפח נמוך בידית = בריא','פריצה = כשהמחיר שובר מעל שפת הכוס'],
        visual:{ candles:cupHandleCandles, overlays:[{type:'phase',fromIdx:47,toIdx:55,color:'#d29922',label:'ידית'},{type:'hline',price:RIM,color:'#d29922',label:'שפת הכוס / נקודת פריצה',dashed:true}], caption:'הידית: ירידה קלה לפני הפריצה — המוכרים האחרונים יוצאים' }},
      { title:'שלב 3: הפריצה ויעד', body:'פריצה = המחיר עובר מעל שפת הכוס בנפח גבוה.\n\nיעד = שפת הכוס + עומק הכוס.\n\nדוגמה: שפה 100, תחתית 78 → יעד 122.',
        keyPoints:['פריצה מעל שפת הכוס + נפח גבוה','יעד = שפה + עומק הכוס','Stop Loss מתחת לתחתית הידית','לא לרדוף — קנה רק בפריצה'],
        visual:{ candles:[...cupHandleCandles,...cupHandleReveal], overlays:[{type:'hline',price:RIM,color:'#d29922',label:'פריצה מכאן!',dashed:true},{type:'hline',price:RIM+(RIM-BOTTOM),color:'#3fb950',label:`יעד ${RIM+(RIM-BOTTOM)}`,dashed:true}], caption:'פריצה מעל שפת הכוס — יעד = שפה + עומק' }},
    ],
  },
  // ── 5 ──────────────────────────────────────────────────────────────────────
  {
    id:'trend-lines', icon:'📈', title:'קווי מגמה', color:'#f85149', duration:'6 דקות',
    description:'זיהוי כיוון השוק — קווי מגמה, ערוצים, ואיתות שינוי כיוון',
    slides:[
      { title:'מגמה עולה (Uptrend)', body:'מגמה עולה = שיאים גבוהים יותר ושפלים גבוהים יותר (Higher Highs & Higher Lows).\n\nקו המגמה מחבר את השפלים. כל עוד המחיר מעל הקו — המגמה שלמה.',
        keyPoints:['חבר 2-3 שפלים עולים','קו מגמה = תמיכה דינמית','נגיעה בקו = הזדמנות קנייה','שבירה מתחת = אזהרה'],
        visual:{ candles:uptrendCandles, overlays:[{type:'trendline',fromIdx:1,fromPrice:80.2,toIdx:15,toPrice:95,color:'#3fb950'}], caption:'קו מגמה עולה מחבר שפלים עולים — תמיכה דינמית' }},
      { title:'מגמה יורדת (Downtrend)', body:'מגמה יורדת = שיאים נמוכים יותר ושפלים נמוכים יותר (Lower Highs & Lower Lows).\n\nקו המגמה מחבר את השיאים. כל עוד המחיר מתחת — המגמה שלמה.',
        keyPoints:['חבר 2-3 שיאים יורדים','קו מגמה = התנגדות דינמית','נגיעה בקו = מכירה','שבירה מעל = אזהרה לשינוי'],
        visual:{ candles:downtrendCandles, overlays:[{type:'trendline',fromIdx:1,fromPrice:94.5,toIdx:13,toPrice:83.2,color:'#f85149'}], caption:'קו מגמה יורד מחבר שיאים יורדים — התנגדות דינמית' }},
      { title:'שבירת קו מגמה', body:'שבירת קו מגמה = אחד האותות החזקים לשינוי כיוון.\n\nחשוב: שבירה "אמיתית" = סגירה יומית ברורה מעל הקו + אישור ביום שלאחר מכן.',
        keyPoints:['סגירה מעל קו יורד = שינוי כיוון','בדוק אישור ביום הבא','פריצה + נפח גבוה = חזק יותר','Pullback לקו לאחר פריצה = כניסה נוספת'],
        visual:{ candles:trendBreakCandles, overlays:[{type:'trendline',fromIdx:1,fromPrice:94.5,toIdx:7,toPrice:89,color:'#f85149'},{type:'arrow',idx:8,text:'פריצה!',color:'#3fb950',position:'top'}], caption:'המחיר שבר את קו המגמה היורד — תחילת מגמה עולה חדשה' }},
      { title:'ערוץ מחיר (Channel)', body:'ערוץ = שני קווי מגמה מקבילים. קנה בתחתית (קו תמיכה), מכור בחלק העליון (קו התנגדות).\n\nפריצה מהערוץ = תנועה חזקה לכיוון הפריצה.',
        keyPoints:['2 קווים מקבילים = ערוץ','קנה בתמיכה, מכור בהתנגדות','פריצה = תנועה בגודל הערוץ','ערוץ עולה + נסחר בתחתיתו = חזק'],
        visual:{ candles:uptrendCandles, overlays:[{type:'trendline',fromIdx:1,fromPrice:80.2,toIdx:15,toPrice:95,color:'#3fb950'},{type:'trendline',fromIdx:0,fromPrice:81.5,toIdx:14,toPrice:96.5,color:'#3fb950',dashed:true}], caption:'ערוץ עולה — קו תמיכה (מלא) וקו התנגדות (מקווקו) מקבילים' }},
    ],
  },
  // ── 6 ──────────────────────────────────────────────────────────────────────
  {
    id:'moving-averages', icon:'〰️', title:'ממוצעים נעים', color:'#f0883e', duration:'8 דקות',
    description:'כלי קריטי לזיהוי כיוון מגמה — MA20, MA50, MA200 ואותות הצלבה',
    slides:[
      { title:'מהו ממוצע נע?', body:'ממוצע נע (Moving Average) = ממוצע מחירי הסגירה של N הנרות האחרונים.\n\nMA20 = ממוצע 20 הימים האחרונים. המחיר מעל MA = מגמה עולה. מתחת = יורדת.\n\nהממוצע "נע" כי הוא מתעדכן עם כל נר חדש.',
        keyPoints:['MA = ממוצע מחירי סגירה','MA20 = קצר טווח','MA50 = בינוני טווח','MA200 = ארוך טווח'],
        visual:{ candles:maTrendCandles, overlays:[{type:'maline',values:ma5,color:'#f0883e',label:'MA5'},{type:'maline',values:ma12,color:'#388bfd',label:'MA12'}], caption:'ממוצעים נעים — קצר (כתום) ובינוני (כחול)' }},
      { title:'MA כתמיכה דינמית', body:'הממוצע הנע פועל כתמיכה או התנגדות דינמית — הוא זז עם המחיר.\n\nמגמה עולה: המחיר נוגע ב-MA20 או MA50 ומיד קופץ. זה הזדמנות כניסה קלאסית.\n\nMА200 = "הממוצע המלכותי" — תמיכה/התנגדות קריטית לטווח ארוך.',
        keyPoints:['MA = תמיכה/התנגדות שזזת','נגיעה ב-MA + תבנית = קנייה/מכירה','MA200 = הכי חשוב לטווח ארוך','מחיר מעל MA200 = "שורי" מוסדי'],
        visual:{ candles:maTrendCandles, overlays:[{type:'maline',values:ma12,color:'#388bfd',label:'MA12 (תמיכה)'}], caption:'ה-MA12 פועל כתמיכה — כל נגיעה בו מייצרת הזדמנות' }},
      { title:'Golden Cross ✨', body:'Golden Cross = הממוצע הקצר (MA20/50) חוצה מעל הממוצע הארוך (MA50/200).\n\nנחשב לאחד האותות השוריים החזקים ביותר. מסמן שהמגמה קצרת הטווח חזקה יותר מהארוכה.',
        keyPoints:['MA20 חוצה מעל MA50 = Golden Cross','אות שורי חזק','הכי אמין על גרף יומי ושבועי','מומלץ לאשר עם נפח גבוה'],
        visual:{ candles:goldenCrossCandles, overlays:[{type:'maline',values:ma5gc,color:'#f0883e',label:'MA5 (קצר)'},{type:'maline',values:ma12gc,color:'#388bfd',label:'MA12 (ארוך)'},{type:'arrow',idx:14,text:'Golden Cross!',color:'#f0883e',position:'top'}], caption:'הממוצע הקצר (כתום) חוצה מעל הארוך (כחול) = Golden Cross' }},
      { title:'Death Cross ☠️', body:'Death Cross = הממוצע הקצר חוצה מתחת לממוצע הארוך.\n\nנחשב לאחד האותות הדוביים החזקים ביותר. מסמן שהמגמה קצרת הטווח חלשה מהארוכה.',
        keyPoints:['MA20 חוצה מתחת MA50 = Death Cross','אות דובי חזק','הכי אמין על גרף יומי ושבועי','מציין לעיתים תחילת ירידה ממושכת'],
        visual:{ candles:goldenCrossCandles.slice(0,12).reverse().map((c,i)=>({...c,time:T0+i*D})), overlays:[{type:'arrow',idx:5,text:'Death Cross!',color:'#f85149',position:'top'}], caption:'הממוצע הקצר חוצה מתחת לארוך = Death Cross' }},
      { title:'שימוש מעשי', body:'כלל מסחר פשוט:\n\n• מחיר מעל MA20 + MA20 מעל MA50 + MA50 מעל MA200 = מגמה עולה חזקה מאוד\n\n• ככל שיותר ממוצעים "מסודרים" — הכיוון בטוח יותר.',
        keyPoints:['3 MA מסודרים = מגמה חזקה מאוד','נגיעה ב-MA = הזדמנות, לא אות לבד','שילוב MA + תבנית נרות = עוצמה','MA200 = גבול בין שוק שורי לדובי'],
        visual:{ candles:maTrendCandles, overlays:[{type:'maline',values:ma5,color:'#f0883e',label:'MA5'},{type:'maline',values:ma12,color:'#388bfd',label:'MA12'}], caption:'3 ממוצעים מסודרים = מגמה בריאה וחזקה' }},
    ],
  },
  // ── 7 ──────────────────────────────────────────────────────────────────────
  {
    id:'volume', icon:'📊', title:'נפח מסחר', color:'#388bfd', duration:'6 דקות',
    description:'נפח הוא "מד הכוח" של כל תנועה — ללא נפח, תבנית היא ספקולציה בלבד',
    slides:[
      { title:'מהו נפח מסחר?', body:'נפח (Volume) = כמות היחידות שנסחרו בתקופה. יותר נפח = יותר השתתפות = יותר שכנוע.\n\nכנר גדול עם נפח נמוך = תנועה חשודה. נר גדול עם נפח גבוה = תנועה אמיתית.',
        keyPoints:['נפח גבוה = השוק מסכים עם התנועה','נפח נמוך = תנועה לא משכנעת','בדוק תמיד: האם הנפח מאשר?','ללא נפח — תבנית היא רק ציור'],
        visual:{ candles:volumeConfirmCandles, overlays:[{type:'hline',price:89.5,color:'#d29922',label:'פריצה',dashed:true}], caption:'נרות קטנים לפני הפריצה = נפח נמוך → נרות גדולים = נפח גבוה, פריצה אמיתית' }},
      { title:'נפח + תבנית = אישור', body:'כל תבנית נרות הופכת הרבה יותר אמינה כשמלוות אותה בנפח גבוה.\n\nפטיש בנפח גבוה > פטיש בנפח נמוך. פריצה בנפח גבוה > פריצה בנפח נמוך.\n\nחוק: "אמן בנפח, כישלון ללא נפח".',
        keyPoints:['תבנית + נפח גבוה = אות חזק','פריצה + נפח גבוה = פריצה אמיתית','נפח ממוצע × 1.5+ = "גבוה"','חפש נפח חריג ביום הפריצה'],
        visual:{ candles:volumeConfirmCandles, overlays:[{type:'hline',price:89.5,color:'#3fb950',label:'פריצה בנפח גבוה!',dashed:true}], caption:'פריצה עם נרות גדולים (= נפח גבוה) = אות אמיתי' }},
      { title:'נפח נמוך = לא משכנע', body:'פריצה בנפח נמוך = "פריצה מזויפת" (False Breakout). המחיר עבר רמה אך אין קונים/מוכרים שיתמכו בתנועה.\n\nציפיה: המחיר יחזור לרמה המקורית.',
        keyPoints:['נרות קטנים + פריצה = חשוד','המתן לנפח גבוה לפני כניסה','False Breakout = פח נפוץ מאוד','נפח קטן בפריצה = יציאה מהירה'],
        visual:{ candles:volumeConfirmCandles.slice(0, 9), overlays:[{type:'hline',price:89.5,color:'#f85149',label:'פריצה חשודה בנפח נמוך',dashed:true}], caption:'נרות קטנים (נפח נמוך) = פריצה לא אמינה' }},
      { title:'סטייה בנפח (Divergence)', body:'מחיר עולה אך נפח יורד = אזהרה. הקונים מתייאשים.\n\nנקרא "סטייה בנפח" (Volume Divergence). לרוב מקדים ירידה.\n\nחפש: נרות שנעשים קטנים יותר ויותר אחרי עלייה גדולה.',
        keyPoints:['מחיר עולה + נפח יורד = אזהרה','Divergence = קונים מתייאשים','סימן מוקדם לירידה קרובה','חפש בשיאי מגמה בעיקר'],
        visual:{ candles:volumeDivCandles, overlays:[{type:'arrow',idx:9,text:'ירידה',color:'#f85149',position:'top'}], caption:'הנרות נעשים קטנים יותר בשיא = קונים מאבדים עוצמה' }},
    ],
  },
  // ── 8 ──────────────────────────────────────────────────────────────────────
  {
    id:'head-shoulders', icon:'👤', title:'ראש וכתפיים', color:'#f85149', duration:'8 דקות',
    description:'תבנית ההיפוך הדובי הכי מפורסמת — 3 שיאים, קו צוואר, ויעד מדויק',
    slides:[
      { title:'מהי תבנית ראש וכתפיים?', body:'ראש וכתפיים (Head & Shoulders) היא תבנית היפוך דובי קלאסית. 3 שיאים: כתף שמאל, ראש (השיא הגבוה ביותר), כתף ימין (נמוכה מהראש).\n\nנחשבת לאחת התבניות האמינות ביותר.',
        keyPoints:['3 שיאים: כתף, ראש, כתף','הראש = השיא הגבוה ביותר','כתפיים ≈ שוות גובה (קצת נמוכות)','מסמנת היפוך ממגמה עולה ליורדת'],
        visual:{ candles:hsCandles, overlays:[{type:'hline',price:NECKLINE,color:'#d29922',label:'קו הצוואר',dashed:true}], caption:'3 שיאים ברורים עם קו צוואר ב-86' }},
      { title:'הכתף השמאלית', body:'הכתף השמאלית: עלייה רגילה בתוך מגמה עולה, אחריה ירידה לרמת תמיכה (קו הצוואר).\n\nבשלב זה התבנית אינה ניכרת עדיין — נראה כמו מגמה עולה רגילה עם תיקון.',
        keyPoints:['עלייה לשיא','ירידה לתמיכה (קו הצוואר)','לא ניכרת עדיין כתבנית','נפח בעלייה = רגיל'],
        visual:{ candles:hsCandles.slice(0,7), overlays:[{type:'hline',price:NECKLINE,color:'#d29922',label:'קו הצוואר',dashed:true},{type:'arrow',idx:3,text:'כתף שמאל',color:'#388bfd',position:'top'}], caption:'כתף שמאל — עלייה ותיקון, נראה כמו עלייה רגילה' }},
      { title:'הראש וקו הצוואר', body:'הראש: עלייה חדה מעל הכתף השמאלית (שיא חדש). אחריה ירידה חזרה לרמת הצוואר.\n\nקו הצוואר (Neckline) = הרמה שאליה המחיר חוזר בין הכתפיים לראש. זו רמת הכניסה.',
        keyPoints:['הראש = שיא הגבוה מהכתף','ירידה חזרה לקו הצוואר','הנקודה שמחברת השפלים = קו הצוואר','שבירת קו הצוואר = אות מכירה!'],
        visual:{ candles:hsCandles.slice(0,16), overlays:[{type:'hline',price:NECKLINE,color:'#d29922',label:'קו הצוואר',dashed:true},{type:'arrow',idx:10,text:'ראש',color:'#f85149',position:'top'}], caption:'הראש = שיא חדש, ואז ירידה חזרה לקו הצוואר' }},
      { title:'הכתף הימנית והפריצה', body:'הכתף הימנית: עלייה שלא מגיעה לגובה הראש (חולשה!). ואז ירידה ושבירה מתחת לקו הצוואר.\n\nשבירת הצוואר = אות כניסה קלאסי. כניסה בסגירה מתחת לצוואר.',
        keyPoints:['כתף ימין נמוכה מהראש = חולשה','שבירת קו הצוואר = אות מכירה','כניסה בסגירה מתחת לצוואר','Stop Loss מעל הכתף הימנית'],
        visual:{ candles:hsCandles, overlays:[{type:'hline',price:NECKLINE,color:'#d29922',label:'קו הצוואר',dashed:true},{type:'arrow',idx:21,text:'שבירה!',color:'#f85149',position:'top'}], caption:'כתף ימין נמוכה + שבירת צוואר = אות מכירה חזק' }},
      { title:'יעד המחיר', body:'יעד = מדוד מגובה הראש מעל קו הצוואר, מוחסר מנקודת הפריצה.\n\nדוגמה: ראש ב-98, צוואר ב-86 → מרחק = 12 → יעד = 86 - 12 = 74.',
        keyPoints:['מרחק = ראש - צוואר','יעד = פריצה - מרחק','Stop Loss: מעל הכתף הימנית','יחס סיכוי/סיכון טוב ב-3:1'],
        visual:{ candles:[...hsCandles,...hsReveal], overlays:[{type:'hline',price:NECKLINE,color:'#d29922',label:'קו הצוואר 86',dashed:true},{type:'hline',price:74,color:'#f85149',label:'יעד 74',dashed:true}], caption:'יעד = צוואר 86 פחות מרחק ראש-צוואר (12) = 74' }},
    ],
  },
  // ── 9 ──────────────────────────────────────────────────────────────────────
  {
    id:'double-patterns', icon:'⚖️', title:'שיא כפול ותחתית כפולה', color:'#3fb950', duration:'7 דקות',
    description:'"M" ו-"W" — שתי תבניות היפוך ברורות ביותר, עם יעדים מדויקים',
    slides:[
      { title:'שיא כפול — צורת M', body:'שיא כפול (Double Top) = שני שיאים באותה רמה, אחריהם ירידה.\n\nדמיין "M": הקונים ניסו פעמיים לפרוץ אותה רמה ונכשלו. מסמן היפוך מגמה עולה → יורדת.',
        keyPoints:['שני שיאים באותה רמה (≈ שוים)','ביניהם: ירידה לרמת "צוואר"','כישלון שני = חולשת קונים','שבירת הצוואר = אות מכירה'],
        visual:{ candles:doubleTopCandles, overlays:[{type:'hline',price:95,color:'#f85149',label:'התנגדות (שני שיאים)',dashed:true},{type:'hline',price:DTOP_NECK,color:'#d29922',label:'צוואר (נקודת כניסה)',dashed:true}], caption:'שני שיאים זהים + שבירת צוואר = שיא כפול' }},
      { title:'תחתית כפולה — צורת W', body:'תחתית כפולה (Double Bottom) = שתי תחתיות באותה רמה, אחריהן עלייה.\n\nדמיין "W": המוכרים ניסו פעמיים לדחוף מטה ונכשלו. מסמן היפוך מגמה יורדת → עולה.',
        keyPoints:['שתי תחתיות באותה רמה','ביניהן: עלייה לרמת "צוואר"','כישלון שני = חולשת מוכרים','שבירת הצוואר מעלה = אות קנייה'],
        visual:{ candles:doubleBottomCandles, overlays:[{type:'hline',price:84,color:'#3fb950',label:'תמיכה (שתי תחתיות)',dashed:true},{type:'hline',price:DBOT_NECK,color:'#d29922',label:'צוואר (נקודת כניסה)',dashed:true}], caption:'שתי תחתיות זהות + שבירת צוואר = תחתית כפולה' }},
      { title:'קו הצוואר — אישור חובה', body:'ללא שבירת קו הצוואר — אין תבנית.\n\nהכלל: אל תיכנס לפני השבירה. הרבה סוחרים ממהרים ומאבדים כסף על "תבניות" שלא אושרו.\n\nהמתן לסגירה יומית ברורה מעבר לצוואר.',
        keyPoints:['שבירה + סגירה ברורה = אישור','אל תיכנס לפני השבירה','נפח גבוה בשבירה = אות חזק יותר','Pullback לצוואר = הזדמנות שנייה'],
        visual:{ candles:doubleTopCandles, overlays:[{type:'hline',price:DTOP_NECK,color:'#d29922',label:'קו הצוואר — חכה לשבירה!',dashed:true}], caption:'ממתינים לסגירה מתחת לקו הצוואר — לא רגע לפני' }},
      { title:'יעד המחיר', body:'יעד = המרחק בין קו הצוואר לשיאים/תחתיות, מוחסר מנקודת הפריצה.\n\nשיא כפול: יעד = צוואר - (שיא - צוואר)\nתחתית כפולה: יעד = צוואר + (צוואר - תחתית)',
        keyPoints:['מרחק = |שיא - צוואר|','שיא כפול: יעד = צוואר - מרחק','תחתית כפולה: יעד = צוואר + מרחק','Stop Loss מעל השיא/מתחת לתחתית'],
        visual:{ candles:doubleBottomCandles, overlays:[{type:'hline',price:84,color:'#3fb950',label:'תחתית 84',dashed:true},{type:'hline',price:DBOT_NECK,color:'#d29922',label:'צוואר 92',dashed:true},{type:'hline',price:100,color:'#3fb950',label:'יעד 100',dashed:true}], caption:'יעד = צוואר 92 + מרחק 8 = 100' }},
    ],
  },
  // ── 10 ─────────────────────────────────────────────────────────────────────
  {
    id:'flags-pennants', icon:'🚩', title:'דגלים ודגלונים', color:'#388bfd', duration:'6 דקות',
    description:'תבניות המשך — אחרי תנועה חזקה, הדגל מסמן שהתנועה תמשיך',
    slides:[
      { title:'מהו דגל?', body:'דגל (Flag) הוא תבנית המשך — לא היפוך. מופיעה אחרי תנועה חזקה (עמוד הדגל) ומסמנת שהמהלך יימשך.\n\nמחזה: ריצה חזקה, הפסקה קצרה (הדגל), ואז המשך בכיוון.',
        keyPoints:['תבנית המשך (לא היפוך)!','עמוד הדגל = התנועה החזקה','הדגל = איחוד/תיקון קצר','פריצה = המשך בכיוון עמוד הדגל'],
        visual:{ candles:flagCandles, overlays:[{type:'phase',fromIdx:0,toIdx:6,color:'#3fb950',label:'עמוד'},{type:'phase',fromIdx:6,toIdx:12,color:'#d29922',label:'דגל'},{type:'phase',fromIdx:12,toIdx:17,color:'#3fb950',label:'פריצה'},{type:'hline',price:96,color:'#d29922',label:'רמת הדגל',dashed:true}], caption:'עמוד (ירוק) → דגל (זהב) → פריצה (ירוק)' }},
      { title:'עמוד הדגל (Flagpole)', body:'עמוד הדגל = התנועה החדה שלפני הדגל. ככל שהוא חזק וחד יותר — הדגל אמין יותר.\n\nנפח גבוה בעמוד = הרבה קונים/מוכרים. נפח נמוך בדגל = איחוד בריא.',
        keyPoints:['עמוד חזק + נפח גבוה = אמין','מינימום 5-8 נרות לעמוד','נפח בדגל אמור לרדת','עמוד ארוך = יעד גדול יותר'],
        visual:{ candles:flagCandles.slice(0,7), overlays:[], caption:'עמוד הדגל — תנועה חדה בנפח גבוה (נרות גדולים)' }},
      { title:'הדגל (Flag)', body:'הדגל = איחוד קצר שמשתלב קצת נגד הכיוון. כאנל מטה בדגל שורי (קצת יורד) — זה תקין ובריא.\n\nנפח בדגל = נמוך. הדגל "מנוח" את השוק לפני ריצה נוספת.',
        keyPoints:['איחוד 3-15 נרות בדרך כלל','נטייה קלה נגד הכיוון = תקין','נפח נמוך = המוכרים חלשים','כניסה בפריצה מעל/מתחת לדגל'],
        visual:{ candles:flagCandles.slice(5,13), overlays:[{type:'trendline',fromIdx:0,fromPrice:96,toIdx:7,toPrice:91,color:'#f85149',dashed:true},{type:'trendline',fromIdx:0,fromPrice:94,toIdx:7,toPrice:89,color:'#f85149',dashed:true}], caption:'הדגל — ערוץ יורד קל עם נרות קטנים (נפח נמוך)' }},
      { title:'יעד הדגל', body:'יעד = ממדוד גובה עמוד הדגל ומוסיף מנקודת הפריצה.\n\nדוגמה: עמוד 18 נקודות (78→96). פריצה מ-96. יעד = 96 + 18 = 114.\n\nStop Loss = מתחת לתחתית הדגל.',
        keyPoints:['יעד = גובה עמוד + נקודת פריצה','Stop Loss מתחת לתחתית הדגל','אל תרדוף — קנה רק בפריצה','דגל שבועי = יעד ענקי'],
        visual:{ candles:flagCandles, overlays:[{type:'hline',price:96,color:'#d29922',label:'נקודת פריצה',dashed:true},{type:'hline',price:114,color:'#3fb950',label:'יעד (96+18)',dashed:true}], caption:'יעד = עמוד (18 נקודות) + נקודת פריצה (96) = 114' }},
    ],
  },
  // ── 11 ─────────────────────────────────────────────────────────────────────
  {
    id:'triangles', icon:'📐', title:'משולשים', color:'#d29922', duration:'7 דקות',
    description:'3 סוגי משולשים — אסצנדינג, דסצנדינג וסימטרי — ואיך לסחור אותם',
    slides:[
      { title:'משולש עולה (Ascending Triangle)', body:'המשולש העולה = התנגדות שטוחה + תמיכה עולה. המחיר דוחס בין שתי הרמות ולבסוף פורץ מעלה.\n\nזהו תבנית המשך שורי (לרוב).',
        keyPoints:['התנגדות שטוחה בחלק העליון','תמיכה עולה (שפלים עולים)','הפריצה בדרך כלל כלפי מעלה','ממתינים לפריצה — לא מנחשים'],
        visual:{ candles:ascTriangleCandles, overlays:[{type:'hline',price:95,color:'#f85149',label:'התנגדות שטוחה',dashed:false},{type:'trendline',fromIdx:7,fromPrice:88.5,toIdx:14,toPrice:90,color:'#3fb950'},{type:'arrow',idx:17,text:'פריצה!',color:'#3fb950',position:'top'}], caption:'התנגדות שטוחה (אדום) + תמיכה עולה (ירוק) = לחץ לפריצה' }},
      { title:'משולש יורד (Descending Triangle)', body:'המשולש היורד = תמיכה שטוחה + התנגדות יורדת. המחיר לחוץ ולרוב פורץ כלפי מטה.\n\nתבנית המשך דובי (לרוב).',
        keyPoints:['תמיכה שטוחה בחלק התחתון','התנגדות יורדת (שיאים יורדים)','הפריצה בדרך כלל כלפי מטה','ממתינים לפריצה — לא מנחשים'],
        visual:{ candles:descTriangleCandles, overlays:[{type:'hline',price:85,color:'#3fb950',label:'תמיכה שטוחה',dashed:false},{type:'trendline',fromIdx:1,fromPrice:91,toIdx:9,toPrice:88,color:'#f85149'},{type:'arrow',idx:13,text:'פריצה!',color:'#f85149',position:'top'}], caption:'תמיכה שטוחה (ירוק) + התנגדות יורדת (אדום) = לחץ לפריצה מטה' }},
      { title:'משולש סימטרי', body:'שני קווי מגמה מתכנסים: שיאים יורדים + שפלים עולים. פריצה יכולה להיות בכל כיוון — ממתינים.\n\nלרוב מסמן תבנית המשך (בכיוון המגמה הקודמת).',
        keyPoints:['שיאים יורדים + שפלים עולים','פריצה בכל כיוון אפשרית','עקוב אחרי כיוון המגמה לפני','נפח גבוה בפריצה = אמינות'],
        visual:{ candles:ascTriangleCandles.slice(0,15), overlays:[{type:'trendline',fromIdx:4,fromPrice:95,toIdx:12,toPrice:95.2,color:'#f85149',dashed:true},{type:'trendline',fromIdx:7,fromPrice:88.5,toIdx:14,toPrice:90,color:'#3fb950',dashed:true}], caption:'משולש סימטרי — קווים מתכנסים משני הצדדים' }},
      { title:'כיצד לסחור משולשים', body:'כלל מספר 1: לעולם אל תנחש את כיוון הפריצה. תמיד המתן.\n\nאסטרטגיה:\n1. מקם הוראת קנייה מעל ההתנגדות\n2. מקם הוראת מכירה מתחת לתמיכה\n3. מה שיפרוץ ראשון — שם תיכנס',
        keyPoints:['לעולם לא לנחש את הפריצה!','Stop Loss: מעבר לצד השני של הפריצה','יעד = גובה בסיס המשולש + נקודת פריצה','אמן: נפח גבוה בפריצה'],
        visual:{ candles:ascTriangleCandles, overlays:[{type:'hline',price:95,color:'#f85149',label:'פריצה מעלה: קנה כאן',dashed:true},{type:'hline',price:88,color:'#3fb950',label:'פריצה מטה: מכור כאן',dashed:true}], caption:'הכן שתי הוראות — ותיכנס לזו שתופעל ראשון' }},
    ],
  },
  // ── 12 ─────────────────────────────────────────────────────────────────────
  {
    id:'fibonacci', icon:'🌀', title:"רמות פיבונאצ'י", color:'#a371f7', duration:'7 דקות',
    description:"יחס הזהב בשווקים — איך לצייר ולהשתמש ברמות 38.2%, 50%, 61.8%",
    slides:[
      { title:"מהן רמות פיבונאצ'י?", body:"פיבונאצ'י הוא סדרת מספרים מהמאה ה-13 שמופיעה בטבע (צדפות, עץ, גלקסיות). שווקים, שמורכבים מהתנהגות אנושית, גם מכבדים יחסים אלה.\n\nרמות תיקון: 23.6%, 38.2%, 50%, 61.8%.",
        keyPoints:["23.6% = תיקון קטן (מגמה חזקה מאוד)","38.2% = תיקון בינוני","50% = תיקון חצי (לא פיבונאצ'י, אך נפוץ)","61.8% = 'יחס הזהב' — החשוב ביותר"],
        visual:{ candles:fibCandles, overlays:[{type:'hline',price:fib236,color:'#6e7681',label:`23.6% — ${fib236}`,dashed:true},{type:'hline',price:fib382,color:'#a371f7',label:`38.2% — ${fib382}`,dashed:true},{type:'hline',price:fib50,color:'#d29922',label:`50% — ${fib50}`,dashed:true},{type:'hline',price:fib618,color:'#f85149',label:`61.8% — ${fib618} ★`,dashed:false}], caption:"רמות פיבונאצ'י על תיקון מ-100 ל-80" }},
      { title:"איך מציירים?", body:"1. מצא swing high (שיא) ו-swing low (שפל) משמעותיים.\n2. בגרף עולה: מושך מהשפל לשיא.\n3. הכלי מצייר אוטומטית את הרמות ביניהם.\n\nכלל: ככל שה-swing גדול יותר — הרמות יותר אמינות.",
        keyPoints:["גרף עולה: מהשפל לשיא","גרף יורד: מהשיא לשפל","השתמש בנקודות swing בולטות","רמות על TF גבוה = יותר חזקות"],
        visual:{ candles:fibCandles.slice(0,7), overlays:[{type:'hline',price:80,color:'#3fb950',label:'Swing Low (80)'},{type:'hline',price:100,color:'#f85149',label:'Swing High (100)'}], caption:"מצא swing high ו-swing low — ומשוך ביניהם" }},
      { title:"61.8% — יחס הזהב", body:"רמת 61.8% נקראת 'יחס הזהב' (Golden Ratio). היא רמת התמיכה/התנגדות החזקה ביותר.\n\nהמחיר מגיע ל-61.8%, מתאחד שם, ואז לעיתים קרובות ממשיך בכיוון המקורי.",
        keyPoints:["61.8% = הרמה הכי חשובה","הרבה היפוכים מתרחשים כאן","שילוב 61.8% + תבנית נרות = עוצמה","מכונה 'הזהב' של הניתוח הטכני"],
        visual:{ candles:fibCandles, overlays:[{type:'hline',price:fib618,color:'#f85149',label:`61.8% = ${fib618} ★★★`,dashed:false},{type:'zone',priceHigh:fib618+0.8,priceLow:fib618-0.8,color:'#f85149'},{type:'arrow',idx:13,text:'הקפצה!',color:'#3fb950',position:'top'}], caption:"המחיר מגיע ל-61.8% ומקפץ — יחס הזהב בפעולה" }},
      { title:"שילוב עם תמיכה/התנגדות", body:"הכוח האמיתי: כאשר רמת פיבונאצ'י חופפת לרמת תמיכה/התנגדות קיימת — 'אשכול' (Cluster).\n\nאשכול = מספר סיבות לאותה רמה = הרבה יותר אמין.",
        keyPoints:["פיבונאצ'י + תמיכה = אשכול חזק","פיבונאצ'י + MA200 = אות חזק מאוד","חפש כפל/שלוש גורמים באותה רמה","ככל שיותר גורמים — הרמה חזקה יותר"],
        visual:{ candles:fibCandles, overlays:[{type:'hline',price:fib618,color:'#a371f7',label:`Fib 61.8% = ${fib618}`,dashed:false},{type:'hline',price:87.5,color:'#3fb950',label:'תמיכה קלאסית 87.5',dashed:true}], caption:"פיבונאצ'י + תמיכה קיימת = 'אשכול' — רמה חזקה במיוחד" }},
    ],
  },
  // ── 13 ─────────────────────────────────────────────────────────────────────
  {
    id:'market-psychology', icon:'🧠', title:'פסיכולוגיית שוק', color:'#8b5cf6', duration:'8 דקות',
    description:'למה אנשים מפסידים — מחזור הרגשות, FOMO, ניהול עצמי בשוק',
    slides:[
      { title:'מחזור הרגשות', body:'כל שוק עובר אותו מחזור רגשי: צבירה → תקווה → תאוות בצע (FOMO) → שיא → פחד → פאניקה → יאוש → חזרה.\n\nהדרך לנצח: לאתר היכן המחזור נמצא ולפעול נגד הרגש.',
        keyPoints:['צבירה: הכסף החכם קונה בשקט','FOMO: כסף "טיפש" קונה בשיא','פחד: כסף "טיפש" מוכר בתחתית','כסף חכם = קונה כשיש דם ברחובות'],
        visual:{ candles:psychoCandles, overlays:[{type:'phase',fromIdx:0,toIdx:5,color:'#388bfd',label:'צבירה'},{type:'phase',fromIdx:5,toIdx:8,color:'#3fb950',label:'תקווה'},{type:'phase',fromIdx:8,toIdx:13,color:'#d29922',label:'FOMO'},{type:'phase',fromIdx:13,toIdx:17,color:'#f85149',label:'פחד'},{type:'phase',fromIdx:17,toIdx:20,color:'#6e7681',label:'יאוש'}], caption:'מחזור הרגשות — כל שוק עובר את אותן תחנות' }},
      { title:'FOMO — האויב הגדול', body:'FOMO (Fear of Missing Out) = פחד לפספס את ההזדמנות.\n\nכאשר כולם מדברים על מניה, הטלוויזיה מכריזה, החברים מתעשרים — הקנייה המסוכנת ביותר.\n\nסטטיסטיקה: 80% מהרי FOMO מגיעים בשיא.',
        keyPoints:['FOMO = קנייה מהפחד, לא מהניתוח','"אם כולם מדברים עליה — מאוחר מדי"','ממשמעת לנקודות כניסה מתוכננות','תוכנית + ניתוח > רגש + אינסטינקט'],
        visual:{ candles:psychoCandles.slice(7,15), overlays:[{type:'arrow',idx:5,text:'כאן קונה FOMO',color:'#f85149',position:'top'},{type:'arrow',idx:7,text:'כאן ירידה',color:'#6e7681',position:'top'}], caption:"FOMO קונה בשיא — בדיוק כשהכסף החכם מוכר" }},
      { title:'למה תבניות עובדות?', body:'תבניות נרות עובדות כי הן מייצגות התנהגות אנושית קבועה.\n\nפטיש: מוכרים ניסו, קונים גברו — תמיד אותה דינמיקה.\nבליעה: שינוי שליטה מהיר — תמיד מייצר תגובה.\n\nהשוק = מיליוני אנשים עם פסיכולוגיה דומה.',
        keyPoints:['תבניות = דפוסי התנהגות קבועים','פסיכולוגיה אנושית אינה משתנה','אותן תבניות עבדו לפני 100 שנה','ועוד יעבדו עוד 100 שנה'],
        visual:{ candles:psychoCandles, overlays:[], caption:"השוק = מיליוני אנשים עם אותה פסיכולוגיה" }},
      { title:'כללים לניהול עצמי', body:'10 כללי ברזל:\n\n1. תמיד הגדר Stop Loss לפני הכניסה\n2. אל תסחר בכסף שאתה לא יכול להפסיד\n3. 1-2% מהחשבון לסיכון לעסקה\n4. אל תנקום בשוק (Revenge Trading)\n5. תוכנית > רגע',
        keyPoints:['Stop Loss = חובה, לא אפשרות','מקסימום 2% מהחשבון לעסקה','Revenge Trading = אבדון מהיר','ממשמעת + סבלנות > חכמה'],
        visual:{ candles:psychoCandles, overlays:[{type:'hline',price:80,color:'#3fb950',label:'קנה כאן (תוכנית)!'},{type:'hline',price:103,color:'#f85149',label:'כולם קונים כאן (FOMO)',dashed:true}], caption:"תוכנית לפני הרגש: קנה בתחתית, לא בשיא" }},
    ],
  },

  // ═══ LESSON 14: SMC - מבנה שוק חכם ═══════════════════════════════════════════
  {
    id: 'smc-structure', icon: '📐', title: 'SMC — מבנה שוק חכם', color: '#9333ea',
    duration: '10 דקות', description: 'הבנת מבנה השוק — איך הכסף החכם משאיר רמזים',
    slides: [
      { title:'מה זה SMC?', body:'SMC = Smart Money Concept\nשיטה המתמקדת בהבנת תנועות הכסף הגדול בשוק.\n\nהכסף החכם (בנקים, קרנות גדולות) משאיר ״עקבות״ בגרף.\nעקבות אלו = הזדמנויות לסוחרים קטנים.',
        keyPoints:['SMC = עקבות של הכסף הגדול','השוק בנוי על מבנה ספציפי','אותו מבנה חוזר שוב ושוב','כלים קטנים יכולים ללכוד אותו'],
        visual:{ candles:buildCandles([{o:100,c:105},{o:105,c:108},{o:108,c:103,wd:2},{o:103,c:107,wu:1.5},{o:107,c:110},{o:110,c:106,wd:1.8}]), overlays:[{type:'zone',fromIdx:2,toIdx:3,color:'#9333ea',label:'מבנה SMC'},{type:'arrow',idx:3,text:'כאן הכסף הגדול נכנס',color:'#9333ea',position:'bottom'}], caption:'SMC מזהה נקודות שבהן הכסף הגדול פוגע' }},
      { title:'Order Blocks', body:'Order Block = רמה שבה נוצר ״רעש״ גדול בגרף.\n\nהכסף החכם מוציא הזמנות בסדר גדול — מה שיוצר תנודה חזקה.\n\nלאחר זה, המחיר חוזר לאותה רמה כדי לאסוף הזמנות נוספות.',
        keyPoints:['Order Block = רמת Liquidity','המחיר חוזר לrob liquidity','זה קורה מדי פעם בנוסחה','יוצר הזדמנויות חוזרות'],
        visual:{ candles:buildCandles([{o:100,c:105},{o:105,c:108},{o:108,c:104,wd:3},{o:104,c:102},{o:102,c:98},{o:98,c:105,wu:2.5}]), overlays:[{type:'zone',fromIdx:1,toIdx:2,color:'#9333ea',label:'Order Block'},{type:'hline',price:104,color:'#f85149',label:'מחיר חוזר לrob',dashed:true}], caption:'כסף גדול משאיר "עדויות" בגרף' }},
      { title:'Fair Value Gap (FVG)', body:'FVG = פער במחיר שלא נטופל.\n\nנניח המחיר יצא מ-100 ל-105 בקפיצה חדה.\nפער זה (100-105) נשאר ״פתוח״.\n\nהמחיר בדרך כלל חוזר למלא פערים אלו.',
        keyPoints:['FVG = פערים לא מטופלים','המחיר "זוכר" פערים','פערים = אזורי ספיגה','אפשר לסחור על ״מילוי״ פערים'],
        visual:{ candles:buildCandles([{o:100,c:100},{o:100,c:105,wu:2.5},{o:105,c:107},{o:107,c:104},{o:104,c:100,wd:3.5}]), overlays:[{type:'zone',fromIdx:0,toIdx:1,color:'#3fb950',label:'FVG - פער שלא מטופל'},{type:'arrow',idx:4,text:'חזרה למלא את הפער',color:'#3fb950',position:'bottom'}], caption:'המחיר חוזר למלא את הפערים' }},
      { title:'Liquidity Levels', body:'Liquidity = נקודות בעלות "נזילות" גבוהה.\n\nנקודות כמו Support/Resistance, High/Low החזקות — כלקות שבהן מרבצים Stop Losses של סוחרים.\n\nהכסף החכם מטרף אותן נקודות כדי לספוג הזמנות.',
        keyPoints:['Liquidity = אזורי ריכוז הזמנות','High/Low חדשים = liquidity levels','כסף גדול "קולע" לlevels אלו','סוחרים קטנים עולים במלכודת'],
        visual:{ candles:buildCandles([{o:100,c:105},{o:105,c:110,wu:1.5},{o:110,c:108},{o:108,c:112,wu:2.2},{o:112,c:107,wd:3.5},{o:107,c:110}]), overlays:[{type:'hline',price:110,color:'#d29922',label:'Liquidity Level'},{type:'arrow',idx:1,text:'כסף גדול פוגע',color:'#d29922',position:'top'}], caption:'liquidity levels = מלכודות של כסף חכם' }},
      { title:'איך להשתמש בSMC?', body:'3 צעדים:\n\n1. מצא Order Blocks ו-FVGs\n2. חכה שהמחיר יחזור לאזור\n3. כנס כשהוא משחזר את ה-Liquidity\n\nRisk: הכסף החכם עלול לשנות כיוון בכל רגע!',
        keyPoints:['SMC = כלי חזק אך לא יחיד','צריך Stop Loss תמיד','צירוף עם תבניות אחרות = עוצמה','ממשמעת בהמתנה = מפתח'],
        visual:{ candles:buildCandles([{o:100,c:105},{o:105,c:108},{o:108,c:103,wd:2.5},{o:103,c:107,wu:1.5},{o:107,c:110},{o:110,c:105,wd:2},{o:105,c:109,wu:2}]), overlays:[{type:'zone',fromIdx:1,toIdx:2,color:'#9333ea',label:'Order Block'},{type:'arrow',idx:4,text:'Liquidity מספיגה',color:'#3fb950',position:'bottom'},{type:'hline',price:103.5,color:'#f85149',label:'Stop Loss תמיד'}], caption:'SMC בפעולה: התאמה + ממשמעת = הצלחה' }},
    ],
  },

  // ═══ LESSON 15: SMC - Breaker Blocks ═══════════════════════════════════════════
  {
    id: 'smc-breaker', icon: '🔨', title: 'SMC — Breaker Blocks', color: '#9333ea',
    duration: '8 דקות', description: 'זיהוי נקודות שינוי כיוון דרך Breaker Blocks',
    slides: [
      { title:'מה זה Breaker Block?', body:'Breaker Block = Order Block שכישל.\n\nהכסף החכם התחיל לרוב, אך לא הצליח.\nהמחיר פרץ דרך הרמה.\n\nכעת הרמה הופכת ללמש חדשה בכיוון הנמוך.',
        keyPoints:['Breaker = Order Block שלא עמד','הרמה הופכת ללמש הפוכה','זה אות שינוי כיוון חזק','יוצר הזדמנויות גדולות'],
        visual:{ candles:buildCandles([{o:100,c:105},{o:105,c:108,wu:1.5},{o:108,c:106},{o:106,c:103,wd:2.5},{o:103,c:100},{o:100,c:97}]), overlays:[{type:'zone',fromIdx:1,toIdx:2,color:'#f85149',label:'Order Block — כישל'},{type:'hline',price:108,color:'#3fb950',label:'Breaker Level'},{type:'arrow',idx:5,text:'פריצה למטה',color:'#f85149',position:'bottom'}], caption:'כאשר Order Block נכשל — הופך ל-Breaker' }},
      { title:'זיהוי Breaker', body:'4 סימנים ל-Breaker:\n\n1. Order Block ברור בגרף\n2. פריצה חדה דרך הרמה\n3. המחיר לא חוזר מעל הרמה\n4. יש momentum בכיוון המתנגד\n\nכש-4 סימנים מוכחים = Breaker טבעי.',
        keyPoints:['Breaker = כישלון של קנייה/מכירה','פריצה חדה + no retrace = Breaker','Breaker Level = resistance הפוכה','סוחרים קטנים נתפסים בו'],
        visual:{ candles:buildCandles([{o:100,c:105},{o:105,c:108},{o:108,c:106},{o:106,c:103},{o:103,c:100},{o:100,c:97},{o:97,c:95}]), overlays:[{type:'hline',price:108,color:'#d29922',label:'Breaker Level'},{type:'zone',fromIdx:3,toIdx:5,color:'#f85149',label:'כיוון הפריצה'},{type:'arrow',idx:6,text:'התחזוקה של הBreaker',color:'#3fb950',position:'bottom'}], caption:'Breaker נשמר — האות חזקה' }},
      { title:'סחר על Breaker', body:'אסטרטגיה קלאסית:\n\n1. חכה ל-Breaker ברור בגרף\n2. כנס כשהמחיר חוזר לBreaker Level\n3. Stop Loss מעל ה-Breaker\n4. Target = הנמך הבא של Order Block\n\nRisk/Reward: בדרך כלל 1:2 ויותר.',
        keyPoints:['Breaker = ליווי חזק','חזרה ל-Breaker = מחזור טבעי','Risk/Reward טוב = key להצלחה','סבלנות בהמתנה ללכידה'],
        visual:{ candles:buildCandles([{o:100,c:105},{o:105,c:108},{o:108,c:103},{o:103,c:99},{o:99,c:96},{o:96,c:102,wu:2},{o:102,c:97}]), overlays:[{type:'hline',price:108,color:'#d29922',label:'Breaker Level'},{type:'arrow',idx:5,text:'כניסה בחזרה',color:'#3fb950',position:'top'},{type:'hline',price:109,color:'#f85149',label:'Stop Loss',dashed:true}], caption:'סחר על Breaker עם Risk Management חזק' }},
      { title:'טעויות נפוצות', body:'❌ טעות 1: סחר כל Breaker\n➜ רק Breaker עם momentum חזק!\n\n❌ טעות 2: Stop Loss גבוה מדי\n➜ יפגע בכל retrace קטן\n\n❌ טעות 3: חוסר ממשמעת\n➜ כול Breaker לא מתפתח לטרנד',
        keyPoints:['לא כל פריצה = Breaker אמיתי','Momentum + Structure = מפתח','Stop Loss הדוק = חיוני','סלקטיביות > נפח עסקות'],
        visual:{ candles:buildCandles([{o:100,c:105},{o:105,c:108},{o:108,c:104},{o:104,c:101},{o:101,c:99},{o:99,c:98},{o:98,c:100,wu:1.5}]), overlays:[{type:'hline',price:108,color:'#f85149',label:'כאן לא היה Breaker חזק'},{type:'zone',fromIdx:5,toIdx:6,color:'#6e7681',label:'חוסר momentum = בדויה'}], caption:'Breaker חלשה — לא סחור!' }},
      { title:'Breaker בעולם האמיתי', body:'Breaker Blocks עובדים כי:\n\n1. סוחרים קטנים מנחשים Stop Loss מעל High\n2. כסף גדול דוקר את ה-Stop Loss\n3. סוחרים קטנים נכנסים באבדון\n4. כסף גדול סוחר בכיוון הנכון\n\nהפסיכולוגיה אנושית חוזרת שוב ושוב.',
        keyPoints:['Breaker = טרנד טבעי','סוחרים קטנים מציבים Stop Losses דומים','כסף גדול דוקר אותם','מחזור זה חוזר בכל שוק'],
        visual:{ candles:buildCandles([{o:100,c:105},{o:105,c:110},{o:110,c:107},{o:107,c:102},{o:102,c:98},{o:98,c:95},{o:95,c:100,wu:2.5}]), overlays:[{type:'zone',fromIdx:1,toIdx:2,color:'#9333ea',label:'סוחרים קטנים → Stop Loss'},{type:'arrow',idx:3,text:'דיקור Stop Loss',color:'#f85149',position:'top'},{type:'arrow',idx:6,text:'כסף גדול + מומנטום',color:'#3fb950',position:'bottom'}], caption:'Breaker פסיכולוגיה — אותה תבנית כל פעם' }},
    ],
  },

  // ═══ LESSON 16: SMC - Internal Liquidity ═══════════════════════════════════════
  {
    id: 'smc-internal', icon: '💧', title: 'SMC — Internal Liquidity', color: '#9333ea',
    duration: '7 דקות', description: 'זיהוי רמות שמטופלות פנימית בתנודה',
    slides: [
      { title:'מה זה Internal Liquidity?', body:'Internal Liquidity = רמה שנטופלת בתוך תנודה אחת.\n\nדוגמה:\nהמחיר עולה מ-100 ל-115.\nדרך הדרך, הוא יוצר High בנתיים (בחציון) של 110.\n\nרמה זו של 110 = Internal Liquidity.',
        keyPoints:['Internal = בתוך תנודה','High/Low בחציון = Liquidity','מזהה מציא רמות חדשות','כסף חכם טרף אותן מדי פעם'],
        visual:{ candles:buildCandles([{o:100,c:102},{o:102,c:105},{o:105,c:108},{o:108,c:110,wu:1.8},{o:110,c:107},{o:107,c:111},{o:111,c:113},{o:113,c:115}]), overlays:[{type:'hline',price:110,color:'#9333ea',label:'Internal Liquidity High'},{type:'arrow',idx:3,text:'Internal High',color:'#9333ea',position:'top'}], caption:'Internal Liquidity בתוך עלייה' }},
      { title:'משמעות ההתנגדות הפנימית', body:'כאשר המחיר חוזר ל-Internal Liquidity:\n\n1. אם ספוג — סוחרים קטנים הוצאו\n2. אם פריצה — momentum חזק\n3. אם retrace — הסוד הבא מתחיל\n\nInternal Liquidity = נקודת חידוש.',
        keyPoints:['Internal = התאמה בתנודה','חזרה ל-Internal = בדיקת סיעור','רמה זו מוגנת יותר משאר','סוחרים קטנים לא מצפים לה'],
        visual:{ candles:buildCandles([{o:100,c:108},{o:108,c:112,wu:2},{o:112,c:109},{o:109,c:115,wu:1.5},{o:115,c:110,wd:2.5},{o:110,c:113}]), overlays:[{type:'hline',price:112,color:'#9333ea',label:'Internal Liquidity'},{type:'arrow',idx:3,text:'חזרה לInternal',color:'#3fb950',position:'bottom'},{type:'arrow',idx:5,text:'עלייה חדשה',color:'#3fb950',position:'top'}], caption:'Internal Liquidity = מחזור תוך-תנודה' }},
      { title:'סחר על Internal Liquidity', body:'הגישה:\n\n1. מצא תנודה ברורה (up או down)\n2. זהה את ה-High או Low בחציון\n3. כשהמחיר חוזר — רמה זו משמשת כמגנט\n4. כנס כשהוא מגע ה-Internal Level\n\nעוצמה: High/Low פנימיות עוצמתיות יותר.',
        keyPoints:['Internal = רמה מסתתרת','סוחרים רבים לא רואים אותה','נמשמעת → בדיקה של רמה זו','ממשמעת = מפתח!'],
        visual:{ candles:buildCandles([{o:100,c:108},{o:108,c:115,wu:2.5},{o:115,c:110},{o:110,c:105},{o:105,c:110,wu:1.5},{o:110,c:114}]), overlays:[{type:'hline',price:115,color:'#f85149',label:'High החיצוני'},{type:'hline',price:112,color:'#9333ea',label:'Internal High'},{type:'arrow',idx:4,text:'בדיקה של Internal',color:'#3fb950',position:'bottom'}], caption:'Internal Liquidity בדיקה טבעית' }},
      { title:'איך זה עובד בשוק?', body:'סוחרים קטנים שמים Stop Loss על High הראשי (115).\n\nכסף גדול יודע זאת.\nהוא מטרף את ה-Internal High (112) תחילה.\nסוחרים קטנים לא רואים, ממשיכים.\nכסף גדול נכנס ברציניות בכיוון החדש.',
        keyPoints:['Liquidity Pyramids = יירוטים בשכבות','סוחרים קטנים חושבים בHigh אחד','כסף גדול חושב בשכבות','המשחק = יירוט שכבתי'],
        visual:{ candles:buildCandles([{o:100,c:110},{o:110,c:115,wu:2},{o:115,c:108},{o:108,c:105},{o:105,c:110},{o:110,c:118,wu:2.5}]), overlays:[{type:'hline',price:115,color:'#f85149',label:'High ראשי — Stop Loss שם'},{type:'hline',price:113,color:'#9333ea',label:'Internal High — יירוט'},{type:'arrow',idx:5,text:'טרנד חדש בכיוון אחר',color:'#3fb950',position:'top'}], caption:'יירוטי כסף חכם בשכבות' }},
      { title:'Internal Liquidity בפרקטיקה', body:'כדי לזהות Internal Liquidity:\n\n1. צפה בתנודה בעלת טווח גדול\n2. מצא High/Low משניים בדרך\n3. תייג אותן (אל תעשה עסקה בכל)\n4. חכה שהמחיר יחזור\n5. כנס עם Risk Management חזק\n\nזה דורש תרגול ותבונה חזקה.',
        keyPoints:['Internal = דקיקות בנתוח','לא כל High/Low משני = Internal','רק High/Low עם נפח → Internal','סבלנות + Discipline = הצלחה'],
        visual:{ candles:buildCandles([{o:100,c:110},{o:110,c:115},{o:115,c:105},{o:105,c:112,wu:1.8},{o:112,c:108},{o:108,c:114},{o:114,c:120,wu:2}]), overlays:[{type:'hline',price:115,color:'#d29922',label:'High חיצוני'},{type:'hline',price:112,color:'#9333ea',label:'Internal High'},{type:'hline',price:105,color:'#9333ea',label:'Internal Low'}], caption:'SMC Internal Liquidity - מלא הרמות' }},
    ],
  },
];

export default lessons;
