# Generates Assets/Sprites/Characters/Player/Player slash.png from "Player melee.png" (frame 2 body,
# blade removed) with a longer blade redrawn at each angle. Usage:
#   python3 freny_slash.py "Player melee.png" "Player slash.png" 25,0,0,0,0 15
# (a quick draw that ends with the blade held straight out; the slash effect in front is FrenyMotion's)
# Cells are 280x200 (70x50 art pixels at 4x); the .meta pivots them at x=100px like the 200px sheets.
# builds "Player slash.png": Freny's arm held out forward (melee frame 2 without its sword)
# and a longer blade redrawn at each angle of one forward cut
import math, sys
from PIL import Image
src, out = sys.argv[1], sys.argv[2]
ANGLES = [float(a) for a in sys.argv[3].split(',')]
L = float(sys.argv[4])
CW, CH, PX = 70, 50, 4            # cell in art pixels, art pixel size
im = Image.open(src).convert('RGBA')
f = im.crop((400, 0, 600, 200))
grid = [[f.getpixel((x*4, y*4)) for x in range(50)] for y in range(50)]
# erase the old blade and hilt: everything right of the hand and above its knuckles
for y in range(50):
    for x in range(50):
        if x >= 37 and y <= 32:
            grid[y][x] = (0, 0, 0, 0)
LIGHT=(176,162,141,255); MID=(129,111,109,255); WHITE=(239,239,233,255); DARK=(48,27,42,255); HILT=(69,40,60,255)
HX, HY = 37.0, 32.0              # grip, in art pixels (y down)
sheet = Image.new('RGBA', (CW*PX*len(ANGLES), CH*PX), (0,0,0,0))
for i, ang in enumerate(ANGLES):
    cell = [[(0,0,0,0)]*CW for _ in range(CH)]
    for y in range(50):
        for x in range(50):
            cell[y][x] = grid[y][x]
    a = math.radians(ang)
    dx, dy = math.cos(a), -math.sin(a)
    px, py = dy, -dx              # perpendicular, toward the blade's lower edge
    if px * 0 + py < 0: px, py = -px, -py
    def put(x, y, c, over=True):
        xi, yi = int(round(x)), int(round(y))
        if 0 <= xi < CW and 0 <= yi < CH and (over or cell[yi][xi][3] == 0):
            cell[yi][xi] = c
    t = 0.0
    while t <= L:
        x, y = HX + dx*t, HY + dy*t
        if t < 1.5:
            put(x, y, DARK)
        elif t < 2.5:
            put(x, y, HILT); put(x+px, y+py, HILT); put(x-px, y-py, HILT)   # crossguard
        else:
            tip = t > L - 1.5
            put(x+px*0.7, y+py*0.7, LIGHT, False)
            put(x-px*0.7, y-py*0.7, WHITE if tip else LIGHT, False)
            put(x, y, WHITE if tip else MID)
        t += 0.25
    tile = Image.new('RGBA', (CW, CH))
    for y in range(CH):
        for x in range(CW):
            tile.putpixel((x, y), cell[y][x])
    sheet.alpha_composite(tile.resize((CW*PX, CH*PX), Image.NEAREST), (i*CW*PX, 0))
sheet.save(out)
