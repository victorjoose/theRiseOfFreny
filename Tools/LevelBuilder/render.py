import sys,re,os
sys.path.insert(0,os.path.dirname(__file__))
from scene import *
from PIL import Image,ImageDraw
G=guidmap()
PX=int(os.environ.get('PX','16'))
_tex={}
def sprites(guid):
    if guid in _tex: return _tex[guid]
    p=REPO+'/'+G[guid]; meta=open(p+'.meta').read()
    img=Image.open(p).convert('RGBA'); W,H=img.size
    ppu=float(re.search(r'spritePixelsToUnits: ([\d.]+)',meta).group(1))
    out={}
    names={}
    cur=None
    for line in meta.split('\n'):
        st=line.strip()
        if st.startswith('name: ') and line.startswith('      name'): cur={'name':st[6:]}
        elif cur is not None:
            for k in ('x','y','width','height'):
                if st.startswith(k+': ') and k not in cur and line.startswith('        '): cur[k]=float(st.split(': ')[1])
            if st.startswith('internalID: '):
                x,y,w,h=cur['x'],cur['y'],cur['width'],cur['height']
                crop=img.crop((int(x),int(H-y-h),int(x+w),int(H-y)))
                out[st[12:]]=(crop,w/ppu,h/ppu); names[cur['name']]=st[12:]; cur=None
    for m in re.finditer(r'- first:\n\s+213: (-?\d+)\n\s+second: (.*)',meta):
        if m.group(2) in names: out[m.group(1)]=out[names[m.group(2)]]
    if not out: out['21300000']=(img,W/ppu,H/ppu)
    _tex[guid]=out; return out
def render(path,outpng,marks=True):
    d=docs(path)
    gos={}
    for fid,(c,b,s) in d.items():
        if c==1:
            gos[fid]={'name':re.search(r'm_Name: (.*)',b).group(1),'active':'m_IsActive: 1' in b}
    order={}
    for fid,(c,b,s) in d.items():
        if c==483693784:
            g=re.search(r'm_GameObject: \{fileID: (-?\d+)',b).group(1)
            order[g]=(int(re.search(r'm_SortingOrder: (-?\d+)',b).group(1)), b.count('m_Enabled: 1')>0)
    layers=[]
    pos={}
    for fid,(c,b,s) in d.items():
        if c==4:
            g=re.search(r'm_GameObject: \{fileID: (-?\d+)',b)
            p=re.search(r'm_LocalPosition: \{x: ([-\d.e]+), y: ([-\d.e]+)',b)
            fa=re.search(r'm_Father: \{fileID: (-?\d+)\}',b)
            if g: pos[g.group(1)]=(float(p.group(1)),float(p.group(2)),fid,fa.group(1))
    tr2go={v[2]:k for k,v in pos.items()}
    def world(g):
        x,y,_,fa=pos[g]
        while fa!='0' and fa in tr2go:
            gx,gy,_,fa=pos[tr2go[fa]]; x+=gx; y+=gy
        return x,y
    allcells=[]
    for fid,(c,b,s) in d.items():
        if c!=1839735485: continue
        g=re.search(r'm_GameObject: \{fileID: (-?\d+)',b).group(1)
        if not gos[g]['active']: continue
        cells=re.findall(r'- first: \{x: (-?\d+), y: (-?\d+), z: 0\}\n\s+second:\n\s+serializedVersion: 2\n\s+m_TileIndex: \d+\n\s+m_TileSpriteIndex: (\d+)\n\s+m_TileMatrixIndex: (\d+)',b.split('m_AnimatedTiles')[0])
        sa=b.split('m_TileSpriteArray:')[1].split('m_TileMatrixArray')[0]
        sprs=re.findall(r'm_Data: \{fileID: (-?\d+)(?:, guid: (\w+))?',sa)
        mats=b.split('m_TileMatrixArray:')[1].split('m_TileColorArray')[0]
        ox,oy=world(g)
        layers.append((order.get(g,(0,1))[0],gos[g]['name'],cells,sprs,ox,oy))
        allcells+= [(int(x)+ox,int(y)+oy) for x,y,_,_ in cells]
    xs=[c[0] for c in allcells]; ys=[c[1] for c in allcells]
    x0,x1,y0,y1=min(xs)-1,max(xs)+2,min(ys)-1,max(ys)+3
    W=int((x1-x0)*PX); H=int((y1-y0)*PX)
    im=Image.new('RGBA',(W,H),(20,16,28,255))
    for so,name,cells,sprs,ox,oy in sorted(layers,key=lambda l:l[0]):
        for x,y,si,mi in cells:
            f,gd=sprs[int(si)]
            if not gd: continue
            try: crop,w,h=sprites(gd)[f]
            except KeyError: continue
            pw,ph=max(1,int(w*PX)),max(1,int(h*PX))
            t=crop.resize((pw,ph),Image.NEAREST)
            px=int((int(x)+ox-x0)*PX); py=int(H-(int(y)+oy-y0)*PX-ph)
            im.alpha_composite(t,(max(px,0),max(py,0)))
    from PIL import ImageEnhance
    a=im.split()[3]; im=ImageEnhance.Brightness(im.convert('RGB')).enhance(float(os.environ.get('BRIGHT','2.6'))).convert('RGBA')
    if marks:
        dr=ImageDraw.Draw(im)
        def mk(x,y,col,label):
            X=(x-x0)*PX; Y=H-(y-y0)*PX
            dr.ellipse((X-5,Y-5,X+5,Y+5),fill=col); dr.text((X+6,Y-12),label,fill=col)
        for g,info in gos.items():
            if g not in pos or not info['active']: continue
            n=info['name']; x,y=world(g)
            if n=='Player': mk(x,y,(0,255,0),'START')
            elif n.endswith('_Enemy') or '_Enemy ' in n: mk(x,y,(255,60,60),n.split('_')[0])
            elif n.startswith('LevelExit') or 'Exit' in n: mk(x,y,(255,255,0),n)
            elif 'Savepoint' in n: mk(x,y,(0,200,255),'SAVE')
        for fid,(c,b,s) in d.items():
            if c==1001:
                sg=re.search(r'm_SourcePrefab: \{fileID: \d+, guid: (\w+)',b).group(1)
                pn=G.get(sg,'?').split('/')[-1]
                xs_=re.findall(r'propertyPath: m_LocalPosition.x\n\s+value: (.*)',b); ys_=re.findall(r'propertyPath: m_LocalPosition.y\n\s+value: (.*)',b)
                if xs_ and ys_ and 'Torch' not in pn: mk(float(xs_[0]),float(ys_[0]),(255,200,0),pn.replace('.prefab',''))
    im.save(outpng); return (x0,x1,y0,y1)
if __name__=='__main__':
    print(render(sys.argv[1],sys.argv[2]))
