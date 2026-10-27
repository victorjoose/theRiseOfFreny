import sys,re,os
sys.path.insert(0,os.path.dirname(__file__))
from scene import *
def layers(path):
    d=docs(path); out={}
    names={f:re.search(r'm_Name: (.*)',b).group(1) for f,(c,b,s) in d.items() if c==1}
    act={f:'m_IsActive: 1' in b for f,(c,b,s) in d.items() if c==1}
    pos={}
    for f,(c,b,s) in d.items():
        if c==4 and re.search(r'm_GameObject: \{fileID: (-?\d+)',b) and 'm_LocalPosition' in b:
            g=re.search(r'm_GameObject: \{fileID: (-?\d+)',b).group(1)
            p=re.search(r'm_LocalPosition: \{x: ([-\d.e]+), y: ([-\d.e]+)',b); pos[g]=(float(p.group(1)),float(p.group(2)))
    for f,(c,b,s) in d.items():
        if c!=1839735485: continue
        g=re.search(r'm_GameObject: \{fileID: (-?\d+)',b).group(1)
        cells=re.findall(r'- first: \{x: (-?\d+), y: (-?\d+), z: 0\}',b.split('m_AnimatedTiles')[0])
        out[(names[g],g)]=(pos.get(g,(0,0)),act[g],set((int(x),int(y)) for x,y in cells))
    return out
