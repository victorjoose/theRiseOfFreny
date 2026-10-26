# 2-4 "As Galerias": two parallel tunnels. The lower one is the fight; the upper one, reached by
# three climbs, is quieter and holds the coins and a chest. The exit is at the top right.
from levelkit import Grid, zigzag
W,H=116,40
OX,OY=-58,-20
SCENE_DIR='Assets/Scenes/Campaign'
TINT=(0.62,0.9,0.86)
g=Grid(W,H)
g.carve(0,6,112,12); g.open_edge([(0,y) for y in range(6,10)])    # lower tunnel, floor 5
g.carve(8,26,115,32); g.open_edge([(W-1,y) for y in range(26,30)])  # upper tunnel, floor 25
# three climbs between them (x ranges), each a short zigzag up to row 25
for x0 in (18,56,94):
    g.carve(x0,6,x0+9,24)
    zigzag(g,x0,x0+9,7,23,width=5,step=5)
    g.carve(x0+3,25,x0+4,25)                         # a 2-wide hole in the upper floor
g.pit(40,41,5); g.pit(76,77,5)
g.fill(30,6,32,7)                                    # crates in the lower tunnel
g.fill(84,6,85,6)
g.fill(44,26,46,27); g.fill(70,26,71,26)             # bumps in the upper tunnel
solid,air=g.finish()
ENT=[
 ('player',3,6),
 ('exit',W-1,26),
 ('checkpoint',52,6),
 ('torch',10,6),('torch',36,6),('torch',50,6),('torch',90,6),('torch',106,6),('torch',30,26),('torch',80,26),('torch',110,26),
 ('walltorch',23,15),('walltorch',61,15),('walltorch',99,15),
 ('coin',13,8),('coin',41,8),('coin',77,8),
 ('coin',34,28),('coin',35,28),('coin',36,28),('coin',50,28),('coin',51,28),('coin',64,28),('coin',86,28),('coin',87,28),('coin',88,28),
 ('chest',106,26),
 ('bug',12,6,8,16),
 ('golem',36,6,33,39),
 ('licker',46,6,43,50),
 ('golem',68,6,66,74),
 ('sorcerer',88,6,88,88),
 ('licker',104,6,100,110),
 ('bat',40,30),('bat',75,30),
 ('bug',64,26,61,68),
]
