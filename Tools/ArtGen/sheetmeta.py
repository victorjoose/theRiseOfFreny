# Writes a Multiple-mode sprite sheet .meta by hand (Unity isn't available here), using
# "Player slash.png.meta" as the template. Keeps the guid when the meta already exists.
import os, re, uuid, random

PLAYER_DIR = 'Assets/Sprites/Characters/Player'


def write_meta(root, path, sprites):
    """sprites: (name, x, y, w, h, (pivot x, pivot y)), rects bottom-up in texels"""
    template = open(os.path.join(root, PLAYER_DIR, 'Player slash.png.meta')).read()
    guid = None
    if os.path.exists(path):                       # keep the guid stable on regeneration
        guid = re.search(r'^guid: (\w+)', open(path).read(), re.M).group(1)
    guid = guid or uuid.uuid4().hex
    ids = [random.randint(-2 ** 62, 2 ** 62) for _ in sprites]
    head, sheet = template.split('  spriteSheet:\n', 1)
    head = re.sub(r'^guid: \w+', 'guid: ' + guid, head, count=1, flags=re.M)
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

