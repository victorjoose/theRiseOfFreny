# Builds Freny's slash and crouch poses out of her own frames. The sword itself isn't drawn into
# them: FrenyMotion holds the item's sword sprite in her hand (the normal sword, or the long sword
# while it lasts), so the cut always shows the weapon she has.
#   Player slash.png              the cut, without the sword: melee frame 2 (the sword arm raised
#                                 forward; the item icons are drawn at that same 45 degrees)
#   Resources/FrenyPoses/poses    poses FrenyMotion swaps in over the Animator's frame:
#                                 crouch, crouch ranged, air ranged, climb
#   Resources/FrenyPoses/swords   Items 100x100_1 and _3, pivoted at the grip
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


# melee frame 2: the fist at (35-37, 32-34), the raised sword above-right of it
ARM_RAISED = take(frame('Player melee', 2), lambda x, y: not (x >= 37 and y <= 32))

CROUCH = frame('Player crouched', 0)       # the hand sticks out at (33-34, 41-42)
RANGED = [frame('Player ranged', i) for i in range(3)]   # the hand at (34, 33-34)
GUN = [take(f, lambda x, y: (x >= 35 and 27 <= y <= 36) or (x == 34 and 35 <= y <= 36)) for f in RANGED]


def slash_frames():
    return [ARM_RAISED] * 5


def poses():
    return [('Freny crouch_0', CROUCH)] \
      + [('Freny crouch ranged_%d' % i, {**CROUCH, **moved(g, 0, 8)}) for i, g in enumerate(GUN)] \
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


# the item icons (Items 100x100.png, drawn 45 degrees up-right) and their grip, in art pixels
# from the top-left of the drawing
SWORDS = [('Freny sword', 1, (1, 10)), ('Freny great sword', 3, (2.5, 15.5))]


def swords():
    items = Image.open(os.path.join(ROOT, 'Assets/Sprites/Items/Items 100x100.png')).convert('RGBA')
    sheet = Image.new('RGBA', (100 * len(SWORDS), 100))
    sprites = []
    for i, (name, index, (gx, gy)) in enumerate(SWORDS):
        cell = items.crop((index * 100, 0, index * 100 + 100, 100))
        left, top = cell.getbbox()[:2]
        sheet.paste(cell, (i * 100, 0))
        pivot = ((left + (gx + 0.5) * PX) / 100, 1 - (top + (gy + 0.5) * PX) / 100)
        sprites.append((name, i * 100, 0, 100, 100, pivot))
    sheet.save(os.path.join(OUT, 'swords.png'))
    random.seed(1)
    write_meta(ROOT, os.path.join(OUT, 'swords.png.meta'), sprites)


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
    swords()


main()
