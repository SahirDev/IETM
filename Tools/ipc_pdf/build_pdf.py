import json, sys
from reportlab.lib.pagesizes import letter
from reportlab.pdfgen import canvas
from reportlab.lib.colors import black, white, HexColor
from reportlab.lib.utils import ImageReader
sys.path.insert(0,__import__('os').path.dirname(__import__('os').path.abspath(__file__)))
D=json.load(open('/home/claude/ietm/Assets/IETM/Resources/IETM/landing_gear_ietm.json'))
pub=D['publication']; fig=D['figure']; items=D['items']
FIGS=json.load(open('/home/claude/ipc/figs.json'))
OUT=sys.argv[1]
W,H=letter; M=36
CH='32-20-00'; FIGNO='1'; DATE='Nov 17/26'
c=canvas.Canvas(OUT,pagesize=letter)
c.setTitle('F-16 Nose Landing Gear - Illustrated Parts Catalog (32-20-00 Fig 1)')
c.setAuthor('IETM POC'); c.setSubject(pub['pmc'])
page=[0]
def frame(title_right='ILLUSTRATED PARTS CATALOG'):
    page[0]+=1
    c.setFont('Helvetica-Bold',9); c.drawString(M,H-M+4,'F-16 NLG')
    c.drawRightString(W-M,H-M+4,title_right)
    c.setLineWidth(0.8); c.line(M,H-M,W-M,H-M)
    # footer
    y=M+6; c.line(M,y+26,W-M,y+26)
    c.setFont('Helvetica',7); c.drawString(M,y+16,'EFFECTIVITY')
    c.setFont('Helvetica-Bold',8); c.drawString(M,y+6,'ALL')
    c.setFont('Helvetica-Bold',11); c.drawRightString(W-M,y+14,CH)
    c.setFont('Helvetica',8); c.drawRightString(W-M,y+3,f'Page {page[0]}')
    c.drawRightString(W-M,y-7,DATE)
    c.setFont('Helvetica',6.5); c.setFillColor(HexColor('#555555'))
    c.drawCentredString(W/2,y+8,pub['pmc']+'   ·   '+fig['icn'])
    c.drawCentredString(W/2,y-2,'SAMPLE DATA – POC ONLY. Part numbers and CAGE codes are placeholders.')
    c.setFillColor(black)

# ---- title page
frame('TITLE PAGE')
c.setFont('Helvetica-Bold',26); c.drawCentredString(W/2,H-200,'F-16 NOSE LANDING GEAR')
c.setFont('Helvetica',16); c.drawCentredString(W/2,H-228,'ILLUSTRATED PARTS CATALOG')
c.setFont('Helvetica',11); c.drawCentredString(W/2,H-256,f'Chapter {CH}  ·  Figure {FIGNO}')
img=ImageReader(FIGS[0][1]['image']); s=330
c.drawImage(img,(W-s)/2,H-600,s,s)
c.setFont('Helvetica',9)
rows=[('Publication code',pub['pmc']),('Issue',f"{pub['issue']}  ({pub['issueDate']})"),('Applicability',pub['applicability']),('Figure ICN',fig['icn'])]
y=200
for k,v in rows:
    c.setFont('Helvetica-Bold',9); c.drawString(110,y,k); c.setFont('Helvetica',9); c.drawString(230,y,v); y-=15
c.setFillColor(HexColor('#B45309')); c.setFont('Helvetica-Bold',8.5)
c.drawCentredString(W/2,110,pub['dataStatus'][:110]); c.setFillColor(black)
c.showPage()

# ---- illustration sheets
NS=len(FIGS)
for i,(name,f) in enumerate(FIGS):
    frame()
    from PIL import Image as _I
    import numpy as _np
    im=_I.open(f['image']).convert('L'); a=_np.array(im)<250
    ys,xs=_np.nonzero(a)
    lx=[L['lx'] for L in f['labels']]; ly=[L['ly'] for L in f['labels']]
    x0=int(max(0,min([xs.min()]+lx)-40)); x1=int(min(im.width,max([xs.max()]+lx)+40))
    y0=int(max(0,min([ys.min()]+ly)-20)); y1=int(min(im.height,max([ys.max()]+ly)+20))
    crop=f['image'].replace('.png','_c.png'); im.crop((x0,y0,x1,y1)).save(crop)
    for L in f['labels']:
        L['ax']-=x0; L['lx']-=x0; L['ay']-=y0; L['ly']-=y0
    f=dict(f,image=crop,size=(x1-x0,y1-y0))
    iw,ih=f['size']; top=H-M-14; bot=M+70
    s=min((W-2*M)/iw,(top-bot)/ih); ox=(W-iw*s)/2; oy=bot+(top-bot-ih*s)/2
    c.drawImage(ImageReader(f['image']),ox,oy,iw*s,ih*s)
    P=lambda x,y:(ox+x*s, oy+(ih-y)*s)
    c.setLineWidth(0.5)
    for L in f['labels']:
        ax,ay=P(L['ax'],L['ay']); lx,ly=P(L['lx'],L['ly'])
        c.setFont('Helvetica-Bold',9); tw=c.stringWidth(L['item'],'Helvetica-Bold',9)
        # leader stops at the label's edge
        vx,vy=ax-lx,ay-ly; dl=max(1e-6,(vx*vx+vy*vy)**0.5)
        ex=lx+vx/dl*(tw/2+2); ey=ly+vy/dl*6
        c.line(ax,ay,ex,ey); c.circle(ax,ay,1.2,fill=1)
        c.drawCentredString(lx,ly-3,L['item'])
    c.setFont('Helvetica-Bold',10); c.drawCentredString(W/2,M+52,fig['title'].upper())
    c.setFont('Helvetica',9); c.drawCentredString(W/2,M+40,f'Figure {FIGNO} (Sheet {i+1} of {NS})  -  {name.upper()}')
    c.showPage()

# ---- parts list
cols=[('FIG.\nITEM',M,52),('PART NUMBER',M+52,104),('1234567\nNOMENCLATURE',M+156,236),('CAGE',M+392,46),('SMR',M+438,42),('EFFECT\nCODE',M+480,32),('UNITS\nPER ASSY',M+512,28)]
def header(y):
    c.setLineWidth(0.8); c.line(M,y+2,W-M,y+2)
    for t,x,w in cols:
        lines=t.split('\n'); c.setFont('Helvetica-Bold',7)
        for k,l in enumerate(lines): c.drawString(x+2,y-8-k*8,l)
    c.line(M,y-22,W-M,y-22); return y-34
def wrap(text,font,size,width):
    words=text.split(); out=[]; cur=''
    for w_ in words:
        t=(cur+' '+w_).strip()
        if c.stringWidth(t,font,size)<=width: cur=t
        else: out.append(cur); cur=w_
    out.append(cur); return out
frame(); y=header(H-M-14)
for it in items:
    dots='.'*(it['indent']-1)
    nom=dots+it['description']
    if it['indent']==1: nom='NHA '+nom if False else nom
    lines=wrap(nom,'Courier',8,cols[2][2]-4)
    info=it.get('info','')
    extra=wrap('('+info+')','Helvetica-Oblique',6.5,cols[2][2]-4-6*len(dots)) if info else []
    need=11*len(lines)+8*len(extra)+4
    if y-need<M+50:
        c.showPage(); frame(); y=header(H-M-14)
    no=it['itemNo']; ill=bool(it['nodes']) or it['indent']>1
    c.setFont('Courier',8)
    c.drawString(cols[0][1]+2,y,f'{FIGNO}-{no}' if it['itemNo']!='001' else f'{FIGNO}-001')
    c.drawString(cols[1][1]+2,y,it['partNumber'])
    for k,l in enumerate(lines): c.drawString(cols[2][1]+2,y-k*11,l)
    c.drawString(cols[3][1]+2,y,it['cage']); c.drawString(cols[4][1]+2,y,it['smr'])
    c.drawString(cols[5][1]+2,y,''); c.drawRightString(cols[6][1]+cols[6][2],y,str(it['upa']))
    yy=y-11*len(lines)+3
    c.setFont('Helvetica-Oblique',6.5); c.setFillColor(HexColor('#444444'))
    for l in extra: c.drawString(cols[2][1]+2+c.stringWidth(dots,'Courier',8),yy,l); yy-=8
    c.setFillColor(black)
    y=yy-6
c.setFont('Helvetica',7); c.drawString(M,y-6,'- ITEM NOT ILLUSTRATED   ·   Indenture: 1 = assembly, each leading dot = one level below its next higher assembly (ATA iSpec 2200).')
c.showPage(); c.save()
print('pages',page[0])
