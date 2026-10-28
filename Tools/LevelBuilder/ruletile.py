import re,struct,os
REPO=os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)),'..','..'))
class RuleTile:
    def __init__(self,path):
        t=open(path).read()
        self.default=re.search(r'm_DefaultSprite: \{fileID: (-?\d+)',t).group(1)
        self.rules=[]
        for blk in t.split('  - m_Id: ')[1:]:
            spr=re.search(r'm_Sprites:\n\s+- \{fileID: (-?\d+)',blk).group(1)
            nb=re.search(r'm_Neighbors: (\w*)',blk).group(1)
            vals=[struct.unpack('<i',bytes.fromhex(nb[i:i+8]))[0] for i in range(0,len(nb),8)]
            pos=[(int(a),int(b)) for a,b in re.findall(r'- \{x: (-?\d+), y: (-?\d+), z: 0\}',blk.split('m_NeighborPositions:')[1].split('m_RuleTransform')[0])]
            tr=int(re.search(r'm_RuleTransform: (\d)',blk).group(1))
            self.rules.append((spr,list(zip(vals,pos)),tr))
    @staticmethod
    def rot(p,a):
        x,y=p
        return {0:(x,y),90:(y,-x),180:(-x,-y),270:(-y,x)}[a]
    def matches(self,nbs,cell,has,f):
        for v,p in nbs:
            q=f(p); other=has((cell[0]+q[0],cell[1]+q[1]))
            if v==1 and not other: return False
            if v==2 and other: return False
        return True
    def pick(self,cell,has):
        """returns (sprite fileID, (sx,sy,angle))"""
        for spr,nbs,tr in self.rules:
            if self.matches(nbs,cell,has,lambda p:p): return spr,(1,1,0)
            if tr==1:
                for a in (90,180,270):
                    if self.matches(nbs,cell,has,lambda p,a=a:self.rot(p,a)): return spr,(1,1,a)
            elif tr==4:
                for mx,my in ((True,True),(True,False),(False,True)):
                    if self.matches(nbs,cell,has,lambda p,mx=mx,my=my:(-p[0] if mx else p[0],-p[1] if my else p[1])): return spr,(-1 if mx else 1,-1 if my else 1,0)
            elif tr==2:
                if self.matches(nbs,cell,has,lambda p:(-p[0],p[1])): return spr,(-1,1,0)
            elif tr==3:
                if self.matches(nbs,cell,has,lambda p:(p[0],-p[1])): return spr,(1,-1,0)
        return self.default,(1,1,0)
def matrix(sx,sy,angle):
    import math
    # Unity: TRS(0, Euler(0,0,-angle), scale)
    a=math.radians(-angle); c,s=round(math.cos(a),6),round(math.sin(a),6)
    m=[[c*sx,-s*sy,0,0],[s*sx,c*sy,0,0],[0,0,1,0],[0,0,0,1]]
    return m
if __name__=='__main__':
    rt=RuleTile(REPO+'/Assets/Sprites/Environment/Ground Rule Tile.asset')
    print(len(rt.rules),[ (r[2],len(r[1])) for r in rt.rules])
