"""The Ciclope's vomit puddle: 3 frames of a lumpy green heap with bubbles and a shine.

Writes Assets/Sprites/Characters/Mobs/Cyclops/Resources/Cyclops/Vomit Puddle_<n>.png
(96x20, imported at 32 px a unit, pivot near the bottom). Run from the repo root.
"""
import math
import random

from PIL import Image

W, H = 96, 20
OUTLINE = (34, 52, 20, 255)
DARK = (70, 110, 30, 255)
MID = (110, 170, 40, 255)
LIGHT = (160, 215, 70, 255)
SHINE = (215, 245, 140, 255)
BUBBLE = (190, 235, 110, 255)
OUT = "Assets/Sprites/Characters/Mobs/Cyclops/Resources/Cyclops/"


def top(x, phase):
    t = x / (W - 1)
    base = H * 0.75 * math.sin(math.pi * t) ** 0.6
    return max(0.0, base + 1.2 * math.sin(x * 0.35 + phase) + 0.8 * math.sin(x * 0.13 + phase * 0.5))


def main():
    random.seed(7)
    chunks = [(random.randint(8, W - 8), random.randint(2, 6)) for _ in range(7)]
    for f in range(3):
        phase = f * 2.1
        im = Image.new("RGBA", (W, H), (0, 0, 0, 0))
        px = im.load()
        heights = [int(round(top(x, phase))) for x in range(W)]
        for x, h in enumerate(heights):
            for k in range(h):
                rel = k / max(1, h)
                px[x, H - 1 - k] = DARK if rel < 0.35 else MID if rel < 0.8 else LIGHT
            if h > 0:
                px[x, H - h] = OUTLINE if h < 2 else SHINE if (x + f * 3) % 11 in (0, 1) else LIGHT
                px[x, H - 1] = OUTLINE
        for x, h in enumerate(heights):
            edge = x == 0 or x == W - 1 or heights[x - 1] == 0 or heights[x + 1] == 0
            if h > 0 and edge:
                for k in range(h):
                    px[x, H - 1 - k] = OUTLINE
        for cx, cy in chunks:
            if heights[cx] > cy + 2:
                px[cx, H - 1 - cy] = DARK
                px[cx + 1, H - 1 - cy] = DARK
        for b in range(4):
            bx = (13 + b * 23 + f * 5) % (W - 10) + 5
            h = heights[bx]
            if h > 4:
                by = H - 1 - int(h * 0.55) - (f + b) % 3
                for ox, oy in ((0, 0), (1, 0), (0, 1), (1, 1)):
                    px[bx + ox, by - oy] = BUBBLE
                px[bx, by - 1] = SHINE
        im.save(OUT + "Vomit Puddle_%d.png" % f)


if __name__ == "__main__":
    main()
