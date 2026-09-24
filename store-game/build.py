#!/usr/bin/env python3
"""Builds the BuyToday mall game with a fresh snapshot of real products.

Run occasionally (prices and deals change):   python3 store-game/build.py
Each main category on buytoday.co.il becomes a department in the mall. The
script reads each category page once, keeps the first products that have a
photo, shrinks the photos and bakes everything into one static page, so the
people playing never load the shop's servers.

Only changed the game code?                   python3 store-game/build.py --offline
Only refresh the hot deals?                   python3 store-game/build.py --add-deals
reuses the products already baked into public/store-game/index.html.

Outputs: public/store-game/index.html (deploy anywhere) and store-game/dist/game.html.
Requires: Pillow (pip install pillow)
"""
import base64, datetime, html, io, json, pathlib, re, sys, time, urllib.request
from PIL import Image

ROOT = pathlib.Path(__file__).resolve().parent
OUT = ROOT.parent / 'public' / 'store-game'
SITE = 'https://buytoday.co.il'
UA = {'User-Agent': 'BuyToday-StoreGame-Builder/2.0'}

# Which 3D model stands on the pedestal: first matching word in the product name wins.
KIND_WORDS = [
    ('מייבש כביסה', 'washer'), ('מייבש שיער', 'hairdryer'), ('תנור חימום', 'heater'), ('מפזר חום', 'heater'),
    ('מקרן', 'tv'), ('טלוויזיה', 'tv'), ('מסך', 'tv'), ('מתקן', 'tv'),
    ('רסיבר', 'receiver'), ('מגבר', 'receiver'), ('רמקול', 'speaker'), ('מקרן קול', 'speaker'), ('סאונד', 'speaker'), ('כבל', 'receiver'),
    ('קומקום', 'kettle'), ('אספרסו', 'coffee'), ('קפה', 'coffee'), ('בלנדר', 'blender'), ('מעבד', 'processor'), ('מסחטת', 'blender'),
    ('אייר פרייר', 'airfryer'), ('טיגון', 'airfryer'), ('גריל', 'grill'), ('טוסטר', 'toasteroven'), ('מטחנת', 'processor'),
    ('מיקרוגל', 'microwave'), ('כיריים', 'cooktop'), ('קולט', 'hood'), ('תנור', 'oven'),
    ('מקרר', 'fridge'), ('מקפיא', 'fridge'), ('יין', 'fridge'), ('כביסה', 'washer'), ('מדיח', 'washer'), ('מייבש', 'washer'),
    ('שואב', 'stickvac'), ('מגהץ', 'iron'), ('מזגן', 'ac'), ('מאוורר', 'fan'), ('רדיאטור', 'heater'), ('תאורת', 'heater'), ('קטלן', 'fan'),
    ('פן', 'hairdryer'), ('מחליק', 'hairdryer'), ('גילוח', 'shaver'), ('תספורת', 'shaver'), ('ברז', 'fridge'), ('בר מים', 'fridge'),
]
ZONE_KIND = {'deal': 'processor', 'tv': 'tv', 'audio': 'speaker', 'kitchen': 'processor', 'ovens': 'oven', 'care': 'shaver', 'fridge': 'fridge',
             'laundry': 'washer', 'home': 'stickvac', 'ac': 'ac', 'heat': 'heater'}

def get(url, binary=False, tries=4):
    for n in range(tries):
        try:
            with urllib.request.urlopen(urllib.request.Request(url, headers=UA), timeout=25) as r:
                data = r.read()
            return data if binary else data.decode('utf-8', 'replace')
        except Exception:
            if n == tries - 1: raise
            time.sleep(2 ** n)

def cards(page):
    """Product cards exactly as the category page lists them."""
    for c in page.split('class="group bg-card relative flex flex-col')[1:]:
        slug = re.search(r'href="/product/([^"]+)"', c)
        name = re.search(r'hover:underline" href="/product/[^"]+">([^<]+)', c)
        price = re.search(r'tabular-nums text-xl">‏?([\d,]+)', c)
        if not (slug and name and price): continue
        img = re.search(r'<img[^>]*src="([^"]+)"', c)
        brand = re.search(r'text-xs font-semibold">([^<]+)</span>', c)
        old = re.search(r'line-through">‏?([\d,]+)', c)
        disc = re.search(r'tabular-nums">(\d+)<!-- -->%-', c)
        yield {'slug': slug.group(1), 'name': html.unescape(name.group(1)).strip(), 'brand': html.unescape(brand.group(1)).strip() if brand else '',
               'price': int(price.group(1).replace(',', '')), 'old': int(old.group(1).replace(',', '')) if old else None,
               'disc': int(disc.group(1)) if disc else 0, 'image': html.unescape(img.group(1)) if img else '', 'soldOut': 'אזל' in c}

def kind_of(name, zone):
    for word, kind in KIND_WORDS:
        if word in name: return kind
    return ZONE_KIND.get(zone, 'microwave')

def short_of(name, brand):
    words = name.replace('–', ' ').replace('—', ' ').split()
    typ = next((w for w in words if re.search(r'[֐-׿]', w)), '')
    latin_brand = next((w for w in words if brand and w.lower() == brand.lower()), None) or (brand if re.search(r'[A-Za-z]', brand) else '')
    extra = next((w for w in words if re.search(r'\d', w) and len(w) <= 9 and w != typ), '')
    out = ' '.join(x for x in (typ, latin_brand or brand, extra) if x)
    return out[:28].strip()

def thumb(url):
    im = Image.open(io.BytesIO(get(url, binary=True))).convert('RGBA')
    im.thumbnail((200, 200))
    bg = Image.new('RGB', (224, 224), 'white')
    bg.paste(im, ((224 - im.width) // 2, (224 - im.height) // 2), im)
    buf = io.BytesIO(); bg.save(buf, 'JPEG', quality=70, optimize=True)
    return 'data:image/jpeg;base64,' + base64.b64encode(buf.getvalue()).decode()

def main():
    cfg = json.loads((ROOT / 'products.config.json').read_text(encoding='utf-8'))
    if '--offline' in sys.argv:
        prev = (OUT / 'index.html').read_text(encoding='utf-8')
        data = json.loads(re.search(r'/\*__PRODUCTS__\*/(.*?)/\*__END__\*/', prev, re.S).group(1))
        return write(data, cfg)
    if '--add-deals' in sys.argv:
        prev = (OUT / 'index.html').read_text(encoding='utf-8')
        data = json.loads(re.search(r'/\*__PRODUCTS__\*/(.*?)/\*__END__\*/', prev, re.S).group(1))
        cfg = dict(cfg, categories=[c for c in cfg['categories'] if c['zone'] == 'deal'])
        fresh = collect(cfg, set())
        slugs = {i['slug'] for i in fresh['items']}
        data['items'] = fresh['items'] + [i for i in data['items'] if i['slug'] not in slugs]
        data['cats'] = fresh['cats'] + [c for c in data.get('cats', []) if c['zone'] != 'deal']
        return write(data, json.loads((ROOT / 'products.config.json').read_text(encoding='utf-8')))
    data = collect(cfg, set())
    if len(data['items']) < 20: sys.exit('too few products fetched, not writing')
    write(data, cfg)

def collect(cfg, seen):
    per = cfg.get('per_category', 7)
    items, cats = [], []
    for cat in cfg['categories']:
        try:
            page = get(SITE + cat.get('path', f"/category/{cat['slug']}"))
        except Exception as e:
            print(f"  skip category {cat['slug']}: {e}", file=sys.stderr); continue
        cats.append({'slug': cat['slug'], 'zone': cat['zone'], 'name': cat['name']})
        got, want = 0, cat.get('count', per)
        for c in cards(page):
            if got >= want: break
            if c['soldOut'] or c['slug'] in seen: continue
            img = ''
            if c['image']:
                try: img = thumb(c['image'])
                except Exception as e: print(f"  photo failed for {c['slug']}: {e}", file=sys.stderr)
            seen.add(c['slug']); got += 1
            items.append({'slug': c['slug'], 'url': f"{SITE}/product/{c['slug']}", 'cat': cat['slug'], 'kind': kind_of(c['name'], cat['zone']),
                          'short': short_of(c['name'], c['brand']), 'name': c['name'], 'brand': c['brand'], 'price': c['price'],
                          'old': c['old'] if c['old'] and c['old'] > c['price'] else None, 'disc': c['disc'] if c['old'] else 0, 'img': img})
            time.sleep(.15)
        print(f"  {cat['name']:<24} {got} מוצרים")
    return {'updated': datetime.date.today().isoformat(), 'cats': cats, 'items': items}

def write(data, cfg):
    src = (ROOT / 'game.html').read_text(encoding='utf-8')
    frag = re.sub(r'/\*__PRODUCTS__\*/.*?/\*__END__\*/', lambda m: '/*__PRODUCTS__*/' + json.dumps(data, ensure_ascii=False) + '/*__END__*/', src, flags=re.S)
    frag = re.sub(r'/\*__CHECKOUT__\*/.*?/\*__END__\*/', lambda m: '/*__CHECKOUT__*/' + json.dumps(cfg.get('checkout_url', '')) + '/*__END__*/', frag, flags=re.S)
    (ROOT / 'dist').mkdir(exist_ok=True)
    (ROOT / 'dist' / 'game.html').write_text(frag, encoding='utf-8')
    head = ('<!doctype html><html lang="he" dir="rtl"><head><meta charset="utf-8">'
            '<meta name="viewport" content="width=device-width,initial-scale=1,maximum-scale=1,user-scalable=no,viewport-fit=cover">'
            '<meta name="theme-color" content="#101530"><meta name="description" content="קניון BuyToday בתלת־ממד: משחקים, אוספים מוצרים אמיתיים וקונים באתר.">'
            '</head><body>')
    OUT.mkdir(parents=True, exist_ok=True)
    (OUT / 'index.html').write_text(head + frag + '</body></html>', encoding='utf-8')
    print(f"built {len(data['items'])} products -> public/store-game/index.html")

if __name__ == '__main__':
    main()
