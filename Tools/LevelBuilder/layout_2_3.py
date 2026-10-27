# 2-3 "As Pontes": a flooded hall crossed on broken bridges over bottomless water, with bats
# over the gaps and a sorcerer guarding the far side.
from levelkit import Grid
W,H=132,28
OX,OY=-66,-14
SCENE_DIR='Assets/Scenes/Campaign'
TINT=(0.62,0.9,0.86)
g=Grid(W,H)
g.carve(0,11,14,18); g.open_edge([(0,y) for y in range(11,15)])   # entrance, floor 10
g.carve(14,11,120,22)                                              # the hall
g.pit(15,117,10)                                                   # water all the way
# bridges: (length, floor row); the gap before each is 2, or 1 when it climbs 2 rows
spans=[(4,10),(3,11),(4,10),(2,12),(4,13),(3,12),(6,10),(2,11),(2,12),(6,11),(2,10),(3,11),
       (2,13),(5,12),(2,11),(3,10),(2,11),(3,12),(4,11),(2,10)]
bridges=[]
x,prev=17,10
for n,f in spans:
    if bridges:
        x+=1 if f-prev>=2 else 2
    bridges.append((x,x+n-1,f))
    x+=n; prev=f
for x0,x1,f in bridges:
    g.fill(x0,f,x1,f)
END=bridges[-1][1]+3
g.fill(15,10,16,10)                                                # the entrance ledge goes on a bit
g.carve(120,11,131,18); g.open_edge([(W-1,y) for y in range(11,15)])
solid,air=g.finish()
ENT=[
 ('player',3,11),
 ('exit',W-1,11),
 ('checkpoint',66,12),
 ('torch',8,11),('torch',50,11),('torch',86,13),('torch',125,11),
 ('walltorch',30,18),('walltorch',60,19),('walltorch',100,19),
 ('coin',24,14),('coin',34,15),('coin',38,16),('coin',39,16),('coin',61,15),('coin',81,16),('coin',92,14),('coin',105,15),
 ('bat',32,17),('bat',47,18),('bat',75,16),('bat',96,17),
 ('bug',50,11,49,52),
 ('golem',67,12,64,69),
 ('licker',86,13,84,88),
 ('sorcerer',112,12,112,112),
 ('bug',126,11,122,129),
]
