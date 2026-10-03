from pathlib import Path
import re
import html
import json
from math import atan2, cos, sin, pi

from reportlab.pdfgen import canvas
from reportlab.platypus import BaseDocTemplate, Frame, PageTemplate, Paragraph, Spacer, Table, TableStyle, PageBreak, Flowable, KeepTogether
from reportlab.lib.styles import ParagraphStyle
from reportlab.lib import colors
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont
from pypdf import PdfReader

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'output/pdf'
TMP = ROOT / 'tmp/pdfs'
SOURCE = OUT / 'SurakshaXR_Internal_Guide.md'
DEST = OUT / 'SurakshaXR_Internal_Guide.pdf'
W, H = 612, 792
LEFT, RIGHT, TOP, BOTTOM = 54, 54, 54, 48
CW = W - LEFT - RIGHT

for name, fn in [('Guide', 'arial.ttf'), ('GuideBold', 'arialbd.ttf'), ('GuideItalic', 'ariali.ttf')]:
    pdfmetrics.registerFont(TTFont(name, 'C:/Windows/Fonts/' + fn))
pdfmetrics.registerFontFamily('Guide', normal='Guide', bold='GuideBold', italic='GuideItalic', boldItalic='GuideBold')
NAVY = colors.HexColor('#16313F')
TEAL = colors.HexColor('#006D77')
INK = colors.HexColor('#18242B')
MUTED = colors.HexColor('#50616B')
LINE = colors.HexColor('#CEDADF')
PALE = colors.HexColor('#EFF5F5')
LIGHT = colors.HexColor('#F6F8F9')

styles = {
    'body': ParagraphStyle('body', fontName='Guide', fontSize=10.5, leading=14.6, textColor=INK, spaceAfter=8),
    'h1': ParagraphStyle('h1', fontName='GuideBold', fontSize=23, leading=26.5, textColor=colors.black, spaceAfter=14),
    'h2': ParagraphStyle('h2', fontName='GuideBold', fontSize=13, leading=16, textColor=colors.black, spaceBefore=9, spaceAfter=7),
    'cell': ParagraphStyle('cell', fontName='Guide', fontSize=9.5, leading=12.4, textColor=INK, splitLongWords=1),
    'cellhead': ParagraphStyle('cellhead', fontName='GuideBold', fontSize=9.4, leading=12, textColor=colors.white),
    'sourcecell': ParagraphStyle('sourcecell', fontName='Guide', fontSize=9, leading=11.8, textColor=INK, splitLongWords=1),
    'code': ParagraphStyle('code', fontName='Courier', fontSize=9.3, leading=13.5, textColor=INK, backColor=LIGHT, borderPadding=10, spaceBefore=4, spaceAfter=9),
    'step': ParagraphStyle('step', fontName='Guide', fontSize=10.3, leading=14, textColor=INK, leftIndent=16, firstLineIndent=-16, spaceAfter=6),
    'meta': ParagraphStyle('meta', fontName='GuideBold', fontSize=9, leading=12.5, textColor=TEAL, spaceAfter=14),
    'subtitle': ParagraphStyle('subtitle', fontName='Guide', fontSize=13, leading=17, textColor=MUTED, spaceAfter=12),
}

MERMAID = {
'web': '''flowchart LR
    W["React web UI: connection-check button"] --> H["GET /health: Vite development proxy"]
    H --> F["FastAPI: status and version JSON"]''',
'architecture': '''flowchart TD
    C["Bundled JSON, layouts and language tables"] --> P["PreviewApp: screens, choices and view selection"]
    P --> S["TrainingSessionService: one attempt and completion"]
    S --> D["ScenarioRuntime and QuestionEngine: steps, decisions and scores"]
    D -->|step state| V["SimulatorView: Ground AR, Immersive Mine or 3D"]
    D -->|results via session service| K["Certificate and trust helpers: canonicalization and Ed25519"]
    K --> L["LocalStore: SQLite transaction, history, progress, refreshers and outbox"]
    S -->|completed attempt, with optional certificate| L''',
'worker': '''flowchart TD
    A["Bootstrap: content, database, preferences and issuer"] --> B["Select local worker and module"]
    B --> C["Choose view and session mode"]
    C --> D["Scenario action loop: instruction, choice, feedback"]
    D -->|Practice| P["Practice completion"]
    D -->|Assessment or Refresher| Q["Knowledge quiz and combined result"]
    P --> R["Save completion; certificate only for eligible passing Assessment"]
    Q --> R''',
'ar': '''flowchart TD
    A["Camera permission and AR availability"] --> B["Track surfaces and collect layout observations"]
    B --> C["Validate footprints, route support and timing"]
    C --> D["Environment Ready and three-second countdown"]
    D --> E["Create native anchor and show training scene"]
    A -->|unavailable| F["Use existing 3D view"]
    B -->|setup cannot become usable| F
    E -->|tracking interruption| G["Pause input and show recovery guidance"]
    G -->|tracking recovers| E''',
'certificate': '''flowchart TD
    subgraph Create
        A["Eligible passing Assessment"] --> B["Payload and canonical UTF-8 bytes"]
        B --> C["Ed25519 signature and envelope"]
        C --> D["Certificate record in completion transaction"]
        D --> E["Generate QR pixels from saved envelope for display"]
    end
    subgraph Verify
        F["Read QR image or envelope text"] --> G["Decode and validate canonical payload"]
        G --> H["Resolve signer in local trust bundle"]
        H --> I["Check signature and trust rules"]
        I --> J["Show verification result"]
    end'''
}

def markup(text):
    text = html.escape(text)
    text = re.sub(r'\*\*(.+?)\*\*', r'<b>\1</b>', text)
    text = re.sub(r'`(.+?)`', r'<font name="Courier">\1</font>', text)
    text = text.replace('BouncyCastle.Cryptography', 'BouncyCastle.<br/>Cryptography')
    return text

def para(text, sty='body'):
    return Paragraph(markup(text), styles[sty])

class Diagram(Flowable):
    def __init__(self, kind):
        Flowable.__init__(self)
        self.kind = kind
        self.width = CW
        self.height = {'architecture':306, 'worker':294, 'ar':278, 'certificate':290, 'web':44}[kind]
        self.spaceBefore = 3
        self.spaceAfter = 10

    def box(self, x, top, w, h, title, detail='', strong=False):
        c = self.canv
        y = self.height - top - h
        c.setFillColor(NAVY if strong else PALE)
        c.setStrokeColor(NAVY if strong else LINE)
        c.setLineWidth(.8)
        c.roundRect(x, y, w, h, 6, fill=1, stroke=1)
        title_style = ParagraphStyle('node', fontName='GuideBold', fontSize=9.6, leading=11.5,
            textColor=colors.white if strong else INK, alignment=1)
        detail_style = ParagraphStyle('detail', fontName='Guide', fontSize=8.8, leading=10.5,
            textColor=colors.white if strong else MUTED, alignment=1)
        a = Paragraph(html.escape(title), title_style)
        _, ah = a.wrap(w-16, h)
        b = Paragraph(html.escape(detail), detail_style) if detail else None
        bh = b.wrap(w-16, h)[1] if b else 0
        th = ah + (3 + bh if b else 0)
        if th > h-8:
            raise ValueError(f'Node text exceeds box: {title} {th} > {h-8}')
        ty = y + (h + th)/2
        a.drawOn(c, x+8, ty-ah)
        if b: b.drawOn(c, x+8, ty-ah-3-bh)

    def path(self, points, head=True, both=False):
        c = self.canv
        p = [(x, self.height-y) for x,y in points]
        c.setStrokeColor(TEAL)
        c.setFillColor(TEAL)
        c.setLineWidth(1.35)
        path = c.beginPath()
        path.moveTo(*p[0])
        for point in p[1:]: path.lineTo(*point)
        c.drawPath(path)
        def tip(a,b):
            angle = atan2(b[1]-a[1], b[0]-a[0])
            q=c.beginPath(); q.moveTo(*b)
            q.lineTo(b[0]-5*cos(angle-pi/6), b[1]-5*sin(angle-pi/6))
            q.lineTo(b[0]-5*cos(angle+pi/6), b[1]-5*sin(angle+pi/6)); q.close()
            c.drawPath(q, fill=1, stroke=0)
        if head: tip(p[-2],p[-1])
        if both: tip(p[1],p[0])

    def label(self, text, x, top, w=160, align=1):
        st = ParagraphStyle('label', fontName='Guide', fontSize=8.2, leading=9.8, alignment=align, textColor=MUTED)
        p=Paragraph(html.escape(text),st)
        _, h=p.wrap(w,100)
        p.drawOn(self.canv,x,self.height-top-h)

    def draw(self):
        k=self.kind
        if k=='web':
            self.box(0,0,152,42,'React web UI','Connection-check button')
            self.box(176,0,152,42,'GET /health','Vite development proxy')
            self.box(352,0,152,42,'FastAPI','Status and version JSON')
            self.path([(152,21),(176,21)])
            self.path([(328,21),(352,21)])
        elif k=='architecture':
            self.box(0,0,160,48,'Bundled resources','JSON, layouts, languages')
            self.box(190,0,314,48,'PreviewApp','Screens, worker choices and view selection')
            self.path([(160,24),(190,24)])
            self.box(80,70,344,42,'TrainingSessionService','Owns the attempt, quiz and completion',True)
            self.path([(347,48),(347,59),(252,59),(252,70)])
            self.box(80,134,344,42,'ScenarioRuntime + QuestionEngine','Steps, decisions, category scores and quiz')
            self.path([(252,112),(252,134)])
            self.box(0,204,230,51,'SimulatorView + OptionalArSession','Ground AR / Immersive Mine / 3D')
            self.box(274,204,230,51,'Certificate + trust helpers','Results arrive through the session service')
            self.path([(170,176),(170,190),(115,190),(115,204)])
            self.path([(334,176),(334,190),(389,190),(389,204)])
            self.box(274,272,230,34,'LocalStore -> SQLite','Records, progress, refreshers, outbox')
            self.path([(389,255),(389,272)])
            self.path([(424,91),(492,91),(492,186),(504,186),(504,266),(504,288),(495,288)])
            self.label('Completion saves an attempt even when no certificate is issued.',0,269,238)
        elif k=='worker':
            nodes=[(0,'Bootstrap','Load content, database, preferences and issuer'),
                   (53,'Select worker and module','Local profile and bundled training content'),
                   (106,'Choose view and session mode','View selection; Practice, Assessment or due Refresher'),
                   (159,'Run the scenario action loop','Instruction -> explicit choice -> feedback')]
            for top,title,detail in nodes:
                self.box(24,top,456,37,title,detail,strong=top==159)
            for a,b in [(37,53),(90,106),(143,159)]: self.path([(252,a),(252,b)])
            self.box(0,216,165,34,'Practice completion')
            self.box(195,216,309,34,'Knowledge quiz + combined result','Assessment or Refresher')
            self.path([(152,196),(152,204),(82.5,204),(82.5,216)])
            self.path([(352,196),(352,216)])
            self.box(0,270,504,24,'Save completion; certificate only for eligible passing Assessment')
            self.path([(82.5,250),(82.5,260),(220,260),(220,270)])
            self.path([(350,250),(350,260),(284,260),(284,270)])
        elif k=='ar':
            main=[(0,'Permission + AR availability','AR services must already be installed'),
                  (58,'Track surfaces and collect observations','ARCore tracking + measured horizontal planes'),
                  (116,'Validate the local layout','Footprints, routes, ground variation and timing'),
                  (174,'Environment Ready','Three-second countdown'),
                  (232,'Create native anchor + show scene','Continue through the shared scenario engine')]
            for top,title,detail in main:self.box(0,top,319,42,title,detail,strong=top==232)
            for t in [42,100,158,216]:self.path([(159.5,t),(159.5,t+16)])
            self.box(356,89,148,58,'Existing 3D view','Available if AR setup cannot be used')
            self.path([(319,21),(430,21),(430,89)])
            self.label('Unavailable',326,43,98)
            self.path([(319,79),(337,79),(337,118),(356,118)])
            self.box(356,221,148,57,'Tracking recovery','Pause input; keep the session and anchor')
            self.path([(319,245),(356,245)],both=True)
            self.label('Loss / recovery',348,191,156)
        elif k=='certificate':
            self.label('CREATE ON THE PHONE',0,0,240)
            self.label('VERIFY ON THE PHONE',264,0,240)
            left=[('Eligible passing Assessment','Result is certificate-eligible'),
                  ('Canonical payload bytes','Fixed UTF-8 representation'),
                  ('Ed25519 signature + envelope','Payload and signature travel together'),
                  ('Certificate record saved','Part of the completion transaction'),
                  ('Generate QR for display','Pixels come from the saved envelope')]
            right=[('Read QR or envelope text','Camera decode or pasted payload'),
                   ('Decode and validate','Envelope, fields and canonical bytes'),
                   ('Resolve trusted signer','Verified local trust bundle'),
                   ('Check signature and trust','Ed25519, validity and revocation rules'),
                   ('Show verification result','No certificate website lookup')]
            for x,items in [(0,left),(264,right)]:
                for i,(title,detail) in enumerate(items):
                    top=26+i*55
                    self.box(x,top,240,40,title,detail,strong=i==4)
                    if i<4:self.path([(x+120,top+40),(x+120,top+55)])

def table_from(lines, section):
    rows=[]
    for line in lines:
        bits=[x.strip() for x in line.strip().strip('|').split('|')]
        if all(re.fullmatch(r':?-+:?',b) for b in bits): continue
        rows.append(bits)
    n=len(rows[0])
    if n==2:
        widths = [40,CW-40] if section==14 else ([200,CW-200] if section==6 else ([174,CW-174] if section==1 else [122,CW-122]))
    else:
        widths={2:[133,143,228],3:[148,128,228],7:[78,213,213],9:[94,221,189]}.get(section,[CW/n]*n)
    data=[]
    for ri,row in enumerate(rows):
        style='cellhead' if ri==0 else ('sourcecell' if section==14 else 'cell')
        data.append([para(cell,style) for cell in row])
    t=Table(data,colWidths=widths,hAlign='LEFT',repeatRows=1)
    t.setStyle(TableStyle([
        ('BACKGROUND',(0,0),(-1,0),NAVY),
        ('ROWBACKGROUNDS',(0,1),(-1,-1),[colors.white,LIGHT]),
        ('VALIGN',(0,0),(-1,-1),'TOP'),
        ('LEFTPADDING',(0,0),(-1,-1),8),('RIGHTPADDING',(0,0),(-1,-1),8),
        ('TOPPADDING',(0,0),(-1,-1),6),('BOTTOMPADDING',(0,0),(-1,-1),6),
        ('LINEBELOW',(0,0),(-1,0),.5,NAVY),
        ('LINEBELOW',(0,1),(-1,-1),.35,LINE),
    ]))
    t.spaceBefore=3;t.spaceAfter=10
    return t

def parse_section(text, number):
    lines=text.strip().splitlines()
    result=[];i=0
    while i<len(lines):
        line=lines[i].strip()
        if not line:i+=1;continue
        if line.startswith('# '):
            if number==1:result.append(para('SURAKSHAXR  /  ENGINEERING EXPLAINED','meta'))
            else:result.append(para(f'{number:02d}  /  INTERNAL GUIDE','meta'))
            result.append(para(line[2:],'h1'));i+=1;continue
        if line.startswith('## '):result.append(para(line[3:],'h2'));i+=1;continue
        if line.startswith('|'):
            group=[]
            while i<len(lines) and lines[i].strip().startswith('|'):group.append(lines[i]);i+=1
            result.append(table_from(group,number));continue
        match=re.fullmatch(r'\{\{diagram:(\w+)\}\}|<!-- diagram:(\w+) -->',line)
        if match:
            result.append(Diagram(match.group(1) or match.group(2)))
            i+=1
            if i<len(lines) and lines[i].startswith('```mermaid'):
                i+=1
                while i<len(lines) and not lines[i].startswith('```'):i+=1
                i+=1
            continue
        if line.startswith('```'):
            group=[];i+=1
            while i<len(lines) and not lines[i].startswith('```'):group.append(html.escape(lines[i]));i+=1
            result.append(Paragraph('<br/>'.join(group),styles['code']));i+=1;continue
        if re.match(r'^\d+\. ',line):result.append(para(line,'step'));i+=1;continue
        group=[line];i+=1
        while i<len(lines) and lines[i].strip() and not lines[i].lstrip().startswith(('#','|','```','{{','<!--')):
            if re.match(r'^\d+\. ',lines[i]):break
            group.append(lines[i].strip());i+=1
        txt=' '.join(group)
        sty='body'
        if number==1 and txt.startswith('How the Android'):sty='subtitle'
        if number==1 and txt.startswith('ForgeIndia'):sty='meta'
        result.append(para(txt,sty))
    return result

class NumberCanvas(canvas.Canvas):
    def __init__(self,*args,**kwargs):
        canvas.Canvas.__init__(self,*args,**kwargs);self._pages=[]
    def showPage(self):
        self._pages.append(dict(self.__dict__));self._startPage()
    def save(self):
        count=len(self._pages)
        for state in self._pages:
            self.__dict__.update(state)
            self.footer(count)
            canvas.Canvas.showPage(self)
        canvas.Canvas.save(self)
    def footer(self,count):
        self.setStrokeColor(LINE);self.setLineWidth(.6);self.line(LEFT,36,W-RIGHT,36)
        self.setFillColor(MUTED);self.setFont('Guide',8)
        self.drawString(LEFT,23,'SURAKSHAXR  |  Architecture and internal workflow')
        self.drawRightString(W-RIGHT,23,f'{self._pageNumber:02d} / {count:02d}')
        if self._pageNumber>1:
            self.setFont('Guide',7.7);self.drawRightString(W-RIGHT,H-30,'FORGEINDIA  /  SIH26041')

text=SOURCE.read_text(encoding='utf-8')
sections=text.split('<!-- page -->')
story=[]
for n,section in enumerate(sections,1):
    if n>1:story.append(PageBreak())
    story.extend(parse_section(section,n))
doc=BaseDocTemplate(str(DEST),pagesize=(W,H),leftMargin=LEFT,rightMargin=RIGHT,topMargin=TOP,bottomMargin=BOTTOM,
    title='SurakshaXR: Internal Architecture and Workflow',author='ForgeIndia',
    subject='How the Android app is assembled and how its training, AR, data and certificates work')
doc.addPageTemplates(PageTemplate(id='body',frames=Frame(LEFT,BOTTOM,CW,H-TOP-BOTTOM,leftPadding=0,rightPadding=0,topPadding=0,bottomPadding=0)))
doc.build(story,canvasmaker=NumberCanvas)
reader=PdfReader(DEST)
page_data=[{'page':i+1,'chars':len(p.extract_text()),'first_lines':p.extract_text().splitlines()[:4]} for i,p in enumerate(reader.pages)]
(TMP/'text-check.json').write_text(json.dumps(page_data,indent=2),encoding='utf-8')
all_text='\n'.join(p.extract_text() for p in reader.pages)
assert '{{diagram' not in all_text and '<!--' not in all_text
for term in ['ScenarioRuntime','TrainingSessionService','SQLite','AR Foundation','Ed25519','Santali','FastAPI','IL2CPP']:
    assert term in all_text, term
for key,code in MERMAID.items():
    text=text.replace('{{diagram:'+key+'}}','<!-- diagram:'+key+' -->\n```mermaid\n'+code+'\n```')
SOURCE.write_text(text,encoding='utf-8')
print(json.dumps({'pdf':str(DEST),'pages':len(reader.pages),'bytes':DEST.stat().st_size,'page_text':page_data},indent=2))
