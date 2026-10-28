# Stage 1-6 "O Poço" (the well): a hall over a bottomless pit, a shaft to climb and an upper
# corridor to the exit. Grid coordinates: x 0..W-1, y 0..H-1 (y up). Scene cell = (x+OX, y+OY).
# An entity at (x, y) stands on the cell row y (the floor is row y-1).
W,H=112,36
OX,OY=-50,-20
air=set()
def carve(x0,y0,x1,y1):
    for x in range(x0,x1+1):
        for y in range(y0,y1+1): air.add((x,y))
def fill(x0,y0,x1,y1):
    for x in range(x0,x1+1):
        for y in range(y0,y1+1): air.discard((x,y))
# A: entrance corridor, open on the left edge, floor row 3
carve(0,4,20,8)
carve(4,9,16,10)
# B: two steps up
carve(20,4,32,11)
fill(23,4,25,4)             # step 1 (stand 5)
fill(26,4,32,5)             # step 2 = hall floor on this side (stand 6)
# C: the hall, floor row 5; the pit x33..48 has no bottom
carve(30,6,58,19)
carve(33,0,48,5)
fill(35,5,37,5)             # platform A (stand 6)
fill(40,6,42,6)             # platform B (stand 7)
fill(45,5,46,5)             # platform C (stand 6)
# D: the shaft, x56..67; staggered ledges two rows apart, the last one joins the corridor
carve(56,6,67,30)
for i,y in enumerate(range(7,24,2)):
    if i%2==0: fill(62,y,67,y)
    else: fill(56,y,61,y)
# E: upper corridor, floor row 24, open on the right edge
carve(67,25,111,30)
fill(80,25,82,25)           # bump
carve(91,0,92,24)           # 2-wide drop hole
fill(98,25,100,26)          # step (stand 27)
fill(101,25,108,28)         # shelf block (stand 29)
carve(101,29,108,33)        # the nook above the shelf
solid=set((x,y) for x in range(W) for y in range(H) if (x,y) not in air)
for y in range(4,8): solid.discard((0,y))      # entrance opening
for y in range(25,29): solid.discard((W-1,y))  # exit opening
for x in range(33,49): solid.discard((x,0))    # pit
for x in range(91,93): solid.discard((x,0))    # drop hole
air|=set((x,y) for x in range(W) for y in range(H) if (x,y) not in solid)

ENT=[
 ('player',3,4),
 ('exit',W-1,25),
 ('checkpoint',72,25),
 ('torch',8,4),('torch',28,6),('torch',31,6),('torch',52,6),('torch',70,25),('torch',86,25),('torch',96,25),
 ('walltorch',44,14),('walltorch',59,18),('walltorch',64,26),('walltorch',105,31),
 ('coin',12,6),('coin',13,6),('coin',41,9),('coin',42,9),('coin',36,8),('coin',45,8),
 ('coin',59,22),('coin',65,16),('coin',91,27),('coin',92,27),
 ('chest',106,29),
 ('bug',16,4,10,19),
 ('golem',53,6,50,55),
 ('bug',58,18,56,61),
 ('sorcerer',78,25,78,78),
 ('golem',87,25,84,89),
 ('licker',95,25,93,97),
 ('bug',104,29,102,107),
]
