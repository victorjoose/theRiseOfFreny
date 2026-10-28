import sys
from grid import *
L=layers(sys.argv[1])
solid=set(); bg=set()
for (n,g),((ox,oy),a,cells) in L.items():
    if not a or abs(ox)>1 or abs(oy)>1: continue
    if 'Ground' in n: solid|=cells
    if n.startswith('Background') or n.startswith('EXTRA Background'): bg|=cells
xs=[x for x,y in bg]; ys=[y for x,y in bg]
x0,x1,y0,y1=min(xs),max(xs),min(ys),max(ys)
print('bg bounds',x0,x1,y0,y1)
# passable = bg and not solid; find passable cells at edges
for side,cells in [('LEFT',[(x0,y) for y in range(y0,y1+1)]),('RIGHT',[(x1,y) for y in range(y0,y1+1)]),('BOTTOM',[(x,y0) for x in range(x0,x1+1)]),('TOP',[(x,y1) for x in range(x0,x1+1)])]:
    open_=[c for c in cells if c in bg and c not in solid]
    if open_: print(side,open_[0],'..',open_[-1],len(open_))
