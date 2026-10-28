# Builds the recoloured copies of an enemy's sheets that a variant draws instead of the original
# frames (SpriteSkin swaps them in by sprite name, after the Animator picks a frame):
#   SorcererSupreme   the Sorcerer's purple (the eye on the hood and the mantle) turned red, and the
#                     rest of him in the violet the prefab used to tint him with
#   GolemRed          the Golem in red stone with glowing red eyes
#
#   python3 enemy_skins.py <repo root>
#
# Writes <the enemy's sprite folder>/Resources/EnemySkins/<skin>/<sheet>.png + .meta. Each .meta is
# the original's (same slicing, pivots and sprite names) under its own guid, kept on regeneration.
# Rerun it after changing any of the original sheets.
import os, re, sys, uuid
from PIL import Image

ROOT = sys.argv[1]
MOBS = os.path.join(ROOT, 'Assets/Sprites/Characters/Mobs')

# the palette both sheets share
DARK, SHADE, BODY = (24, 36, 52), (34, 44, 63), (47, 55, 80)
PURPLE, DEEP_PURPLE = (79, 64, 159), (54, 55, 120)
WHITE = (255, 255, 255)                     # the hit flash


def tint(rgb, by):
    return tuple(round(c * t) for c, t in zip(rgb, by))


SUPREME_TINT = (0.8, 0.55, 1.0)             # what Sorcerer_Supreme's SpriteRenderer colour was
SKINS = {
    'SorcererSupreme': ('Sorcerer', SUPREME_TINT, {
        PURPLE: (236, 48, 48),
        DEEP_PURPLE: (150, 28, 40),
    }),
    'GolemRed': ('Golem', None, {
        BODY: (92, 42, 48),
        SHADE: (68, 31, 38),
        DARK: (42, 21, 30),
        PURPLE: (255, 70, 40),
        DEEP_PURPLE: (190, 36, 30),
    }),
}


def write_folder_meta(path):
    if not os.path.exists(path + '.meta'):
        open(path + '.meta', 'w').write(
            f'fileFormatVersion: 2\nguid: {uuid.uuid4().hex}\nfolderAsset: yes\nDefaultImporter:\n'
            '  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n')


def recolor(src, dst, base_tint, swaps):
    im = Image.open(src).convert('RGBA')
    px = im.load()
    for y in range(im.height):
        for x in range(im.width):
            r, g, b, a = px[x, y]
            if a == 0:
                continue
            rgb = (r, g, b)
            if rgb in swaps:
                rgb = swaps[rgb]
            elif base_tint:
                rgb = tint(rgb, base_tint)
            px[x, y] = rgb + (a,)
    im.save(dst)


def copy_meta(src, dst):
    meta = open(src).read()
    guid = None
    if os.path.exists(dst):                 # keep the guid stable on regeneration
        guid = re.search(r'^guid: (\w+)', open(dst).read(), re.M).group(1)
    meta = re.sub(r'^guid: \w+', 'guid: ' + (guid or uuid.uuid4().hex), meta, count=1, flags=re.M)
    open(dst, 'w').write(meta)


for skin, (enemy, base_tint, swaps) in SKINS.items():
    folder = os.path.join(MOBS, enemy)
    out = os.path.join(folder, 'Resources', 'EnemySkins', skin)
    os.makedirs(out, exist_ok=True)
    for part in (os.path.join(folder, 'Resources'), os.path.join(folder, 'Resources', 'EnemySkins'), out):
        write_folder_meta(part)
    for name in sorted(os.listdir(folder)):
        if name.endswith('.png'):
            recolor(os.path.join(folder, name), os.path.join(out, name), base_tint, swaps)
            copy_meta(os.path.join(folder, name + '.meta'), os.path.join(out, name + '.meta'))
            print(skin, name)
