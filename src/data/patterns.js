// Seeded LCG random number generator for reproducible patterns
function lcg(seed) {
  let s = (seed ^ 0xdeadbeef) >>> 0 || 1;
  return function () {
    s = (Math.imul(1664525, s) + 1013904223) >>> 0;
    return s / 4294967296;
  };
}

const r2 = (n) => Math.round(n * 100) / 100;

// Generate a trend of candles with noise
function genTrend(startTime, startPrice, count, bias, volatility, seed) {
  const rng = lcg(seed);
  const out = [];
  let price = startPrice;
  for (let i = 0; i < count; i++) {
    const change = bias + (rng() - 0.5) * volatility;
    const open = r2(price);
    const close = r2(price + change);
    const wUp = rng() * volatility * 0.35;
    const wDown = rng() * volatility * 0.35;
    out.push({
      time: startTime + i * 86400,
      open,
      high: r2(Math.max(open, close) + wUp),
      low: r2(Math.min(open, close) - wDown),
      close,
    });
    price = close;
  }
  return { candles: out, price, next: startTime + count * 86400 };
}

// Create a single manually-shaped candle
function mk(time, open, close, wUp, wDown) {
  return {
    time,
    open: r2(open),
    high: r2(Math.max(open, close) + wUp),
    low: r2(Math.min(open, close) - wDown),
    close: r2(close),
  };
}

const BASE = 1704067200; // 2024-01-01 UTC
const D = 86400;

const patterns = [];

// ─── 1. HAMMER ──────────────────────────────────────────────────────────────
{
  const ctx = genTrend(BASE, 100, 15, -1.0, 2.2, 1001);
  const p = ctx.price, t = ctx.next;
  patterns.push({
    id: 'hammer',
    name: 'פטיש',
    english: 'Hammer',
    direction: 'bullish',
    difficulty: 'easy',
    description: 'נר עם גוף קטן בחלק העליון ולהב תחתון ארוך. מופיע בסוף מגמת ירידה.',
    tip: 'הלהב התחתון ארוך לפחות פי 2 מגוף הנר — הקונים הצילו את המחיר',
    explanation: 'המוכרים דחפו את המחיר למטה, אך הקונים נכנסו בכוח וסגרו את הנר גבוה. סימן לכך שהירידה מאבדת כוח.',
    questionCandles: [...ctx.candles, mk(t, p, p + 0.9, 1.2, 6.5)],
    revealCandles: genTrend(t + D, p + 0.9, 5, 1.8, 1.8, 2001).candles,
    answer: 'up',
  });
}

// ─── 2. SHOOTING STAR ────────────────────────────────────────────────────────
{
  const ctx = genTrend(BASE, 80, 15, 1.0, 2.0, 1002);
  const p = ctx.price, t = ctx.next;
  patterns.push({
    id: 'shooting_star',
    name: 'כוכב נופל',
    english: 'Shooting Star',
    direction: 'bearish',
    difficulty: 'easy',
    description: 'נר עם גוף קטן בחלק התחתון ולהב עליון ארוך. מופיע בסוף מגמת עלייה.',
    tip: 'הלהב העליון ארוך פי 2 מהגוף — הקונים ניסו ונכשלו',
    explanation: 'הקונים ניסו להעלות את המחיר אך המוכרים נכנסו בכוח ודחפו את הסגירה למטה. סימן שהעלייה מאבדת כוח.',
    questionCandles: [...ctx.candles, mk(t, p, p - 0.9, 6.5, 1.2)],
    revealCandles: genTrend(t + D, p - 0.9, 5, -1.8, 1.8, 2002).candles,
    answer: 'down',
  });
}

// ─── 3. BULLISH ENGULFING ────────────────────────────────────────────────────
{
  const ctx = genTrend(BASE, 100, 13, -1.0, 2.0, 1003);
  const p = ctx.price, t = ctx.next;
  const red = mk(t, p, p - 1.8, 0.5, 0.5);
  const green = mk(t + D, p - 1.8, p + 3.8, 0.8, 0.8);
  patterns.push({
    id: 'bullish_engulfing',
    name: 'בליעה שורית',
    english: 'Bullish Engulfing',
    direction: 'bullish',
    difficulty: 'easy',
    description: 'נר ירוק גדול שבולע לחלוטין את הנר האדום שלפניו. מופיע בסוף ירידה.',
    tip: 'הנר הירוק פותח מתחת לסגירה הקודמת וסוגר מעל הפתיחה הקודמת',
    explanation: 'הבליעה השורית מסמנת שהקונים השתלטו בצורה מוחלטת. הנר הגדול מראה נפח וכוח קנייה חזק.',
    questionCandles: [...ctx.candles, red, green],
    revealCandles: genTrend(t + 2 * D, p + 3.8, 5, 1.5, 1.5, 2003).candles,
    answer: 'up',
  });
}

// ─── 4. BEARISH ENGULFING ────────────────────────────────────────────────────
{
  const ctx = genTrend(BASE, 80, 13, 1.0, 2.0, 1004);
  const p = ctx.price, t = ctx.next;
  const green = mk(t, p, p + 1.8, 0.5, 0.5);
  const red = mk(t + D, p + 1.8, p - 3.8, 0.8, 0.8);
  patterns.push({
    id: 'bearish_engulfing',
    name: 'בליעה דובית',
    english: 'Bearish Engulfing',
    direction: 'bearish',
    difficulty: 'easy',
    description: 'נר אדום גדול שבולע לחלוטין את הנר הירוק שלפניו. מופיע בסוף עלייה.',
    tip: 'הנר האדום פותח מעל הסגירה הקודמת וסוגר מתחת לפתיחה הקודמת',
    explanation: 'הבליעה הדובית מסמנת שהמוכרים השתלטו בכוח. כמות גבוהה מחזקת את האות.',
    questionCandles: [...ctx.candles, green, red],
    revealCandles: genTrend(t + 2 * D, p - 3.8, 5, -1.5, 1.5, 2004).candles,
    answer: 'down',
  });
}

// ─── 5. MORNING STAR ─────────────────────────────────────────────────────────
{
  const ctx = genTrend(BASE, 100, 12, -1.0, 2.0, 1005);
  const p = ctx.price, t = ctx.next;
  const bigRed = mk(t, p, p - 4.0, 0.5, 0.5);
  const doji = mk(t + D, p - 4.0, p - 4.2, 1.8, 1.8);
  const bigGreen = mk(t + 2 * D, p - 4.2, p - 0.5, 0.5, 0.5);
  patterns.push({
    id: 'morning_star',
    name: 'כוכב הבוקר',
    english: 'Morning Star',
    direction: 'bullish',
    difficulty: 'medium',
    description: 'תבנית 3 נרות: אדום גדול → נר קטן (דוג׳י) → ירוק גדול. מסמנת היפוך בתחתית.',
    tip: 'הנר האמצעי מסמן היסוס — הקרב בין קונים ומוכרים',
    explanation: 'שלושת הנרות מראים מאבק כוחות שמסתיים בניצחון הקונים. כוכב הבוקר הוא אחת התבניות האמינות ביותר לשינוי מגמה.',
    questionCandles: [...ctx.candles, bigRed, doji, bigGreen],
    revealCandles: genTrend(t + 3 * D, p - 0.5, 5, 1.6, 1.5, 2005).candles,
    answer: 'up',
  });
}

// ─── 6. EVENING STAR ─────────────────────────────────────────────────────────
{
  const ctx = genTrend(BASE, 80, 12, 1.0, 2.0, 1006);
  const p = ctx.price, t = ctx.next;
  const bigGreen = mk(t, p, p + 4.0, 0.5, 0.5);
  const doji = mk(t + D, p + 4.0, p + 4.2, 1.8, 1.8);
  const bigRed = mk(t + 2 * D, p + 4.2, p + 0.5, 0.5, 0.5);
  patterns.push({
    id: 'evening_star',
    name: 'כוכב הערב',
    english: 'Evening Star',
    direction: 'bearish',
    difficulty: 'medium',
    description: 'תבנית 3 נרות: ירוק גדול → נר קטן → אדום גדול. מסמנת היפוך בשיא.',
    tip: 'הנר האמצעי מגאפ למעלה — מחיר שאינו בר-קיימא',
    explanation: 'כוכב הערב מסמן שהקונים מאבדים שליטה. הנר הירוק, הדוג׳י המהסס, והנר האדום הגדול — שלושתם ביחד מסמנים היפוך.',
    questionCandles: [...ctx.candles, bigGreen, doji, bigRed],
    revealCandles: genTrend(t + 3 * D, p + 0.5, 5, -1.6, 1.5, 2006).candles,
    answer: 'down',
  });
}

// ─── 7. DOJI AT SUPPORT ──────────────────────────────────────────────────────
{
  const ctx = genTrend(BASE, 100, 15, -0.8, 1.8, 1007);
  const p = ctx.price, t = ctx.next;
  patterns.push({
    id: 'doji_support',
    name: "דוג'י בתמיכה",
    english: 'Doji at Support',
    direction: 'bullish',
    difficulty: 'medium',
    description: 'נר ניטרלי: פתיחה וסגירה כמעט זהות, להבים ארוכים משני הצדדים. מסמן היסוס.',
    tip: "דוג'י בסוף ירידה = הקנייה והמכירה מתאזנות — שינוי אפשרי",
    explanation: "הדוג'י מסמן שהשוק מהסס — המוכרים והקונים שווים בכוח. לאחר ירידה, זה סימן שהלחץ כלפי מטה מתמעט.",
    questionCandles: [...ctx.candles, mk(t, p, p + 0.1, 3.0, 3.0)],
    revealCandles: genTrend(t + D, p + 0.1, 5, 1.3, 1.5, 2007).candles,
    answer: 'up',
  });
}

// ─── 8. THREE WHITE SOLDIERS ─────────────────────────────────────────────────
{
  const ctx = genTrend(BASE, 80, 12, -0.5, 2.0, 1008);
  const p = ctx.price, t = ctx.next;
  const s1 = mk(t, p, p + 3.2, 0.4, 0.4);
  const s2 = mk(t + D, p + 2.6, p + 5.8, 0.4, 0.4);
  const s3 = mk(t + 2 * D, p + 5.2, p + 8.6, 0.4, 0.4);
  patterns.push({
    id: 'three_white_soldiers',
    name: 'שלושה חיילים לבנים',
    english: 'Three White Soldiers',
    direction: 'bullish',
    difficulty: 'medium',
    description: 'שלושה נרות ירוקים גדולים ברצף, כל אחד פותח בגוף הקודם וסוגר גבוה יותר.',
    tip: 'כל נר סוגר קרוב לשיא שלו — קנייה עקבית ללא מהסס',
    explanation: 'שלושה חיילים לבנים מסמנים כוח קנייה חזק ומתמשך. תבנית חזקה במיוחד אחרי ירידה.',
    questionCandles: [...ctx.candles, s1, s2, s3],
    revealCandles: genTrend(t + 3 * D, p + 8.6, 5, 1.5, 1.5, 2008).candles,
    answer: 'up',
  });
}

// ─── 9. THREE BLACK CROWS ────────────────────────────────────────────────────
{
  const ctx = genTrend(BASE, 100, 12, 0.5, 2.0, 1009);
  const p = ctx.price, t = ctx.next;
  const cr1 = mk(t, p, p - 3.2, 0.4, 0.4);
  const cr2 = mk(t + D, p - 2.6, p - 5.8, 0.4, 0.4);
  const cr3 = mk(t + 2 * D, p - 5.2, p - 8.6, 0.4, 0.4);
  patterns.push({
    id: 'three_black_crows',
    name: 'שלושה עורבים שחורים',
    english: 'Three Black Crows',
    direction: 'bearish',
    difficulty: 'medium',
    description: 'שלושה נרות אדומים גדולים ברצף, כל אחד פותח בגוף הקודם וסוגר נמוך יותר.',
    tip: 'כל נר סוגר קרוב לתחתית שלו — מכירה עקבית ולחוצה',
    explanation: 'שלושה עורבים שחורים מסמנים לחץ מכירה חזק ומתמשך. תבנית חזקה במיוחד אחרי עלייה.',
    questionCandles: [...ctx.candles, cr1, cr2, cr3],
    revealCandles: genTrend(t + 3 * D, p - 8.6, 5, -1.5, 1.5, 2009).candles,
    answer: 'down',
  });
}

// ─── 10. INVERTED HAMMER ─────────────────────────────────────────────────────
{
  const ctx = genTrend(BASE, 100, 15, -0.9, 2.0, 1010);
  const p = ctx.price, t = ctx.next;
  patterns.push({
    id: 'inverted_hammer',
    name: 'פטיש הפוך',
    english: 'Inverted Hammer',
    direction: 'bullish',
    difficulty: 'hard',
    description: 'נר עם גוף קטן בחלק התחתון ולהב עליון ארוך. מופיע בתחתית ירידה.',
    tip: 'שים לב: כוכב נופל מופיע בשיא — פטיש הפוך מופיע בתחתית!',
    explanation: 'הלהב העליון מראה שהקונים התחילו להיכנס. כניסה של נר ירוק אחריו מאשרת את הסימן לשינוי כיוון.',
    questionCandles: [...ctx.candles, mk(t, p, p + 0.9, 6.0, 1.2)],
    revealCandles: genTrend(t + D, p + 0.9, 5, 1.5, 1.5, 2010).candles,
    answer: 'up',
  });
}

// ─── 11. HANGING MAN ─────────────────────────────────────────────────────────
{
  const ctx = genTrend(BASE, 80, 15, 0.9, 2.0, 1011);
  const p = ctx.price, t = ctx.next;
  patterns.push({
    id: 'hanging_man',
    name: 'האיש התלוי',
    english: 'Hanging Man',
    direction: 'bearish',
    difficulty: 'hard',
    description: 'נר עם גוף קטן בחלק העליון ולהב תחתון ארוך. מופיע בסוף עלייה.',
    tip: 'שים לב: פטיש מופיע בתחתית — האיש התלוי מופיע בשיא!',
    explanation: 'למרות שנראה כמו פטיש, כאן הוא מופיע אחרי עלייה. הלהב הארוך מראה שהמוכרים ניסו לדחוף למטה — אזהרה לסוף העלייה.',
    questionCandles: [...ctx.candles, mk(t, p, p + 0.9, 1.2, 6.5)],
    revealCandles: genTrend(t + D, p + 0.9, 5, -1.5, 1.5, 2011).candles,
    answer: 'down',
  });
}

// ─── 12. BULLISH MARUBOZU ────────────────────────────────────────────────────
{
  const ctx = genTrend(BASE, 80, 14, 0.1, 1.5, 1012);
  const p = ctx.price, t = ctx.next;
  // Strong green candle with no wicks (or tiny ones)
  const marubozu = mk(t, p, p + 5.5, 0.2, 0.2);
  patterns.push({
    id: 'bullish_marubozu',
    name: 'מרובוזו שורי',
    english: 'Bullish Marubozu',
    direction: 'bullish',
    difficulty: 'easy',
    description: 'נר ירוק גדול כמעט ללא להבים — פתיחה בתחתית, סגירה בשיא.',
    tip: 'קנייה חד-משמעית לאורך כל היום ללא היסוס',
    explanation: 'מרובוזו שורי מסמן שהקונים שלטו מהפתיחה עד הסגירה. אין להבים — אין מוכרים. סימן חזק להמשך עלייה.',
    questionCandles: [...ctx.candles, marubozu],
    revealCandles: genTrend(t + D, p + 5.5, 5, 1.4, 1.5, 2012).candles,
    answer: 'up',
  });
}

export default patterns;
