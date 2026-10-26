# 2-8 "A Torre": the climb out of the cisterns. A tall tower in two halves split by a resting
# room with the checkpoint; bats and sorcerers make every hop count.
from levelkit import Grid, zigzag, hole_over
W,H=48,90
OX,OY=-24,-45
SCENE_DIR='Assets/Scenes/Campaign'
TINT=(0.62,0.9,0.86)
g=Grid(W,H)
g.carve(0,3,20,8); g.open_edge([(0,y) for y in range(3,7)])       # entrance, floor 2
g.carve(8,3,40,40)                                                 # lower half
low=zigzag(g,8,40,4,38,width=4,step=4)
g.carve(4,41,44,46); g.fill(4,40,44,40)                            # resting room, floor 40
top_low=low[-1]
hole_over(g,low,40)                                                # way up into the room
g.carve(8,47,40,82)                                                # upper half
g.fill(12,41,14,41)                                                # crate up to the first ledge
up=zigzag(g,8,40,43,79,width=4,step=4)
g.carve(0,82,30,87); g.fill(0,81,30,81)                            # exit hall, floor 81
g.open_edge([(0,y) for y in range(82,86)])
top_up=up[-1]
hole_over(g,up,81)
solid,air=g.finish()
def on(l,dx=1): return l[0]+dx, l[2]+1
walls=[l for l in low+up if l[0]==8 or l[1]==40]
ENT=[
 ('player',3,3),
 ('exit',0,82),
 ('checkpoint',24,41),
 ('torch',14,3),('torch',30,41),('torch',10,82),('torch',27,82),
 ('walltorch',24,14),('walltorch',24,28),('walltorch',24,56),('walltorch',24,70),
 ('coin',16,43),('coin',17,43),('coin',36,43),('coin',37,43),
 ('chest',40,41),
 ('bat',30,18),('bat',18,30),('bat',30,60),('bat',18,72),
 ('golem',8,41,5,11),
 ('bug',12,82,6,18),
]
for l in walls[1:-1:3]:
    x=l[0]+1 if l[0]==8 else l[1]-1
    ENT.append(('sorcerer',x,l[2]+1,x,x))
for l in (low+up)[3::5]:
    ENT.append(('coin',l[0]+1,l[2]+3))
