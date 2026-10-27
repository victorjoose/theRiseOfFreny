# 2-1 "A Descida": steps down into the cisterns, then a long corridor with small pits.
# Introduces the bats.
from levelkit import Grid
W,H=120,26
OX,OY=-60,-13
SCENE_DIR='Assets/Scenes/Campaign'
TINT=(0.62,0.9,0.86)
g=Grid(W,H)
g.carve(0,15,20,21); g.open_edge([(0,y) for y in range(15,19)])   # entrance, floor 14
g.carve(20,13,27,21)                                              # floor 12
g.carve(27,11,34,21)                                              # floor 10
g.carve(34,9,119,17)                                              # corridor, floor 8
g.carve(52,18,66,20)                                              # higher vault
g.pit(48,49,8); g.pit(68,69,8); g.pit(88,89,8)
g.fill(76,9,79,10)                                                # a block to climb over
g.fill(100,9,101,9); g.fill(102,9,118,10)                         # steps up to the exit landing
g.open_edge([(W-1,y) for y in range(11,15)])
solid,air=g.finish()
ENT=[
 ('player',3,15),
 ('exit',W-1,11),
 ('checkpoint',72,9),
 ('torch',10,15),('torch',30,11),('torch',44,9),('torch',60,9),('torch',84,9),('torch',96,9),('torch',112,11),
 ('walltorch',24,17),('walltorch',58,16),('walltorch',108,15),
 ('coin',15,16),('coin',16,16),('coin',48,11),('coin',49,11),('coin',59,11),('coin',60,11),('coin',77,13),('coin',88,11),('coin',89,11),
 ('bug',40,9,36,46),
 ('bat',58,14),
 ('golem',62,9,52,66),
 ('bat',82,13),
 ('licker',94,9,91,99),
 ('bug',110,11,105,116),
]
