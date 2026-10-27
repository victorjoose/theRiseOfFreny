# Builds Freny's swappable helmets: for every frame of every sheet her Animator plays, finds the
# grey helmet (the pixels above the orange neck, flood-filled from the top of the figure) and
# repaints it as the Rage and Berserker helmets. Each helmet becomes one small sprite cropped to
# the head and pivoted so that, drawn on the body's transform, it sits exactly on the old helmet.
#
#   python3 freny_heads.py <repo root>
#
# Writes Assets/Sprites/Characters/Player/Resources/FrenyHeads/{rage,berserker}.png + .meta; FrenyMotion loads them by the
# body sprite's name. Frames with no helmet (white hit flash, death) get no head.
import os, re, sys, uuid, random
from PIL import Image

ROOT = sys.argv[1]
PLAYER = os.path.join(ROOT, 'Assets/Sprites/Characters/Player')
OUT = os.path.join(ROOT, 'Assets/Sprites/Characters/Player/Resources/FrenyHeads')
PX = 4                                   # art pixel size in texels
SHEETS = ['Player idle', 'Player walk', 'Player jump', 'Player jump ranged', 'Player hit',
          'Player crouched', 'Player dash', 'Player kick', 'Player ranged', 'Player melee long',
          'Player slash']

ORANGE = (202, 117, 56)
LIGHT, MID, SHADE, WHITE = (176, 162, 141), (129, 111, 109), (99, 86, 85), (239, 239, 233)
PLUM, DARK, BLACK = (69, 40, 60), (48, 27, 42), (26, 17, 25)
RED, DEEP_RED, PALE_RED = (208, 47, 47), (150, 32, 40), (240, 120, 105)

# the grey helmet's colours, repainted per helmet (anything else keeps its colour)
PALETTES = {
    'rage': {LIGHT: RED, MID: DEEP_RED, SHADE: PLUM, WHITE: PALE_RED},
    'berserker': {LIGHT: PLUM, MID: DARK, SHADE: BLACK, WHITE: MID},
}
ALIGN = {0: (.5, .5), 1: (0, 1), 2: (.5, 1), 3: (1, 1), 4: (0, .5), 5: (1, .5), 6: (0, 0), 7: (.5, 0), 8: (1, 0)}


def sprites_of(meta_path):
    meta = open(meta_path).read()
    sheet = meta[meta.index('  spriteSheet:'):]
    for block in re.split(r'\n    - serializedVersion: 2\n', sheet)[1:]:
        name = re.search(r'name: (.+)', block).group(1).strip()
        r = {k: float(re.search(r'\n        %s: ([\d.\-]+)' % k, block).group(1)) for k in ('x', 'y', 'width', 'height')}
        align = int(re.search(r'\n      alignment: (\d+)', block).group(1))
        if align == 9:
            p = re.search(r'pivot: \{x: ([\d.\-e]+), y: ([\d.\-e]+)\}', block)
            pivot = (float(p.group(1)), float(p.group(2)))
        else:
            pivot = ALIGN[align]
        yield name, r, pivot


def helmet(cell):
    """art-pixel coordinates (x, y down) of the helmet in a cell image, or None"""
    w, h = cell.width // PX, cell.height // PX
    px = lambda x, y: cell.getpixel((x * PX, y * PX))
    opaque = lambda x, y: px(x, y)[3] > 0
    grey = lambda x, y: opaque(x, y) and px(x, y)[:3] in (LIGHT, MID, SHADE, WHITE)
    orange = lambda x, y: opaque(x, y) and px(x, y)[:3] == ORANGE
    if len({px(x, y)[:3] for x in range(w) for y in range(h) if opaque(x, y)}) < 3:
        return None                                   # the white hit flash
    # the neck: the highest orange pixel with helmet right on top of it (hands are orange too,
    # but have coat or nothing above them)
    def top_near(x):   # highest opaque pixel within a head's width of column x
        return min((y for y in range(h) for c in range(max(0, x - 6), min(w, x + 7)) if opaque(c, y)), default=h)
    necks = [(y, x) for y in range(1, h) for x in range(w)
             if orange(x, y) and grey(x, y - 1) and y - top_near(x) <= 12]
    if necks:
        neck, xc = min(necks)
        inside = lambda x, y: y < neck and opaque(x, y)
        seed = (xc, neck - 1)
    else:
        # crouched: the helmet sits on the coat with no neck showing; take the grey block on top,
        # at most 8 rows tall and as wide as its upper rows
        top = next((y for y in range(h) if any(opaque(x, y) for x in range(w))), None)
        if top is None or not any(grey(x, top) for x in range(w)):
            return None
        xs = [x for y in range(top, top + 4) for x in range(w) if grey(x, y)]
        lo, hi = min(xs) - 1, max(xs) + 1
        inside = lambda x, y: top <= y < top + 8 and lo <= x <= hi and grey(x, y)
        seed = next((x, top) for x in range(w) if grey(x, top))
    seen, stack = {seed}, [seed]
    while stack:
        x, y = stack.pop()
        for nx, ny in ((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)):
            if 0 <= nx < w and 0 <= ny < h and (nx, ny) not in seen and inside(nx, ny):
                seen.add((nx, ny))
                stack.append((nx, ny))
    return seen if len(seen) >= 12 else None


def repaint(c, pal):
    """the helmet colour's counterpart; shades the old palette lacks go to the nearest by brightness"""
    if c in pal:
        return pal[c]
    lum = lambda k: 0.3 * k[0] + 0.59 * k[1] + 0.11 * k[2]
    return pal[min(pal, key=lambda k: abs(lum(k) - lum(c)))]


def berserker_extras(pixels):
    """horns curling out of both sides and a dark visor slit"""
    xs = [x for x, _ in pixels]; ys = [y for _, y in pixels]
    left, right, top, bottom = min(xs), max(xs), min(ys), max(ys)
    mid = (left + right) // 2
    extra = {}
    for side, edge in ((-1, left), (1, right)):
        for i, (dx, dy) in enumerate(((1, 3), (2, 2), (3, 1), (3, 0), (3, -1), (2, -2))):
            extra[(edge + side * dx, top + dy)] = LIGHT if i < 5 else WHITE
    for y in range(top + 3, bottom):
        extra[(mid, y)] = BLACK
    return extra


def main():
    os.makedirs(OUT, exist_ok=True)
    entries = []                       # (name, pixels {(x,y): rgb per helmet}, origin, pivot px)
    for sheet in SHEETS:
        img = Image.open(os.path.join(PLAYER, sheet + '.png')).convert('RGBA')
        for name, r, pivot in sprites_of(os.path.join(PLAYER, sheet + '.png.meta')):
            # meta rects are bottom-up; PIL is top-down
            x0, y0 = int(r['x']), int(img.height - r['y'] - r['height'])
            cell = img.crop((x0, y0, x0 + int(r['width']), y0 + int(r['height'])))
            head = helmet(cell)
            if head is None:
                continue
            per = {}
            for kind, pal in PALETTES.items():
                pix = {p: repaint(cell.getpixel((p[0] * PX, p[1] * PX))[:3], pal) for p in head}
                if kind == 'berserker':
                    pix.update(berserker_extras(head))
                per[kind] = pix
            entries.append((name, per, r, pivot))

    random.seed(1978)
    for kind in PALETTES:
        tiles = []
        for name, per, r, pivot in entries:
            pix = per[kind]
            xs = [x for x, _ in pix]; ys = [y for _, y in pix]
            bx, by = min(xs), min(ys)
            tw, th = max(xs) - bx + 1, max(ys) - by + 1
            tile = Image.new('RGBA', (tw * PX, th * PX))
            for (x, y), c in pix.items():
                tile.paste(c + (255,), ((x - bx) * PX, (y - by) * PX, (x - bx + 1) * PX, (y - by + 1) * PX))
            # the body sprite's pivot, in texels from the tile's bottom-left
            pvx = r['width'] * pivot[0] - bx * PX
            pvy = r['height'] * pivot[1] - (r['height'] - (by + th) * PX)
            tiles.append((name, tile, (pvx / tile.width, pvy / tile.height)))
        cols, cw, ch = 10, 96, 96
        rows = (len(tiles) + cols - 1) // cols
        atlas = Image.new('RGBA', (cols * cw, rows * ch))
        sprites = []
        for i, (name, tile, piv) in enumerate(tiles):
            cx, cy = (i % cols) * cw, (i // cols) * ch
            atlas.paste(tile, (cx, cy))
            sprites.append((name, cx, atlas.height - cy - tile.height, tile.width, tile.height, piv))
        atlas.save(os.path.join(OUT, kind + '.png'))
        write_meta(os.path.join(OUT, kind + '.png.meta'), sprites)
        print(kind, len(sprites), 'heads', atlas.size)


def write_meta(path, sprites):
    template = open(os.path.join(PLAYER, 'Player slash.png.meta')).read()
    guid = None
    if os.path.exists(path):                       # keep the guid stable on regeneration
        guid = re.search(r'^guid: (\w+)', open(path).read(), re.M).group(1)
    guid = guid or uuid.uuid4().hex
    ids = [random.randint(-2 ** 62, 2 ** 62) for _ in sprites]
    head, sheet = template.split('  spriteSheet:\n', 1)
    head = re.sub(r'^guid: \w+', 'guid: ' + guid, head, 1, flags=re.M)
    table = ''.join(f'  - first:\n      213: {i}\n    second: {s[0]}\n' for i, s in zip(ids, sprites))
    head = re.sub(r'  internalIDToNameTable:\n.*?(?=  externalObjects)', lambda m: '  internalIDToNameTable:\n' + table, head, flags=re.S)
    body = ''
    for i, (name, x, y, w, h, piv) in zip(ids, sprites):
        body += (f"    - serializedVersion: 2\n      name: {name}\n      rect:\n        serializedVersion: 2\n"
                 f"        x: {x}\n        y: {y}\n        width: {w}\n        height: {h}\n"
                 f"      alignment: 9\n      pivot: {{x: {piv[0]:.6f}, y: {piv[1]:.6f}}}\n"
                 f"      border: {{x: 0, y: 0, z: 0, w: 0}}\n      outline: []\n      physicsShape: []\n"
                 f"      tessellationDetail: 0\n      bones: []\n      spriteID: {uuid.uuid4().hex}\n"
                 f"      internalID: {i}\n      vertices: []\n      indices: \n      edges: []\n      weights: []\n")
    start = sheet.index('    sprites:\n') + len('    sprites:\n')
    rest = sheet[start:]
    tail = rest[rest.rindex('      weights: []\n') + len('      weights: []\n'):]
    tail = re.sub(r'    nameFileIdTable:\n(?:      .*\n)*', '    nameFileIdTable:\n' + ''.join(f'      {s[0]}: {i}\n' for i, s in zip(ids, sprites)), tail)
    open(path, 'w').write(head + '  spriteSheet:\n' + sheet[:start] + body + tail)


main()
