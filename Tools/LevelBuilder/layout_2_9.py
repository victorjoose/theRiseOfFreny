# 2-9 "O Feiticeiro Supremo": the boss of the cisterns. A short hall with the checkpoint, then a
# round chamber with platforms; the door out only opens once the Sorcerer Supreme is dead.
from levelkit import Grid
W,H=72,30
OX,OY=-36,-15
SCENE_DIR='Assets/Scenes/Campaign'
TINT=(0.62,0.9,0.86)
EXIT_PRESS_UP=True
BOSS='Sorcerer_Supreme'
g=Grid(W,H)
g.carve(0,5,20,10); g.open_edge([(0,y) for y in range(5,9)])      # hall, floor 4
g.carve(20,5,68,24)                                                # the chamber
for x0,x1,row in ((25,31,6),(49,55,6),(33,37,8),(43,47,8),(38,42,10),(34,36,12),(44,46,12)):
    g.fill(x0,row,x1,row)
g.fill(64,5,68,5)                                                  # step up to the door
solid,air=g.finish()
ENT=[
 ('player',3,5),
 ('exit',66,6),
 ('checkpoint',14,5),
 ('torch',8,5),('torch',23,5),('torch',60,5),
 ('walltorch',28,18),('walltorch',52,18),('walltorch',40,22),
 ('coin',28,9),('coin',52,9),('coin',40,13),('coin',35,15),('coin',45,15),
 ('supreme',58,5,58,58),
 ('sorcerer',40,11,40,40),
 ('bat',30,16),('bat',50,16),
]
