# 2-2 "A Escadaria": a tall cistern climbed on staggered ledges (the ones against the walls
# hold the sorcerers and the checkpoint), up to a gallery at the top.
from levelkit import Grid, zigzag
W,H=52,74
OX,OY=-26,-37
SCENE_DIR='Assets/Scenes/Campaign'
TINT=(0.62,0.9,0.86)
g=Grid(W,H)
g.carve(0,3,24,9); g.open_edge([(0,y) for y in range(3,7)])       # entrance room, floor 2
g.carve(14,3,38,66)                                                # the cistern
ledges=zigzag(g,14,38,4,62,width=5,step=5)
g.carve(38,64,51,69); g.fill(36,63,51,63)                          # top gallery, floor 63
g.open_edge([(W-1,y) for y in range(64,68)])
solid,air=g.finish()
top=ledges[-1]
ENT=[
 ('player',3,3),
 ('exit',W-1,64),
 ('checkpoint',15,21),
 ('torch',8,3),('torch',20,3),('torch',44,64),
 ('walltorch',26,12),('walltorch',26,28),('walltorch',26,44),('walltorch',26,58),
 ('coin',10,5),('coin',11,5),('coin',15,23),('coin',16,23),('coin',35,46),('coin',36,46),('coin',16,55),('coin',48,66),
 ('bug',8,3,4,12),
 ('sorcerer',36,29,36,36),
 ('sorcerer',16,37,16,16),
 ('sorcerer',16,53,16,16),
 ('bat',27,24),('bat',24,40),('bat',27,56),
 ('golem',45,64,41,48),
]
