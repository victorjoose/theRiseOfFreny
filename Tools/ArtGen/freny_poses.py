# Builds Freny's slash and crouch poses out of her own frames. The sword itself isn't drawn into
# them: FrenyMotion holds the item's sword sprite in her hand (the normal sword, or the long sword
# while it lasts), so the cut always shows the weapon she has.
#   Player slash.png              the cut, without the sword: melee frame 2 (the sword arm raised
#                                 forward; the item icons are drawn at that same 45 degrees)
#   Resources/FrenyPoses/poses    poses FrenyMotion swaps in over the Animator's frame:
#                                 crouch, crouch ranged, air ranged, climb
#   Resources/FrenyPoses/swords   Items 100x100_1 and _3, pivoted at the grip
#   Resources/FrenyPoses/poses    also "Freny swing_<deg>": the sword arm drawn straight out at
#                                 -60..90 degrees (0 = forward, up is +) for the 3-hit combo
#   Resources/FrenyPoses/blades   both swords redrawn pixel by pixel every 15 degrees, -90..180,
#                                 pivoted at the grip ("Freny blade_<deg>", "Freny great blade_<deg>")
#   it also prints the fist of each swing pose for FrenyMotion.SwingGrips
#
#   python3 freny_poses.py <repo root>      then rerun freny_heads.py for their helmets
#
# Cells are 280x200 (70x50 art pixels at 4x) pivoted at x=100px, like the 200px sheets' centre.
import os, sys, math, random
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


# the swing poses: frame 2 without its sword arm, and the arm redrawn from the shoulder at an angle
COAT, COAT_SHADE, SKIN = (129, 111, 109, 255), (99, 86, 85, 255), (202, 117, 56, 255)
NO_ARM = take(frame('Player melee', 2), lambda x, y: not (x >= 31 and 29 <= y <= 36) and not (x >= 37 and y <= 32))
SHOULDER = (29.5, 31.5)
ARM = 5.5                       # shoulder to wrist, art pixels
SWING_ANGLES = list(range(-60, 91, 15))
BLADE_ANGLES = list(range(-90, 181, 15))


def swing_pose(deg):
    """the body with the sword arm pointing at deg; returns the pixels and the fist's centre"""
    pix = dict(NO_ARM)
    a = math.radians(deg)
    dx, dy = math.cos(a), -math.sin(a)
    nx, ny = -dy, dx                       # across the arm, towards its underside
    steps = int(ARM * 3)
    for k in range(steps + 1):
        t = k / steps * ARM
        cx, cy = SHOULDER[0] + dx * t, SHOULDER[1] + dy * t
        for off, col in ((-1, COAT), (0, COAT), (1, COAT_SHADE)):
            pix[(round(cx + nx * off * 0.9), round(cy + ny * off * 0.9))] = col
    fx, fy = SHOULDER[0] + dx * (ARM + 1.2), SHOULDER[1] + dy * (ARM + 1.2)
    left, top = round(fx - 0.5), round(fy - 0.5)
    for ox in (0, 1):
        for oy in (0, 1):
            pix[(left + ox, top + oy)] = SKIN
    return pix, (left + 1, top + 1)        # the 2x2 fist's centre, in art pixels


# the two swords as straight blades, grip at u = 0: (length, cross-section rows top to bottom,
# colours by u), so they can be drawn cleanly at any angle
WHITE, LIGHT, MID, PLUM, DARK, BLUE, GOLD = (239, 239, 233, 255), (176, 162, 141, 255), (129, 111, 109, 255), \
    (69, 40, 60, 255), (48, 27, 42, 255), (99, 155, 255, 255), (219, 165, 40, 255)


OUTLINE = (26, 17, 25, 255)


def blade_colour(great, u, j, length, width):
    """colour of the blade at u pixels from the grip, row j of width (0 = the top edge)"""
    if great:
        if u < 3:
            return GOLD if j == 1 else None                  # the grip
        if u < 4:
            return GOLD                                      # the guard, full width
        if u >= length - 1:
            return WHITE if j == 1 else None                 # the point
        return (WHITE, BLUE, MID)[j] if u > length - 4 and j == 0 else (LIGHT, BLUE, MID)[j]
    if u < 2:
        return DARK if j == 0 else None
    if u < 3:
        return PLUM
    if u >= length - 1:
        return WHITE if j == 0 else None
    return (WHITE if u > length - 4 else LIGHT, MID)[j]


def blade(great, deg):
    length, width = (24, 3) if great else (15, 2)
    a = math.radians(deg)
    dx, dy = math.cos(a), -math.sin(a)
    nx, ny = -dy, dx
    pix = {}
    for k in range(length * 2):
        u = k / 2
        for j in range(width):
            c = blade_colour(great, int(u), j, length, width)
            if c is None:
                continue
            off = j - (width - 1) / 2
            if great and int(u) == 3:
                off *= 1.8                                   # the guard sticks out
            pix[(math.floor(u * dx + off * nx + 0.5), math.floor(u * dy + off * ny + 0.5))] = c
    # a dark outline, so the blade reads against the helmet when it is taken back over his head
    for (x, y) in list(pix):
        for nb in ((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)):
            if nb not in pix:
                pix[nb] = OUTLINE
    return pix


def blades():
    """one row per sword, cells pivoted at the grip"""
    cell = 2 * 26 + 2
    cols = 10
    names = []
    for great in (False, True):
        for deg in BLADE_ANGLES:
            names.append((('Freny great blade_%d' if great else 'Freny blade_%d') % deg, blade(great, deg)))
    rows = (len(names) + cols - 1) // cols
    im = Image.new('RGBA', (cols * cell * PX, rows * cell * PX))
    sprites = []
    for i, (name, pix) in enumerate(names):
        cx, cy = (i % cols) * cell, (i // cols) * cell
        for (x, y), c in pix.items():
            px, py = cx + cell // 2 + x, cy + cell // 2 + y
            im.paste(c, (px * PX, py * PX, px * PX + PX, py * PX + PX))
        # the grip pixel's centre is the pivot
        sprites.append((name, cx * PX, (rows - 1 - i // cols) * cell * PX, cell * PX, cell * PX,
                        ((cell // 2 + 0.5) / cell, 1 - (cell // 2 + 0.5) / cell)))
    im.save(os.path.join(OUT, 'blades.png'))
    random.seed(1515)
    write_meta(ROOT, os.path.join(OUT, 'blades.png.meta'), sprites)


def slash_frames():
    return [ARM_RAISED] * 5


def poses():
    return [('Freny crouch_0', CROUCH)] \
      + [('Freny crouch ranged_%d' % i, {**CROUCH, **moved(g, 0, 8)}) for i, g in enumerate(GUN)] \
      + [('Freny air ranged_%d' % i, frame('Player jump ranged', i)) for i in range(5)] \
      + [('Freny climb_%d' % i, frame('Player climb', i)) for i in range(2)] \
      + [('Freny swing_%d' % d, swing_pose(d)[0]) for d in SWING_ANGLES]


def draw(pix):
    cell = Image.new('RGBA', (CW * PX, CH * PX))
    for (x, y), c in pix.items():
        if 0 <= x < CW and 0 <= y < CH:
            cell.paste(c, (x * PX, y * PX, x * PX + PX, y * PX + PX))
    return cell


GRID = 7          # cells per row: 7 x 280 px stays under Unity's 2048 px texture size


def sheet(cells, grid=None):
    cols = grid or len(cells)
    rows = (len(cells) + cols - 1) // cols
    im = Image.new('RGBA', (CW * PX * cols, CH * PX * rows))
    for i, pix in enumerate(cells):
        im.paste(draw(pix), ((i % cols) * CW * PX, (i // cols) * CH * PX))
    return im


def rect(i, count, grid):
    """cell i's rect, bottom-up as Unity wants it"""
    rows = (count + grid - 1) // grid
    return (i % grid) * CW * PX, (rows - 1 - i // grid) * CH * PX, CW * PX, CH * PX


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
    sheet([p for _, p in named], GRID).save(os.path.join(OUT, 'poses.png'))
    random.seed(2026)
    write_meta(ROOT, os.path.join(OUT, 'poses.png.meta'),
               [(n, *rect(i, len(named), GRID), PIVOT) for i, (n, _) in enumerate(named)])
    print('poses', len(named))
    swords()
    blades()
    # FrenyMotion.SwingGrips: each swing pose's fist in the Sprite's units (pivot at x = 25, y = 25 art px)
    grips = ['new Vector2(%.2ff, %.2ff)' % ((fx - 25) * PX / 100, (25 - fy) * PX / 100)
             for fx, fy in (swing_pose(d)[1] for d in SWING_ANGLES)]
    print('SwingGrips (%d..%d by 15):' % (SWING_ANGLES[0], SWING_ANGLES[-1]), ', '.join(grips))


main()
