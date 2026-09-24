#!/usr/bin/env python3
"""Builds the store game with a fresh snapshot of real BuyToday products.

Run occasionally (e.g. when deals change):  python3 store-game/build.py
It reads each product in products.config.json from buytoday.co.il once, shrinks
its photo, and bakes everything into the game file. Players never hit the site.

Outputs:
  public/store-game/index.html   standalone page (deploy anywhere)
  store-game/dist/game.html      fragment used for the Claude artifact preview
Only changed the game code?  python3 store-game/build.py --offline
reuses the products already baked into public/store-game/index.html.
Requires: Pillow (pip install pillow)
"""
import base64, html, io, json, re, sys, time, urllib.request, datetime, pathlib
from PIL import Image

ROOT = pathlib.Path(__file__).resolve().parent
SITE = 'https://buytoday.co.il'
UA = {'User-Agent': 'BuyToday-StoreGame-Builder/1.0'}

def get(url, binary=False, tries=4):
    for n in range(tries):
        try:
            req = urllib.request.Request(url, headers=UA)
            with urllib.request.urlopen(req, timeout=25) as r:
                data = r.read()
            return data if binary else data.decode('utf-8', 'replace')
        except Exception:
            if n == tries - 1: raise
            time.sleep(2 ** n)

def old_prices():
    """slug -> (old price, discount %) from the deal cards on the home and /deals pages."""
    out = {}
    for path in ('/deals', '/'):
        page = get(SITE + path)
        for card in page.split('class="group bg-card relative flex flex-col')[1:]:
            slug = re.search(r'href="/product/([^"]+)"', card)
            old = re.search(r'line-through">‏?([\d,]+)', card)
            disc = re.search(r'tabular-nums">(\d+)<!-- -->%-', card)
            if slug and old:
                out.setdefault(slug.group(1), (int(old.group(1).replace(',', '')), int(disc.group(1)) if disc else 0))
    return out

def product(slug):
    page = get(f'{SITE}/product/{slug}')
    for block in re.findall(r'<script type="application/ld\+json"[^>]*>(.*?)</script>', page, re.S):
        d = json.loads(block)
        if d.get('@type') == 'Product':
            img = d.get('image') or []
            return {
                'name': html.unescape(d['name']).strip(),
                'brand': (d.get('brand') or {}).get('name', ''),
                'price': round(float(d['offers']['price'])),
                'inStock': 'InStock' in d['offers'].get('availability', ''),
                'images': img if isinstance(img, list) else [img],
            }
    raise ValueError(f'no product data for {slug}')

def thumb(url):
    im = Image.open(io.BytesIO(get(url, binary=True))).convert('RGBA')
    im.thumbnail((240, 240))
    bg = Image.new('RGB', (256, 256), 'white')
    bg.paste(im, ((256 - im.width) // 2, (256 - im.height) // 2), im)
    buf = io.BytesIO(); bg.save(buf, 'JPEG', quality=72, optimize=True)
    return 'data:image/jpeg;base64,' + base64.b64encode(buf.getvalue()).decode()

def main():
    out = ROOT.parent / 'public' / 'store-game'
    if '--offline' in sys.argv:
        prev = (out / 'index.html').read_text(encoding='utf-8')
        data = json.loads(re.search(r'/\*__PRODUCTS__\*/(.*?)/\*__END__\*/', prev, re.S).group(1))
        return write(data, out)
    cfg = json.loads((ROOT / 'products.config.json').read_text(encoding='utf-8'))
    olds = old_prices()
    items = []
    for c in cfg['products']:
        try:
            p = product(c['slug'])
        except Exception as e:  # keep building if one product disappears from the site
            print(f'  skip {c["slug"]}: {e}', file=sys.stderr); continue
        img = ''
        for u in p['images']:  # first photo that downloads; the game draws a plain box without one
            try: img = thumb(u); break
            except Exception as e: print(f'  photo failed {u[:70]}: {e}', file=sys.stderr)
        old, disc = olds.get(c['slug'], (None, 0))
        if old and old <= p['price']: old, disc = None, 0
        items.append({'slug': c['slug'], 'url': f'{SITE}/product/{c["slug"]}', 'kind': c['kind'], 'short': c['short'],
                      'name': p['name'], 'brand': p['brand'], 'price': p['price'], 'old': old, 'disc': disc,
                      'inStock': p['inStock'], 'img': img})
        print(f'  {c["short"]:<24} ₪{p["price"]:>6,}' + (f'  (במקום ₪{old:,}, -{disc}%)' if old else ''))
        time.sleep(.3)
    if len(items) < 6: sys.exit('too few products fetched, not writing')
    write({'updated': datetime.date.today().isoformat(), 'items': items}, out)

def write(data, out):
    src = (ROOT / 'game.html').read_text(encoding='utf-8')
    frag = re.sub(r'/\*__PRODUCTS__\*/.*?/\*__END__\*/', lambda m: '/*__PRODUCTS__*/' + json.dumps(data, ensure_ascii=False) + '/*__END__*/', src, flags=re.S)
    (ROOT / 'dist').mkdir(exist_ok=True)
    (ROOT / 'dist' / 'game.html').write_text(frag, encoding='utf-8')
    head = ('<!doctype html><html lang="he" dir="rtl"><head><meta charset="utf-8">'
            '<meta name="viewport" content="width=device-width,initial-scale=1,maximum-scale=1,user-scalable=no,viewport-fit=cover">'
            '<meta name="theme-color" content="#141a30"></head><body>')
    out.mkdir(parents=True, exist_ok=True)
    (out / 'index.html').write_text(head + frag + '</body></html>', encoding='utf-8')
    print(f'built {len(data["items"])} products -> public/store-game/index.html')

if __name__ == '__main__':
    main()
