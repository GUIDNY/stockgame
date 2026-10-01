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
Only refresh the full department catalogues?  python3 store-game/build.py --catalog

Besides the showroom products baked into the page, every department has a
catalogue kiosk listing the WHOLE category. That list is crawled from every
page of the category and written to public/store-game/catalog.json, which the
game fetches only when somebody opens a kiosk. Photos in it are the shop's own
URLs, loaded lazily as the list scrolls.

Outputs: public/store-game/index.html (deploy anywhere) and store-game/dist/game.html.
Requires: Pillow (pip install pillow)
"""
import base64, datetime, html, io, json, pathlib, re, sys, time, urllib.request
from PIL import Image, ImageDraw, ImageFilter

ROOT = pathlib.Path(__file__).resolve().parent
OUT = ROOT.parent / 'public' / 'store-game'
SITE = 'https://buytoday.co.il'
UA = {'User-Agent': 'BuyToday-StoreGame-Builder/2.0'}

# Which 3D model stands on the pedestal: the word that comes FIRST in the product name wins
# (the product type leads the name; a later word like "מסך" in a fridge's features must not),
# and at the same spot the longer phrase wins ("מקרן חום" is a heater, "מקרן" a projector).
KIND_WORDS = [
    ('מייבש כביסה', 'washer'), ('מייבש שיער', 'hairdryer'), ('תנור חימום', 'heater'), ('מפזר חום', 'heater'),
    ('מקרן חום', 'heater'), ('מקרן חימום', 'heater'), ('קמין', 'heater'), ('קרש גיהוץ', 'iron'), ('טאבון', 'toasteroven'), ('מעשנה', 'grill'),
    ('בידורית', 'speaker'), ('סאב', 'speaker'), ('וופר', 'speaker'), ('ברד', 'blender'), ('מיקסר', 'processor'), ('טרימר', 'shaver'),
    ('מזגנית', 'heater'), ('גיהוץ', 'iron'), ('תנור לאמבטיה', 'heater'), ('תנור אינפרא', 'heater'),
    ('מקרן', 'tv'), ('טלוויזיה', 'tv'), ('מסך', 'tv'), ('מתקן', 'tv'),
    ('רסיבר', 'receiver'), ('מגבר', 'receiver'), ('רמקול', 'speaker'), ('מקרן קול', 'speaker'), ('סאונד', 'speaker'), ('כבל', 'receiver'),
    ('קומקום', 'kettle'), ('אספרסו', 'coffee'), ('קפה', 'coffee'), ('בלנדר', 'blender'), ('מעבד', 'processor'), ('מסחטת', 'blender'),
    ('אייר פרייר', 'airfryer'), ('טיגון', 'airfryer'), ('גריל', 'grill'), ('טוסטר', 'toasteroven'), ('מטחנת', 'processor'),
    ('מיקרוגל', 'microwave'), ('כיריים', 'cooktop'), ('קולט', 'hood'), ('תנור', 'oven'),
    ('מקרר', 'fridge'), ('מקפיא', 'fridge'), ('יין', 'fridge'), ('כביסה', 'washer'), ('מדיח', 'washer'), ('מייבש', 'washer'),
    ('שואב', 'stickvac'), ('מגהץ', 'iron'), ('מזגן', 'ac'), ('מאוורר', 'fan'), ('רדיאטור', 'heater'), ('קטלן', 'fan'),
    ('פן', 'hairdryer'), ('מחליק', 'hairdryer'), ('גילוח', 'shaver'), ('תספורת', 'shaver'), ('ברז', 'fridge'), ('בר מים', 'fridge'),
]
# Accessories are sold on the site, but on a showroom pedestal a speaker stand would stand there as a speaker.
ACCESSORY_WORDS = ('אביזר', 'סטנד', 'חצובה', 'מעמד', 'מתקן תלייה', 'זרוע', 'כיסוי', 'מתאם', 'נוזל', 'מסנן', 'פילטר', 'שלט רחוק', 'כבל')

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

def base_name(name):
    """The name without its colour tail ("... - לבן"), so two colours of one product count once."""
    return re.sub(r'\s+[-–—]\s+[^-–—]*$', '', name).strip()

def kind_of(name, zone):
    hits = [(name.find(w), -len(w), k) for w, k in KIND_WORDS if w in name]
    return min(hits)[2] if hits else ZONE_KIND.get(zone, 'microwave')

def short_of(name, brand):
    words = name.replace('–', ' ').replace('—', ' ').split()
    typ = next((w for w in words if re.search(r'[֐-׿]', w)), '')
    latin_brand = next((w for w in words if brand and w.lower() == brand.lower()), None) or (brand if re.search(r'[A-Za-z]', brand) else '')
    extra = next((w for w in words if re.search(r'\d', w) and len(w) <= 9 and w != typ), '')
    out = ' '.join(x for x in (typ, latin_brand or brand, extra) if x)
    return out[:28].strip()

def thumb(url, raw=None):
    im = Image.open(io.BytesIO(raw or get(url, binary=True))).convert('RGBA')
    im.thumbnail((200, 200))
    bg = Image.new('RGB', (224, 224), 'white')
    bg.paste(im, ((224 - im.width) // 2, (224 - im.height) // 2), im)
    buf = io.BytesIO(); bg.save(buf, 'JPEG', quality=70, optimize=True)
    return 'data:image/jpeg;base64,' + base64.b64encode(buf.getvalue()).decode()

def cutout(raw):
    """The product on its own: the photo's plain white backdrop cut away, so the real product can stand on
    its pedestal instead of a look-alike model. None when it cannot be cut cleanly (a photo in a room, a
    grey studio backdrop, a product touching every edge); that product keeps its 3D model."""
    im = Image.open(io.BytesIO(raw)).convert('RGBA')
    bg = Image.new('RGBA', im.size, (255, 255, 255, 255)); bg.alpha_composite(im); im = bg.convert('RGB')
    im.thumbnail((240, 240))
    W, H = im.size; px = im.load()
    border = [px[x, 0] for x in range(W)] + [px[x, H - 1] for x in range(W)] + [px[0, y] for y in range(H)] + [px[W - 1, y] for y in range(H)]
    if sum(1 for p in border if min(p) >= 232) / len(border) < .9: return None
    seed = px[0, 0] if min(px[0, 0]) >= 232 else (255, 255, 255)
    pad = Image.new('RGB', (W + 4, H + 4), seed); pad.paste(im, (2, 2))
    ImageDraw.floodfill(pad, (0, 0), (255, 0, 255), thresh=26)
    mask = Image.new('L', pad.size, 255); mp = mask.load(); pp = pad.load()
    for y in range(pad.size[1]):
        for x in range(pad.size[0]):
            if pp[x, y] == (255, 0, 255): mp[x, y] = 0
    mask = mask.crop((2, 2, W + 2, H + 2)).filter(ImageFilter.MinFilter(3)).filter(ImageFilter.GaussianBlur(.8))
    bb = mask.point(lambda v: 255 if v > 40 else 0).getbbox()
    if not bb or (bb[2] - bb[0] >= W - 2 and bb[3] - bb[1] >= H - 2): return None
    out = im.convert('RGBA'); out.putalpha(mask); out = out.crop(bb)
    buf = io.BytesIO(); out.save(buf, 'WEBP', quality=78, method=6)
    return 'data:image/webp;base64,' + base64.b64encode(buf.getvalue()).decode(), round(out.width / out.height, 3)

def crawl_catalog(cfg):
    """Every product of every category, all pages, text only."""
    out = {}
    for cat in cfg['categories']:
        base = SITE + cat.get('path', f"/category/{cat['slug']}")
        rows, seen = [], set()
        for page in range(1, 60):
            try:
                pg = get(base + (f'?page={page}' if page > 1 else ''))
            except Exception as e:
                print(f"  catalog {cat['slug']} page {page}: {e}", file=sys.stderr); break
            new = [c for c in cards(pg) if c['slug'] not in seen]
            if not new: break
            for c in new:
                seen.add(c['slug'])
                img = c['image']
                if img.startswith('/'): img = SITE + img
                rows.append([c['slug'], c['name'], c['brand'], c['price'], c['old'] if c['old'] and c['old'] > c['price'] else 0,
                             c['disc'] if c['old'] else 0, img, 1 if c['soldOut'] else 0])
            if f'page={page + 1}' not in pg: break
            time.sleep(.25)
        out[cat['slug']] = rows
        print(f"  catalogue {cat['name']:<24} {len(rows)} מוצרים")
    return {'updated': datetime.date.today().isoformat(), 'cats': out}

def write_catalog(cat):
    OUT.mkdir(parents=True, exist_ok=True)
    (OUT / 'catalog.json').write_text(json.dumps(cat, ensure_ascii=False, separators=(',', ':')), encoding='utf-8')

def with_totals(data):
    """How many products each department's kiosk will list, so its screen can say so before the list loads."""
    try: cat = json.loads((OUT / 'catalog.json').read_text(encoding='utf-8'))['cats']
    except Exception: return data
    for c in data.get('cats', []):
        if c['slug'] in cat: c['total'] = len(cat[c['slug']])
    return data

def main():
    cfg = json.loads((ROOT / 'products.config.json').read_text(encoding='utf-8'))
    if '--catalog' in sys.argv:
        write_catalog(crawl_catalog(cfg))
        prev = (OUT / 'index.html').read_text(encoding='utf-8')
        data = json.loads(re.search(r'/\*__PRODUCTS__\*/(.*?)/\*__END__\*/', prev, re.S).group(1))
        return write(data, cfg)
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
    write_catalog(crawl_catalog(cfg))
    write(data, cfg)

def collect(cfg, seen):
    """The showroom: the first products of each category that are in stock AND whose photo
    really downloads (some suppliers block servers, and a pedestal without a photo looks broken)."""
    per = cfg.get('per_category', 7)
    items, cats = [], []
    for cat in cfg['categories']:
        base = SITE + cat.get('path', f"/category/{cat['slug']}")
        got, want, skipped = 0, cat.get('count', per), 0
        for n in range(1, 6):
            if got >= want: break
            try:
                page = get(base + (f'?page={n}' if n > 1 else ''))
            except Exception as e:
                print(f"  skip {cat['slug']} page {n}: {e}", file=sys.stderr); break
            if n == 1: cats.append({'slug': cat['slug'], 'zone': cat['zone'], 'name': cat['name']})
            for c in cards(page):
                if got >= want: break
                if c['soldOut'] or c['slug'] in seen or not c['image']: continue
                if base_name(c['name']) in {base_name(i['name']) for i in items} or any(w in c['name'] for w in ACCESSORY_WORDS) \
                        or not any(w in c['name'] for w, _ in KIND_WORDS):
                    skipped += 1; print(f"  no 3D model fits {c['slug']}: {c['name'][:50]}", file=sys.stderr); continue
                src = SITE + c['image'] if c['image'].startswith('/') else c['image']
                try: raw = get(src, binary=True); img = thumb(src, raw)
                except Exception as e:
                    skipped += 1; print(f"  no photo for {c['slug']}: {e}", file=sys.stderr); continue
                try: cut = cutout(raw)
                except Exception as e: cut = None; print(f"  no cut-out for {c['slug']}: {e}", file=sys.stderr)
                seen.add(c['slug']); got += 1
                items.append({'slug': c['slug'], 'url': f"{SITE}/product/{c['slug']}", 'cat': cat['slug'], 'kind': kind_of(c['name'], cat['zone']),
                              'short': short_of(c['name'], c['brand']), 'name': c['name'], 'brand': c['brand'], 'price': c['price'],
                              'old': c['old'] if c['old'] and c['old'] > c['price'] else None, 'disc': c['disc'] if c['old'] else 0, 'img': img,
                              **({'cut': cut[0], 'ca': cut[1]} if cut else {})})
                time.sleep(.15)
            if f'page={n + 1}' not in page: break
        print(f"  {cat['name']:<24} {got} מוצרים" + (f" (דילגתי על {skipped}: בלי תמונה, אביזר, כפול או בלי דגם תלת־ממד מתאים)" if skipped else ''))
    return {'updated': datetime.date.today().isoformat(), 'cats': cats, 'items': items}

def write(data, cfg):
    data = with_totals(data)
    src = (ROOT / 'game.html').read_text(encoding='utf-8')
    frag = re.sub(r'/\*__PRODUCTS__\*/.*?/\*__END__\*/', lambda m: '/*__PRODUCTS__*/' + json.dumps(data, ensure_ascii=False) + '/*__END__*/', src, flags=re.S)
    frag = re.sub(r'/\*__CHECKOUT__\*/.*?/\*__END__\*/', lambda m: '/*__CHECKOUT__*/' + json.dumps(cfg.get('checkout_url', '')) + '/*__END__*/', frag, flags=re.S)
    on = cfg.get('online') or {}
    online = {'url': on['url'], 'key': on['key']} if on.get('enabled') and on.get('url') and on.get('key') else None
    def with_online(v): return re.sub(r'/\*__ONLINE__\*/.*?/\*__END__\*/', lambda m: '/*__ONLINE__*/' + json.dumps(v) + '/*__END__*/', frag, flags=re.S)
    (ROOT / 'dist').mkdir(exist_ok=True)
    # the artifact copy stays single-player: its sandbox would refuse the socket anyway
    (ROOT / 'dist' / 'game.html').write_text(with_online(None), encoding='utf-8')
    frag = with_online(online)
    head = ('<!doctype html><html lang="he" dir="rtl"><head><meta charset="utf-8">'
            '<meta name="viewport" content="width=device-width,initial-scale=1,maximum-scale=1,user-scalable=no,viewport-fit=cover">'
            '<meta name="theme-color" content="#101530"><meta name="description" content="קניון BuyToday בתלת־ממד: בוחרים דמות, מטיילים בין 10 מחלקות עם מוצרים אמיתיים וקונים באתר.">'
            '<meta property="og:type" content="website"><meta property="og:site_name" content="BuyToday">'
            '<meta property="og:title" content="הקניון של BuyToday"><meta property="og:description" content="בוחרים דמות ומטיילים בקניון תלת־ממדי עם המוצרים והמבצעים האמיתיים של BuyToday.">'
            '<meta property="og:url" content="https://play.buytoday.co.il/"><meta property="og:image" content="https://play.buytoday.co.il/og.jpg">'
            '<meta property="og:image:width" content="1200"><meta property="og:image:height" content="630"><meta name="twitter:card" content="summary_large_image">'
            '<link rel="icon" href="https://buytoday.co.il/favicon.ico">'
            # Safari's own app banner on iPhone: "פתח" when the Buy Today app is installed, "הורד" when it is not.
            # app-argument is this page, so a version of the app that handles it opens straight into the mall.
            '<meta name="apple-itunes-app" content="app-id=6810211732, app-argument=https://play.buytoday.co.il/">'
            '</head><body>')
    OUT.mkdir(parents=True, exist_ok=True)
    (OUT / 'index.html').write_text(head + frag + '</body></html>', encoding='utf-8')
    print(f"built {len(data['items'])} products -> public/store-game/index.html")

if __name__ == '__main__':
    main()
