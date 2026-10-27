"""Replaces the enemies that were copied by hand into a scene (2023 copies, with old values) by
instances of the enemy prefabs, so every stage and the arena use the same enemies. Keeps each
enemy's name, parent, position, facing and patrol points. Run: enemies_to_prefabs.py 1-1 1-2 ..."""
import sys,re,os,glob,random
REPO=os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)),'..','..'))
# script name -> (prefab guid, root GameObject, root Transform, enemy MonoBehaviour), as in build_stage.py
PREFABS={
 'Bug':('60d87c22096e25a46afea541c8d6f3e2','8274441830027215983','6101331372153397134','8871542281216455844'),
 'Golem':('49496e0f74af48345867a62b81c12758','6496106852740584793','5877934722757971472','1568797996859213525'),
 'Licker':('c46c49e424834b74baa7cd1d50995add','332949994701094415','2064545646695465190','3840879670598176171'),
 'Sorcerer':('20e5ccc9f5a154c48af6bbaec4f33329','4902039112802150574','6741818021595377839','1835722495582547347'),
}
scripts={}
for m in glob.glob(f'{REPO}/Assets/Scripts/Enemy/**/*.cs.meta',recursive=True):
    scripts[re.search(r'guid: (\w+)',open(m).read()).group(1)]=os.path.basename(m)[:-8]

def convert(path):
    src=open(path).read()
    parts=re.split(r'(^--- !u!\d+ &-?\d+(?: stripped)?\n)',src,flags=re.M)
    pre=parts[0]; docs=[[h,b] for h,b in zip(parts[1::2],parts[2::2])]
    fid=lambda h: re.match(r'--- !u!\d+ &(-?\d+)',h).group(1)
    cls=lambda h: re.match(r'--- !u!(\d+)',h).group(1)
    by={fid(h):d for d in docs for h in [d[0]]}
    go_of=lambda b: (re.search(r'm_GameObject: \{fileID: (-?\d+)\}',b) or [None,None])[1]
    comps={}; tr_of={}
    for h,b in docs:
        g=go_of(b)
        if g and 'stripped' not in h:
            comps.setdefault(g,[]).append(fid(h))
            if cls(h)=='4': tr_of[g]=fid(h)
    kids=lambda b: re.findall(r'- \{fileID: (-?\d+)\}',b.split('m_Children:')[1].split('m_Father')[0])
    used=set(by); rnd=random.Random(path)
    def newid():
        while True:
            n=str(rnd.randint(10**8,2*10**9))
            if n not in used: used.add(n); return n
    delete=set(); new=[]; count=0
    def kill(t):
        g=go_of(by[t][1]); delete.add(g); delete.update(comps[g])
        for k in kids(by[t][1]): kill(k)
    for h,b in list(docs):
        if cls(h)!='114' or re.search(r'm_PrefabInstance: \{fileID: -?[1-9]',b): continue
        m=re.search(r'm_Script: \{fileID: 11500000, guid: (\w+)',b)
        kind=scripts.get(m.group(1)) if m else None
        if kind not in PREFABS: continue
        guid,root_go,root_tr,mb=PREFABS[kind]
        g=go_of(b); t=tr_of[g]; tb=by[t][1]
        name=re.search(r'm_Name: (.*)',by[g][1]).group(1)
        father=re.search(r'm_Father: \{fileID: (-?\d+)\}',tb).group(1)
        pos=re.search(r'm_LocalPosition: \{x: (\S+), y: (\S+), z: (\S+)\}',tb).groups()
        scale=re.search(r'm_LocalScale: \{x: (\S+), y: (\S+), z: (\S+)\}',tb).groups()
        pa=re.search(r'pointA: \{fileID: (-?\d+)\}',b).group(1)
        pb=re.search(r'pointB: \{fileID: (-?\d+)\}',b).group(1)
        kill(t)
        inst,strip=newid(),newid()
        mods=[(root_go,'m_Name',name,'0')]
        mods+=[(root_tr,f'm_LocalPosition.{a}',v,'0') for a,v in zip('xyz',pos)]
        mods+=[(root_tr,f'm_LocalScale.{a}',v,'0') for a,v in zip('xyz',scale)]
        mods+=[(mb,'pointA','',pa),(mb,'pointB','',pb)]
        body=f'PrefabInstance:\n  m_ObjectHideFlags: 0\n  serializedVersion: 2\n  m_Modification:\n    m_TransformParent: {{fileID: {father}}}\n    m_Modifications:\n'
        for tg,p,v,r in mods:
            body+=f'    - target: {{fileID: {tg}, guid: {guid}, type: 3}}\n      propertyPath: {p}\n      value: {v}\n      objectReference: {{fileID: {r}}}\n'
        body+=f'    m_RemovedComponents: []\n    m_RemovedGameObjects: []\n    m_AddedGameObjects: []\n    m_AddedComponents: []\n  m_SourcePrefab: {{fileID: 100100000, guid: {guid}, type: 3}}\n'
        new.append([f'--- !u!1001 &{inst}\n',body])
        new.append([f'--- !u!4 &{strip} stripped\n',f'Transform:\n  m_CorrespondingSourceObject: {{fileID: {root_tr}, guid: {guid}, type: 3}}\n  m_PrefabInstance: {{fileID: {inst}}}\n  m_PrefabAsset: {{fileID: 0}}\n'])
        # the parent now lists the instance's transform in place of the old one
        fd=by[father]
        fd[1]=fd[1].replace(f'  - {{fileID: {t}}}\n',f'  - {{fileID: {strip}}}\n',1)
        count+=1
    docs=[d for d in docs if fid(d[0]) not in delete]+new
    out=pre+''.join(h+b for h,b in docs)
    left=[i for i in delete if re.search(r'\{fileID: '+i+r'\}',out)]
    if left:
        raise SystemExit(f'{path}: still referenced after removal: {left[:5]}')
    open(path,'w').write(out)
    print(os.path.basename(path),'enemies replaced:',count)

for stage in sys.argv[1:]:
    convert(f'{REPO}/Assets/Scenes/MaxLevels/{stage}.unity')
