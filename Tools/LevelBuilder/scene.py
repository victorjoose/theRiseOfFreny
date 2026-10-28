import re,sys,os
REPO=os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)),'..','..'))
def guidmap():
    m={}
    for root,_,fs in os.walk(REPO+'/Assets'):
        for f in fs:
            if f.endswith('.meta'):
                p=os.path.join(root,f)
                try:
                    for l in open(p,errors='ignore'):
                        if l.startswith('guid:'): m[l.split()[1]]=p[len(REPO)+1:-5]; break
                except: pass
    return m
def docs(path):
    txt=open(path,errors='ignore').read()
    parts=re.split(r'^--- !u!(\d+) &(-?\d+)( stripped)?\n',txt,flags=re.M)
    out={}
    for i in range(1,len(parts),4):
        out[parts[i+1]]=(int(parts[i]),parts[i+3],parts[i+2] is not None)
    return out
