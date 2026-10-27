# Builds Freny's sword and crouch poses out of her own frames, so the sword is always the one she
# already holds (no redrawn blade):
#   Player slash.png              the cut: melee frame 2 (sword raised, being drawn), then melee
#                                 frame 3 (sword straight out) with the blade made longer
#   Resources/FrenyPoses/poses    poses FrenyMotion swaps in over the Animator's frame:
#                                 crouch, crouch slash, crouch ranged, air ranged, climb
#
#   python3 freny_poses.py <repo root>      then rerun freny_heads.py for their helmets
#
# Cells are 280x200 (70x50 art pixels at 4x) pivoted at x=100px, like the 200px sheets' centre.
import os, sys, random
from PIL import Image
from sheetmeta import write_meta

ROOT = sys.argv[1]
PLAYER = os.path.join(ROOT, 'Assets/Sprites/Characters/Player')
OUT = os.path.join(PLAYER, 'Resources/FrenyPoses')
PX, CW, CH = 4, 70, 50
EXTEND = 6                 # art pixels added to the blade, for the reach José asked for
PIVOT = (100 / 280, 0.5)


def frame(sheet, index):
    """one 200x200 cell as {(x, y): rgba} in art pixels (y down)"""
    im = Image.open(os.path.join(PLAYER, sheet + '.png')).convert('RGBA')
    cell = im.crop((index * 200, 0, index * 200 + 200, 200))
    return {(x, y): cell.getpixel((x * PX, y * PX)) for x in range(50) for y in range(50)
            if cell.getpixel((x * PX, y * PX))[3] > 0}


def take(pix, keep):
    return {p: c for p, c in pix.items() if keep(*p)}


def moved(pix, dx, dy):
    return {(x + dx, y + dy): c for (x, y), c in pix.items()}


def longer(pix, at, rows):
    """the blade stretched by EXTEND: everything right of column `at` in those rows slides right
    and the gap repeats column `at`, so the blade keeps its own edge colours and tip"""
    out = {}
    for (x, y), c in pix.items():
        if y in rows and x > at:
            out[(x + EXTEND, y)] = c
        else:
            out[(x, y)] = c
    for (x, y), c in pix.items():
        if y in rows and x == at:
            for i in range(1, EXTEND + 1):
                out[(x + i, y)] = c
    return out


# melee frame 3: the hand at (31-32, 37-38), the sword everything from the crossguard (x 33) on,
# rows 36-39; melee frame 2: the hand at (35-37, 32-34), the raised sword above-right of it
MELEE_DRAW, MELEE_OUT = frame('Player melee', 2), frame('Player melee', 3)
SWORD_OUT = take(MELEE_OUT, lambda x, y: x >= 33 and 35 <= y <= 39)
SWORD_DRAW = take(MELEE_DRAW, lambda x, y: x >= 37 and y <= 32)
BLADE_ROWS = range(35, 40)
BLADE_AT = 40

CROUCH = frame('Player crouched', 0)       # the hand sticks out at (33-34, 41-42)
RANGED = [frame('Player ranged', i) for i in range(3)]   # the hand at (34, 33-34)
GUN = [take(f, lambda x, y: (x >= 35 and 27 <= y <= 36) or (x == 34 and 35 <= y <= 36)) for f in RANGED]


def slash_frames():
    out = [MELEE_DRAW]
    held = longer(MELEE_OUT, BLADE_AT, BLADE_ROWS)
    out += [held] * 4
    return out


def poses():
    crouch_sword = longer(SWORD_OUT, BLADE_AT, BLADE_ROWS)
    return [
        ('Freny crouch_0', CROUCH),
        ('Freny crouch slash_0', {**CROUCH, **moved(SWORD_DRAW, -2, 8)}),
        ('Freny crouch slash_1', {**CROUCH, **moved(crouch_sword, 2, 4)}),
    ] + [('Freny crouch ranged_%d' % i, {**CROUCH, **moved(g, 0, 8)}) for i, g in enumerate(GUN)] \
      + [('Freny air ranged_%d' % i, frame('Player jump ranged', i)) for i in range(5)] \
      + [('Freny climb_%d' % i, frame('Player climb', i)) for i in range(2)]


def draw(pix):
    cell = Image.new('RGBA', (CW * PX, CH * PX))
    for (x, y), c in pix.items():
        if 0 <= x < CW and 0 <= y < CH:
            cell.paste(c, (x * PX, y * PX, x * PX + PX, y * PX + PX))
    return cell


def sheet(cells):
    im = Image.new('RGBA', (CW * PX * len(cells), CH * PX))
    for i, pix in enumerate(cells):
        im.paste(draw(pix), (i * CW * PX, 0))
    return im


def main():
    # same layout as before, so "Player slash.png.meta" and the Melee Attack clip stay as they are
    sheet(slash_frames()).save(os.path.join(PLAYER, 'Player slash.png'))
    os.makedirs(OUT, exist_ok=True)
    named = poses()
    sheet([p for _, p in named]).save(os.path.join(OUT, 'poses.png'))
    random.seed(2026)
    write_meta(ROOT, os.path.join(OUT, 'poses.png.meta'),
               [(n, i * CW * PX, 0, CW * PX, CH * PX, PIVOT) for i, (n, _) in enumerate(named)])
    print('poses', len(named))


main()
