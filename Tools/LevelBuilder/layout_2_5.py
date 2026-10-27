# 2-5 "O Ninho": a wide cave with a hilly floor and floating rocks, home of the bats.
import math
from levelkit import Grid
W,H=104,36
OX,OY=-52,-18
SCENE_DIR='Assets/Scenes/Campaign'
TINT=(0.62,0.9,0.86)
g=Grid(W,H)
g.carve(0,8,12,14); g.open_edge([(0,y) for y in range(8,12)])      # entrance, floor 7
g.carve(12,5,92,30)                                                 # the cave
# hilly floor: height changes by at most 1 per column step of 3
heights=[7,7,8,9,9,8,7,6,6,7,8,9,10,10,9,8,8,9,10,11,11,10,9,8,7,7,8,9,9]
for i,h in enumerate(heights):
    x0=12+i*3
    g.fill(x0,5,x0+2,h)
def floor(x):
    i=(x-12)//3
    return heights[i]+1 if 0<=i<len(heights) else None
# floating rocks: each one 1 row above head height from the hill next to it, and two short
# stairs of rocks up to the coins and the chest near the roof
rocks=[]
def rock(x0,x1,row):
    g.fill(x0,row,x1,row); rocks.append((x0,x1,row))
rock(22,24,floor(20)+1); rock(26,28,floor(20)+3); rock(30,32,floor(20)+5)
rock(50,52,floor(48)+1); rock(54,56,floor(48)+3); rock(58,61,floor(48)+5)
rock(76,78,floor(74)+1); rock(80,82,floor(74)+3)
g.carve(92,10,103,16); g.open_edge([(W-1,y) for y in range(10,14)])
g.fill(92,5,103,9)
solid,air=g.finish()
def flat(x):
    a=b=x
    while floor(a-1)==floor(x): a-=1
    while floor(b+1)==floor(x): b+=1
    return a,b
def on_rock(i): return rocks[i][0]+1, rocks[i][2]+1
ENT=[
 ('player',3,8),
 ('exit',W-1,10),
 ('checkpoint',52,floor(52)) if False else ('checkpoint',47,floor(47)),
 ('torch',8,8),('torch',30,floor(30)),('torch',64,floor(64)),('torch',96,10),
 ('walltorch',20,22),('walltorch',46,25),('walltorch',72,24),('walltorch',88,20),
 ('coin',on_rock(0)[0],on_rock(0)[1]+1),('coin',on_rock(1)[0],on_rock(1)[1]+1),('coin',on_rock(2)[0],on_rock(2)[1]+1),('coin',on_rock(2)[0]+1,on_rock(2)[1]+1),
 ('coin',on_rock(3)[0],on_rock(3)[1]+1),('coin',on_rock(4)[0],on_rock(4)[1]+1),('coin',on_rock(5)[0],on_rock(5)[1]+1),
 ('coin',on_rock(6)[0],on_rock(6)[1]+1),('coin',on_rock(7)[0],on_rock(7)[1]+1),('coin',on_rock(7)[0]+1,on_rock(7)[1]+1),
 ('chest',on_rock(5)[0]+2,on_rock(5)[1]),
 ('bat',20,20),('bat',34,22),('bat',46,20),('bat',56,24),('bat',66,21),('bat',80,20),('bat',88,16),
 ('bug',36,floor(36),*flat(36)),
 ('golem',72,floor(72),*flat(72)),
 ('licker',86,floor(86),*flat(86)),
]
