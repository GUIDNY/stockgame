// Seeded LCG for reproducible canonical pattern examples
function lcg(seed) {
  let s = (seed ^ 0xdeadbeef) >>> 0 || 1;
  return () => { s = (Math.imul(1664525, s) + 1013904223) >>> 0; return s / 4294967296; };
}
const r2 = (n) => Math.round(n * 100) / 100;
function genTrend(startTime, price, count, bias, vol, seed) {
  const rng = lcg(seed);
  const out = [];
  for (let i = 0; i < count; i++) {
    const change = bias + (rng() - 0.5) * vol;
    const open = r2(price);
    const close = r2(price + change);
    const wu = rng() * vol * 0.35, wd = rng() * vol * 0.35;
    out.push({ time: startTime + i * 86400, open, high: r2(Math.max(open, close) + wu), low: r2(Math.min(open, close) - wd), close });
    price = close;
  }
  return { candles: out, price, next: startTime + count * 86400 };
}
function mk(time, open, close, wu, wd) {
  return { time, open: r2(open), high: r2(Math.max(open, close) + wu), low: r2(Math.min(open, close) - wd), close: r2(close) };
}
const T0 = 1704067200, D = 86400;

// ── Build canonical (synthetic) candles for each pattern ───────────────────
function buildHammer() {
  const ctx = genTrend(T0, 100, 12, -1.0, 2.2, 1001);
  const p = ctx.price, t = ctx.next;
  return { q: [...ctx.candles, mk(t, p, p + 0.9, 1.2, 6.5)], r: genTrend(t + D, p + 0.9, 5, 1.8, 1.8, 2001).candles };
}
function buildShootingStar() {
  const ctx = genTrend(T0, 80, 12, 1.0, 2.0, 1002);
  const p = ctx.price, t = ctx.next;
  return { q: [...ctx.candles, mk(t, p, p - 0.9, 6.5, 1.2)], r: genTrend(t + D, p - 0.9, 5, -1.8, 1.8, 2002).candles };
}
function buildBullishEngulfing() {
  const ctx = genTrend(T0, 100, 11, -1.0, 2.0, 1003);
  const p = ctx.price, t = ctx.next;
  return { q: [...ctx.candles, mk(t, p, p - 1.8, 0.5, 0.5), mk(t + D, p - 1.8, p + 3.8, 0.8, 0.8)], r: genTrend(t + 2 * D, p + 3.8, 5, 1.5, 1.5, 2003).candles };
}
function buildBearishEngulfing() {
  const ctx = genTrend(T0, 80, 11, 1.0, 2.0, 1004);
  const p = ctx.price, t = ctx.next;
  return { q: [...ctx.candles, mk(t, p, p + 1.8, 0.5, 0.5), mk(t + D, p + 1.8, p - 3.8, 0.8, 0.8)], r: genTrend(t + 2 * D, p - 3.8, 5, -1.5, 1.5, 2004).candles };
}
function buildMorningStar() {
  const ctx = genTrend(T0, 100, 10, -1.0, 2.0, 1005);
  const p = ctx.price, t = ctx.next;
  return { q: [...ctx.candles, mk(t, p, p - 4.0, 0.5, 0.5), mk(t + D, p - 4.0, p - 4.2, 1.8, 1.8), mk(t + 2 * D, p - 4.2, p - 0.5, 0.5, 0.5)], r: genTrend(t + 3 * D, p - 0.5, 5, 1.6, 1.5, 2005).candles };
}
function buildEveningStar() {
  const ctx = genTrend(T0, 80, 10, 1.0, 2.0, 1006);
  const p = ctx.price, t = ctx.next;
  return { q: [...ctx.candles, mk(t, p, p + 4.0, 0.5, 0.5), mk(t + D, p + 4.0, p + 4.2, 1.8, 1.8), mk(t + 2 * D, p + 4.2, p + 0.5, 0.5, 0.5)], r: genTrend(t + 3 * D, p + 0.5, 5, -1.6, 1.5, 2006).candles };
}
function buildDoji() {
  const ctx = genTrend(T0, 100, 12, -0.8, 1.8, 1007);
  const p = ctx.price, t = ctx.next;
  return { q: [...ctx.candles, mk(t, p, p + 0.1, 3.0, 3.0)], r: genTrend(t + D, p + 0.1, 5, 1.3, 1.5, 2007).candles };
}
function buildThreeWhite() {
  const ctx = genTrend(T0, 80, 10, -0.5, 2.0, 1008);
  const p = ctx.price, t = ctx.next;
  return { q: [...ctx.candles, mk(t, p, p + 3.2, 0.4, 0.4), mk(t + D, p + 2.6, p + 5.8, 0.4, 0.4), mk(t + 2 * D, p + 5.2, p + 8.6, 0.4, 0.4)], r: genTrend(t + 3 * D, p + 8.6, 5, 1.5, 1.5, 2008).candles };
}
function buildThreeCrows() {
  const ctx = genTrend(T0, 100, 10, 0.5, 2.0, 1009);
  const p = ctx.price, t = ctx.next;
  return { q: [...ctx.candles, mk(t, p, p - 3.2, 0.4, 0.4), mk(t + D, p - 2.6, p - 5.8, 0.4, 0.4), mk(t + 2 * D, p - 5.2, p - 8.6, 0.4, 0.4)], r: genTrend(t + 3 * D, p - 8.6, 5, -1.5, 1.5, 2009).candles };
}
function buildInvertedHammer() {
  const ctx = genTrend(T0, 100, 12, -0.9, 2.0, 1010);
  const p = ctx.price, t = ctx.next;
  return { q: [...ctx.candles, mk(t, p, p + 0.9, 6.0, 1.2)], r: genTrend(t + D, p + 0.9, 5, 1.5, 1.5, 2010).candles };
}
function buildHangingMan() {
  const ctx = genTrend(T0, 80, 12, 0.9, 2.0, 1011);
  const p = ctx.price, t = ctx.next;
  return { q: [...ctx.candles, mk(t, p, p + 0.9, 1.2, 6.5)], r: genTrend(t + D, p + 0.9, 5, -1.5, 1.5, 2011).candles };
}
function buildMarubozu() {
  const ctx = genTrend(T0, 80, 12, 0.1, 1.5, 1012);
  const p = ctx.price, t = ctx.next;
  return { q: [...ctx.candles, mk(t, p, p + 5.5, 0.2, 0.2)], r: genTrend(t + D, p + 5.5, 5, 1.4, 1.5, 2012).candles };
}

// ── The full definition array ───────────────────────────────────────────────
const hammer = buildHammer();
const shootingStar = buildShootingStar();
const bullishEngulfing = buildBullishEngulfing();
const bearishEngulfing = buildBearishEngulfing();
const morningStar = buildMorningStar();
const eveningStar = buildEveningStar();
const doji = buildDoji();
const threeWhite = buildThreeWhite();
const threeCrows = buildThreeCrows();
const invertedHammer = buildInvertedHammer();
const hangingMan = buildHangingMan();
const marubozu = buildMarubozu();

const patternDefinitions = [
  {
    id: 'hammer',
    name: 'פטיש',
    english: 'Hammer',
    direction: 'bullish',
    difficulty: 'easy',
    description: 'נר עם גוף קטן בחלק העליון ולהב תחתון ארוך. מופיע בסוף מגמת ירידה.',
    tip: 'הלהב התחתון ארוך לפחות פי 2 מגוף הנר — הקונים הצילו את המחיר',
    explanation: 'המוכרים דחפו את המחיר למטה, אך הקונים נכנסו בכוח וסגרו את הנר גבוה. סימן שהירידה מאבדת כוח.',
    checklist: ['גוף קטן בשליש העליון', 'להב תחתון ≥ 2× גוף', 'להב עליון קטן או אפסי', 'מופיע אחרי ירידה'],
    winRate: 68,
    questionCandles: hammer.q,
    revealCandles: hammer.r,
    answer: 'up',
  },
  {
    id: 'shooting_star',
    name: 'כוכב נופל',
    english: 'Shooting Star',
    direction: 'bearish',
    difficulty: 'easy',
    description: 'נר עם גוף קטן בחלק התחתון ולהב עליון ארוך. מופיע בסוף מגמת עלייה.',
    tip: 'הלהב העליון ארוך פי 2 מהגוף — הקונים ניסו ונכשלו',
    explanation: 'הקונים ניסו להעלות את המחיר אך המוכרים נכנסו בכוח ודחפו את הסגירה למטה.',
    checklist: ['גוף קטן בשליש התחתון', 'להב עליון ≥ 2× גוף', 'להב תחתון קטן', 'מופיע אחרי עלייה'],
    winRate: 66,
    questionCandles: shootingStar.q,
    revealCandles: shootingStar.r,
    answer: 'down',
  },
  {
    id: 'bullish_engulfing',
    name: 'בליעה שורית',
    english: 'Bullish Engulfing',
    direction: 'bullish',
    difficulty: 'easy',
    description: 'נר ירוק גדול שבולע את הנר האדום שלפניו. מופיע בסוף ירידה.',
    tip: 'הנר הירוק פותח מתחת לסגירה הקודמת וסוגר מעל הפתיחה הקודמת',
    explanation: 'הבליעה השורית מסמנת שהקונים השתלטו בצורה מוחלטת. הנר הגדול מראה נפח וכוח קנייה חזק.',
    checklist: ['נר אדום לפני', 'נר ירוק שבולע לחלוטין', 'פתיחה מתחת לסגירה הקודמת', 'סגירה מעל הפתיחה הקודמת'],
    winRate: 72,
    questionCandles: bullishEngulfing.q,
    revealCandles: bullishEngulfing.r,
    answer: 'up',
  },
  {
    id: 'bearish_engulfing',
    name: 'בליעה דובית',
    english: 'Bearish Engulfing',
    direction: 'bearish',
    difficulty: 'easy',
    description: 'נר אדום גדול שבולע את הנר הירוק שלפניו. מופיע בסוף עלייה.',
    tip: 'הנר האדום פותח מעל הסגירה הקודמת וסוגר מתחת לפתיחה הקודמת',
    explanation: 'הבליעה הדובית מסמנת שהמוכרים השתלטו בכוח. כמות גבוהה מחזקת את האות.',
    checklist: ['נר ירוק לפני', 'נר אדום שבולע לחלוטין', 'פתיחה מעל הסגירה הקודמת', 'סגירה מתחת לפתיחה הקודמת'],
    winRate: 70,
    questionCandles: bearishEngulfing.q,
    revealCandles: bearishEngulfing.r,
    answer: 'down',
  },
  {
    id: 'morning_star',
    name: 'כוכב הבוקר',
    english: 'Morning Star',
    direction: 'bullish',
    difficulty: 'medium',
    description: 'תבנית 3 נרות: אדום גדול ← דוג׳י קטן ← ירוק גדול. מסמנת היפוך בתחתית.',
    tip: 'הנר השלישי חוזר לפחות לאמצע הנר הראשון — שינוי כיוון מאושר',
    explanation: 'שלושת הנרות מראים מאבק כוחות שמסתיים בניצחון הקונים. אחת התבניות האמינות ביותר לשינוי מגמה.',
    checklist: ['נר אדום גדול', "נר דוג'י קטן (גאפ למטה)", 'נר ירוק גדול', 'הנר הירוק סוגר מעל אמצע הנר האדום'],
    winRate: 74,
    questionCandles: morningStar.q,
    revealCandles: morningStar.r,
    answer: 'up',
  },
  {
    id: 'evening_star',
    name: 'כוכב הערב',
    english: 'Evening Star',
    direction: 'bearish',
    difficulty: 'medium',
    description: 'תבנית 3 נרות: ירוק גדול ← דוג׳י קטן ← אדום גדול. מסמנת היפוך בשיא.',
    tip: 'הנר השלישי יורד לפחות לאמצע הנר הראשון — היפוך מאושר',
    explanation: 'כוכב הערב מסמן שהקונים מאבדים שליטה. הנר הירוק, הדוג׳י המהסס, והנר האדום — שלושתם ביחד מסמנים היפוך.',
    checklist: ['נר ירוק גדול', "נר דוג'י קטן (גאפ למעלה)", 'נר אדום גדול', 'הנר האדום סוגר מתחת לאמצע הנר הירוק'],
    winRate: 72,
    questionCandles: eveningStar.q,
    revealCandles: eveningStar.r,
    answer: 'down',
  },
  {
    id: 'doji_support',
    name: "דוג'י בתמיכה",
    english: 'Doji at Support',
    direction: 'bullish',
    difficulty: 'medium',
    description: 'נר ניטרלי: פתיחה וסגירה כמעט זהות, להבים ארוכים משני הצדדים.',
    tip: "דוג'י בסוף ירידה = קנייה ומכירה מתאזנות — שינוי אפשרי",
    explanation: "הדוג'י מסמן שהשוק מהסס — המוכרים והקונים שווים בכוח. לאחר ירידה, סימן שהלחץ כלפי מטה מתמעט.",
    checklist: ['פתיחה ≈ סגירה', 'להבים ארוכים משני הצדדים', 'מופיע אחרי ירידה', 'נר ירוק אחריו מאשר'],
    winRate: 60,
    questionCandles: doji.q,
    revealCandles: doji.r,
    answer: 'up',
  },
  {
    id: 'three_white_soldiers',
    name: 'שלושה חיילים לבנים',
    english: 'Three White Soldiers',
    direction: 'bullish',
    difficulty: 'medium',
    description: 'שלושה נרות ירוקים גדולים ברצף, כל אחד פותח בגוף הקודם וסוגר גבוה יותר.',
    tip: 'כל נר סוגר קרוב לשיא שלו — קנייה עקבית ללא היסוס',
    explanation: 'שלושה חיילים לבנים מסמנים כוח קנייה חזק ומתמשך. תבנית חזקה במיוחד אחרי ירידה.',
    checklist: ['3 נרות ירוקים גדולים', 'כל אחד פותח בגוף הקודם', 'כל אחד סוגר בשיא חדש', 'להבים עליונים קטנים'],
    winRate: 76,
    questionCandles: threeWhite.q,
    revealCandles: threeWhite.r,
    answer: 'up',
  },
  {
    id: 'three_black_crows',
    name: 'שלושה עורבים שחורים',
    english: 'Three Black Crows',
    direction: 'bearish',
    difficulty: 'medium',
    description: 'שלושה נרות אדומים גדולים ברצף, כל אחד פותח בגוף הקודם וסוגר נמוך יותר.',
    tip: 'כל נר סוגר קרוב לתחתית שלו — מכירה עקבית ולחוצה',
    explanation: 'שלושה עורבים שחורים מסמנים לחץ מכירה חזק ומתמשך. תבנית חזקה במיוחד אחרי עלייה.',
    checklist: ['3 נרות אדומים גדולים', 'כל אחד פותח בגוף הקודם', 'כל אחד סוגר בשפל חדש', 'להבים תחתונים קטנים'],
    winRate: 74,
    questionCandles: threeCrows.q,
    revealCandles: threeCrows.r,
    answer: 'down',
  },
  {
    id: 'inverted_hammer',
    name: 'פטיש הפוך',
    english: 'Inverted Hammer',
    direction: 'bullish',
    difficulty: 'hard',
    description: 'נר עם גוף קטן בחלק התחתון ולהב עליון ארוך. מופיע בתחתית ירידה.',
    tip: 'שונה מכוכב נופל! הפטיש ההפוך מופיע בסוף ירידה — לא עלייה',
    explanation: 'הלהב העליון מראה שהקונים התחילו להיכנס. נר ירוק אחריו מאשר את שינוי הכיוון.',
    checklist: ['גוף קטן בשליש התחתון', 'להב עליון ≥ 2× גוף', 'מופיע אחרי ירידה', 'נר ירוק אחריו = אישור'],
    winRate: 62,
    questionCandles: invertedHammer.q,
    revealCandles: invertedHammer.r,
    answer: 'up',
  },
  {
    id: 'hanging_man',
    name: 'האיש התלוי',
    english: 'Hanging Man',
    direction: 'bearish',
    difficulty: 'hard',
    description: 'נר עם גוף קטן בחלק העליון ולהב תחתון ארוך. מופיע בסוף עלייה.',
    tip: 'נראה כמו פטיש אך מופיע בשיא! ההקשר קובע הכל',
    explanation: 'למרות שנראה כמו פטיש, כאן הוא מופיע אחרי עלייה. הלהב מראה שהמוכרים ניסו לדחוף למטה — אזהרה לסוף העלייה.',
    checklist: ['גוף קטן בשליש העליון', 'להב תחתון ≥ 2× גוף', 'מופיע אחרי עלייה', 'נר אדום אחריו = אישור'],
    winRate: 60,
    questionCandles: hangingMan.q,
    revealCandles: hangingMan.r,
    answer: 'down',
  },
  {
    id: 'bullish_marubozu',
    name: 'מרובוזו שורי',
    english: 'Bullish Marubozu',
    direction: 'bullish',
    difficulty: 'easy',
    description: 'נר ירוק גדול כמעט ללא להבים — פתיחה בתחתית, סגירה בשיא.',
    tip: 'קנייה מוחלטת מהפתיחה עד הסגירה — אין ספק בשוק',
    explanation: 'מרובוזו שורי מסמן שהקונים שלטו לאורך כל היום. אין להבים — אין מוכרים. סימן חזק להמשך עלייה.',
    checklist: ['נר ירוק גדול', 'כמעט ללא להבים', 'פתיחה = שפל', 'סגירה = שיא'],
    winRate: 78,
    questionCandles: marubozu.q,
    revealCandles: marubozu.r,
    answer: 'up',
  },
];

export default patternDefinitions;
