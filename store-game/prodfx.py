"""Sharper products with real relief, made offline, free.

For every product in the mall that stands as a cut-out photo: the original photo again at 512 px instead of
240, cut from its white backdrop the same way build.py does, and a depth map of it from Depth Anything V2
Small (Apache-2.0, run on the CPU). The game raises each cut-out into a relief from its depth map, lit by the
mall, so a product has volume and shading when walked round instead of being a picture on a card.

  <venv with torch + transformers>/bin/python store-game/prodfx.py      # writes public/store-game/prod/
Run it after build.py has refreshed the products; products already done are skipped (--force redoes them).
"""
import io, json, re, sys, urllib.request
from pathlib import Path
import numpy as np
from PIL import Image, ImageDraw, ImageFilter
from scipy import ndimage

ROOT = Path(__file__).resolve().parent
PUB = ROOT.parent / 'public' / 'store-game'
OUT = PUB / 'prod'
SIZE = 512          # the cut-out's longer side
DEPTH = 128         # the depth map's longer side

def get(url):
    req = urllib.request.Request(url, headers={'User-Agent': 'Mozilla/5.0 (BuyToday mall build)'})
    with urllib.request.urlopen(req, timeout=40) as r: return r.read()

def cutout(raw):
    """build.py's cut, at SIZE: flood the white backdrop from the corners, keep the rest. None if the photo
    has no clean white backdrop."""
    im = Image.open(io.BytesIO(raw)).convert('RGBA')
    bg = Image.new('RGBA', im.size, (255, 255, 255, 255)); bg.alpha_composite(im); im = bg.convert('RGB')
    im.thumbnail((SIZE * 2, SIZE * 2), Image.LANCZOS)
    W, H = im.size; px = im.load()
    border = [px[x, 0] for x in range(W)] + [px[x, H - 1] for x in range(W)] + [px[0, y] for y in range(H)] + [px[W - 1, y] for y in range(H)]
    if sum(1 for p in border if min(p) >= 232) / len(border) < .9: return None
    seed = px[0, 0] if min(px[0, 0]) >= 232 else (255, 255, 255)
    pad = Image.new('RGB', (W + 4, H + 4), seed); pad.paste(im, (2, 2))
    ImageDraw.floodfill(pad, (0, 0), (255, 0, 255), thresh=26)
    a = np.asarray(pad); m = ~((a[..., 0] == 255) & (a[..., 1] == 0) & (a[..., 2] == 255))
    mask = Image.fromarray((m * 255).astype(np.uint8)).crop((2, 2, W + 2, H + 2)).filter(ImageFilter.MinFilter(3)).filter(ImageFilter.GaussianBlur(1.2))
    bb = mask.point(lambda v: 255 if v > 40 else 0).getbbox()
    if not bb or (bb[2] - bb[0] >= W - 2 and bb[3] - bb[1] >= H - 2): return None
    out = im.convert('RGBA'); out.putalpha(mask); out = out.crop(bb)
    out.thumbnail((SIZE, SIZE), Image.LANCZOS)
    # no white fringe: every pixel that is not fully the product takes the colour of the nearest one that is
    a = np.asarray(out).copy(); solid = a[..., 3] > 230
    if solid.any():
        idx = ndimage.distance_transform_edt(~solid, return_distances=False, return_indices=True)
        a[..., :3] = a[..., :3][tuple(idx)]
    return Image.fromarray(a, 'RGBA')

def depth_of(est, rgba):
    """0 at the back of the product, 1 at its nearest point; outside the product the nearest edge value
    carries on, so the relief does not tear at the silhouette."""
    rgb = Image.new('RGB', rgba.size, 'white'); rgb.paste(rgba, mask=rgba.split()[3])
    d = np.asarray(est(rgb)['predicted_depth'].squeeze().cpu().numpy(), np.float32)   # larger = nearer
    d = np.asarray(Image.fromarray(d).resize(rgba.size, Image.BILINEAR))
    m = np.asarray(rgba.split()[3]) > 128
    if m.sum() < 50: return None
    lo, hi = np.percentile(d[m], 2), np.percentile(d[m], 98)
    d = np.clip((d - lo) / max(hi - lo, 1e-6), 0, 1)
    d = ndimage.gaussian_filter(d, 1.2)
    idx = ndimage.distance_transform_edt(~m, return_distances=False, return_indices=True)
    d = d[tuple(idx)]                                   # outside: the value of the nearest product pixel
    im = Image.fromarray((d * 255).astype(np.uint8))
    im.thumbnail((DEPTH, DEPTH), Image.LANCZOS)
    return im

def main():
    html = (PUB / 'index.html').read_text(encoding='utf-8')
    items = json.loads(re.search(r'/\*__PRODUCTS__\*/(.*?)/\*__END__\*/', html, re.S).group(1))['items']
    cat = json.loads((PUB / 'catalog.json').read_text(encoding='utf-8'))['cats']
    src = {x[0]: x[6] for arr in cat.values() for x in arr}
    OUT.mkdir(exist_ok=True)
    man_path = OUT / 'manifest.json'
    man = json.loads(man_path.read_text()) if man_path.exists() and '--force' not in sys.argv else {}
    from transformers import pipeline
    est = pipeline('depth-estimation', model='depth-anything/Depth-Anything-V2-Small-hf', device='cpu')
    for it in items:
        s = it['slug']
        if not it.get('cut') or s in man or s not in src: continue
        try:
            cut = cutout(get(src[s]))
            if cut is None: print('  no clean backdrop:', s); continue
            dep = depth_of(est, cut)
            if dep is None: print('  too small:', s); continue
            cut.save(OUT / f'{s}.webp', 'WEBP', quality=82, method=6)
            dep.save(OUT / f'{s}-d.png', optimize=True)
            man[s] = {'ca': round(cut.width / cut.height, 3)}
            print('  ok', s, cut.size)
        except Exception as e:
            print('  failed', s, e)
    man_path.write_text(json.dumps(man, indent=0, sort_keys=True))
    print(len(man), 'products with relief')

if __name__ == '__main__':
    main()
