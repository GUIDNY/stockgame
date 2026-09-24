# שעת סגירה · BuyToday

משחק תלת־ממד לנייד שבו דוחפים עגלה בחנות BuyToday, אוספים רשימת קניות ומגיעים לקופה לפני 21:00.
המוצרים על המדפים אמיתיים: שם, מחיר, מבצע ותמונה מ־buytoday.co.il.

## למה זה לא מכביד על האתר
- המשחק הוא קובץ HTML סטטי אחד (בערך 135KB, ועוד three.js מ־CDN). אין לו שרת ואין לו מסד נתונים.
- נתוני המוצרים והתמונות המוקטנות "נאפים" לתוך הקובץ בזמן הבנייה. שחקנים לא שולחים אף בקשה ל־buytoday.co.il.
- רק לחיצה על "לקנייה באתר" פותחת את עמוד המוצר, עם `utm_source=closing-time-game` כדי שאפשר יהיה לראות באנליטיקס כמה מכירות הגיעו מהמשחק.

## עדכון מוצרים ומחירים
1. עורכים את `products.config.json` (slug מהכתובת באתר, סוג הדגם התלת־ממדי, שם קצר).
2. מריצים `python3 store-game/build.py` (צריך `pip install pillow`).
3. הפלט הוא `public/store-game/index.html`, שאפשר לפרוס בכל מקום.

שינוי בקוד בלבד: `python3 store-game/build.py --offline`.

סוגי דגמים: tv, receiver, kettle, coffee, airfryer, grill, microwave, fridge, blender, processor, toasteroven, stickvac, washer.
