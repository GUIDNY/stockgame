# חיבור הקופה במשחק לתשלום באתר (הצעה, לא הוחל)

**מה בדקתי בקוד של האתר (קריאה בלבד, בלי שינוי):**
- העגלה נשמרת במסד הנתונים של האתר, לפי העוגייה `prec_cart_sid` או לפי המשתמש המחובר (`src/lib/cart.ts`).
- הוספה לעגלה נעשית רק דרך ה־server action `addToCartAction` (`src/actions/cart.ts`). אין כתובת שממלאת עגלה.
- התשלום עובר דרך Pelecard (`src/app/api/pelecard`, `/checkout/pay`).

המסקנה: בלי שינוי בקוד של האתר, הכי רחוק שאפשר להגיע הוא עמוד המוצר ("קנה עכשיו" שם מעביר לתשלום). כך המשחק עובד היום.

**כשתחליטו לחבר (שינוי אחד, קובץ חדש, בלי לגעת בקוד קיים):**
1. מעתיקים את `game-checkout.route.ts` ל־`src/app/(shop)/game-checkout/route.ts` בריפו `GUIDNY/pr`.
2. ב־`store-game/products.config.json` כאן ממלאים `"checkout_url": "https://buytoday.co.il/game-checkout?items={items}"`.
3. מריצים `python3 store-game/build.py --offline` ופורסים. בקופה במשחק יופיע "לתשלום באתר", והלקוח ינחת ב־`/checkout` עם כל העגלה.

הנתיב מוסיף רק מוצרים שמוצגים באתר ויש מהם במלאי, מדלג על מה שכבר בעגלה, מוגבל ל־10 פריטים, ומשאיר את כל התשלום במסוף הקיים.
