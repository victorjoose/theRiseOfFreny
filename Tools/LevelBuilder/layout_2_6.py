# 2-6 "O Fosso": the way goes down. From the top, drop from ledge to ledge through a deep well
# (each ledge has someone waiting), then cross the flooded bottom to the exit.
from levelkit import Grid
W,H=76,72
OX,OY=-38,-36
SCENE_DIR='Assets/Scenes/Campaign'
TINT=(0.62,0.9,0.86)
g=Grid(W,H)
g.carve(0,62,24,67); g.open_edge([(0,y) for y in range(62,66)])   # entrance, floor 61
g.carve(16,8,60,61)                                                # the well
# ledges going down, alternating sides; each is reachable by dropping from the one above
ledges=[(24,38,54),(42,56,47),(18,30,40),(36,50,33),(20,34,26),(40,56,19),(18,28,13)]
for x0,x1,row in ledges:
    g.fill(x0,row,x1,row)
g.fill(16,61,23,61)
g.pit(34,35,7)                                                     # flooded hole in the bottom
g.carve(60,8,75,14); g.open_edge([(W-1,y) for y in range(8,12)])  # exit tunnel, floor 7
solid,air=g.finish()
def on(i,dx=1): return ledges[i][0]+dx, ledges[i][2]+1
ENT=[
 ('player',3,62),
 ('exit',W-1,8),
 ('checkpoint',*on(4,2)),
 ('torch',10,62),('torch',*on(1,10)),('torch',*on(3,10)),('torch',*on(5,12)),('torch',66,8),
 ('walltorch',17,50),('walltorch',59,40),('walltorch',17,30),('walltorch',59,24),
 ('coin',*on(0,8)),('coin',*on(1,2)),('coin',*on(2,3)),('coin',*on(3,4)),('coin',*on(5,3)),('coin',*on(6,4)),
 ('bug',*on(0,5),ledges[0][0],ledges[0][1]),
 ('golem',*on(1,7),ledges[1][0]+2,ledges[1][1]),
 ('sorcerer',*on(2,1),ledges[2][0]+1,ledges[2][0]+1),
 ('licker',*on(3,7),ledges[3][0]+2,ledges[3][1]),
 ('bat',40,30),('bat',30,16),
 ('golem',*on(5,8),ledges[5][0]+3,ledges[5][1]),
 ('bug',50,8,44,58),
 ('licker',68,8,63,72),
 ('chest',*on(6,1)),
]
