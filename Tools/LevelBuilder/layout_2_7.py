# 2-7 "O Corredor dos Golens": a long, low aqueduct packed with golems and lickers. Little room
# to jump over anyone: fight or dash through.
from levelkit import Grid
W,H=144,24
OX,OY=-72,-12
SCENE_DIR='Assets/Scenes/Campaign'
TINT=(0.62,0.9,0.86)
g=Grid(W,H)
# segments: (x0, x1, floor row, ceiling row); neighbours differ by at most 2 floor rows
segs=[(0,14,8,13),(15,30,8,12),(31,44,10,14),(45,58,9,13),(59,72,7,12),(73,86,9,13),
      (87,100,11,16),(101,114,9,13),(115,128,8,12),(129,143,10,15)]
for x0,x1,f,c in segs:
    g.carve(x0,f+1,x1,c)
g.open_edge([(0,y) for y in range(9,13)])
g.open_edge([(W-1,y) for y in range(11,15)])
g.pit(52,53,9); g.pit(94,95,11)
g.fill(64,8,65,8)                                   # a lip to hop
g.carve(76,14,84,17)                                # alcove with coins and a chest,
g.fill(82,10,84,11); g.fill(76,13,80,13)            # reached from a crate
solid,air=g.finish()
def fl(x):
    for x0,x1,f,c in segs:
        if x0<=x<=x1: return f+1
ENT=[
 ('player',3,fl(3)),
 ('exit',W-1,fl(W-1)),
 ('checkpoint',75,fl(75)),
 ('torch',10,fl(10)),('torch',38,fl(38)),('torch',66,fl(66)),('torch',90,fl(90)),('torch',120,fl(120)),('torch',136,fl(136)),
 ('walltorch',24,11),('walltorch',50,12),('walltorch',107,12),
 ('coin',52,fl(52)+2),('coin',53,fl(53)+2),('coin',78,16),('coin',79,16),('coin',80,16),('coin',94,fl(94)+2),('coin',95,fl(95)+2),
 ('chest',77,14),
 ('bug',22,fl(22),16,29),
 ('golem',38,fl(38),32,43),
 ('licker',48,fl(48),45,51),
 ('golem',68,fl(68),66,72),
 ('golem',88,fl(88),87,93),
 ('licker',98,fl(98),96,100),
 ('sorcerer',112,fl(112),112,112),
 ('golem',105,fl(105),101,110),
 ('licker',122,fl(122),116,128),
 ('golem',134,fl(134),130,140),
]
