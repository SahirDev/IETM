# Line-art illustrations of F-16 systems for the HELIOS IETM cards (original drawings, 300x130).
import subprocess, os
ST='#C7D2FE'; HL='#A5B4FC'
import re as _re
def _dedupe(m):
    tag=m.group(0); seen={}
    for a in _re.finditer(r'([\w-]+)="[^"]*"',tag): seen[a.group(1)]=a.group(0)
    head=_re.match(r'<\w+',tag).group(0); tail='/>' if tag.endswith('/>') else '>'
    return head+' '+' '.join(seen.values())+tail
def svg(body, w=300, h=130):
    body=_re.sub(r'<\w+\s[^<>]*?/?>',_dedupe,body)
    return f'''<svg xmlns="http://www.w3.org/2000/svg" width="{w}" height="{h}" viewBox="0 0 {w} {h}">
<g fill="rgba(165,180,252,0.07)" stroke="{ST}" stroke-width="1.6" stroke-linejoin="round" stroke-linecap="round">{body}</g></svg>'''
D='stroke-width="1" opacity="0.65" fill="none"'           # detail line
H=f'fill="rgba(99,102,241,0.30)" stroke="{HL}" stroke-width="1.8"'   # highlighted part
# F-16 side profile, nose left (300x130)
SIDE="M10 72 L40 66 L62 62 L78 55 Q92 38 116 37 Q138 38 152 52 L214 54 L252 10 L268 9 L270 14 L262 52 L282 56 L292 60 L292 76 L282 80 L210 84 L120 88 L96 92 L86 86 L74 79 L40 76 Z"
CANOPY="M80 55 Q94 40 116 39 Q136 40 150 52"
# F-16 top view (from the 600x700 path) for use with a transform
TOP="M300,40 L311,85 L319,140 L325,200 L333,248 L350,296 L372,342 L546,452 L556,455 L558,506 L372,506 L357,520 L353,556 L468,606 L470,630 L352,628 L342,648 L324,668 L276,668 L258,648 L248,628 L130,630 L132,606 L247,556 L243,520 L228,506 L42,506 L44,455 L54,452 L228,342 L250,296 L267,248 L275,200 L281,140 L289,85 L300,40 Z"
def top(tx, ty, sc, extra=''):
    sw=1.6/sc
    extra=extra.replace('stroke-width="5"',f'stroke-width="{sw*1.1:.1f}"').replace('stroke-width="3.2"',f'stroke-width="{sw*1.1:.1f}"').replace('stroke-width="2.4"',f'stroke-width="{sw:.1f}"').replace('stroke-width="3"',f'stroke-width="{sw*0.7:.1f}"')
    return f'<g transform="translate({tx},{ty}) scale({sc})"><path d="{TOP}" stroke-width="{sw:.1f}"/>{extra}</g>'
I={}
# ---------------------------------------------------------------- systems
I['engine']=f'''
<path d="M40 40 L200 40 Q214 40 222 46 L258 52 L258 78 L222 84 Q214 90 200 90 L40 90 Z"/>
<ellipse cx="40" cy="65" rx="10" ry="25"/>
<ellipse cx="40" cy="65" rx="6" ry="17" {D}/><path d="M40 48 V82 M34 52 L46 78 M46 52 L34 78" {D}/>
<path d="M70 40 V90 M78 40 V90 M86 40 V90 M94 40 V90 M102 42 V88 M110 44 V86 M118 46 V84" {D}/>
<rect x="128" y="48" width="30" height="34" rx="6" {H}/>
<path d="M168 44 V86 M176 44 V86 M184 44 V86" {D}/>
<path d="M258 52 L284 56 L284 74 L258 78" /><path d="M262 56 L280 58 M262 74 L280 72 M262 65 H282" {D}/>
<path d="M60 90 L60 100 L120 100 L120 90 M80 100 V106 H100 V100" {D}/>
<text x="143" y="70" font-size="7" fill="{HL}" stroke="none" text-anchor="middle" font-family="sans-serif">COMB</text>'''
I['wings']=top(93,-2.6,0.19,f'''
<path d="M372,342 L546,452 L556,455 L558,506 L372,506 Z" fill="rgba(99,102,241,0.30)" stroke="{HL}" stroke-width="3.2"/>
<path d="M228,342 L54,452 L44,455 L42,506 L228,506 Z" fill="rgba(99,102,241,0.30)" stroke="{HL}" stroke-width="3.2"/>
<path d="M384,360 L540,458 M216,360 L60,458" stroke-dasharray="14 10" stroke-width="2.4" fill="none"/>
<path d="M392,506 L392,486 L520,486 L520,506 M208,506 L208,486 L80,486 L80,506" fill="none" stroke-width="2.4"/>''') + f'''
<text x="220" y="58" font-size="8" fill="{HL}" stroke="none" font-family="sans-serif">leading-edge flaps</text>
<text x="220" y="104" font-size="8" fill="{ST}" stroke="none" font-family="sans-serif">flaperons</text>'''
I['cockpit']=f'''
<path d="M14 82 L44 74 L70 70 L84 60 Q98 40 124 38 Q150 39 166 56 L250 60 L250 98 L80 100 L50 94 Z"/>
<path d="M86 60 Q100 42 124 40 Q148 41 164 56 Z" {H}/>
<path d="M118 40 L120 58" {D}/>
<path d="M130 54 L134 92 L156 92 L160 64 Q160 54 150 54 Z" {D}/><path d="M140 46 L150 46 L150 56" {D}/>
<path d="M98 66 L112 58 L116 70 Z" {D}/>
<path d="M96 72 H124 V84 H96 Z" {D}/><text x="110" y="80" font-size="5" fill="{ST}" stroke="none" text-anchor="middle" font-family="sans-serif">MFD</text>
<path d="M250 60 L268 64 M250 98 L268 94" {D}/>'''
I['cooling']=f'''
<rect x="110" y="34" width="80" height="62" rx="6" {H}/>
<path d="M120 40 V90 M128 40 V90 M136 40 V90 M144 40 V90 M152 40 V90 M160 40 V90 M168 40 V90 M176 40 V90 M184 40 V90" {D}/>
<path d="M30 52 H110 M30 78 H110" /><path d="M30 52 Q20 65 30 78" />
<path d="M190 58 H236 Q250 58 250 44 V30 M190 72 H236 Q262 72 262 44 V30"/>
<circle cx="66" cy="65" r="11"/><path d="M66 54 V76 M55 65 H77 M58 57 L74 73 M74 57 L58 73" {D}/>
<path d="M232 106 H272 M240 100 L252 106 L240 112" stroke="{HL}"/>
<path d="M14 65 H34 M26 59 L34 65 L26 71" stroke="{HL}"/>
<text x="150" y="112" font-size="8" fill="{HL}" stroke="none" text-anchor="middle" font-family="sans-serif">heat exchanger · ECS pack</text>'''
I['fuel']=f'''<path d="{SIDE}"/><path d="{CANOPY}" {D}/>
<rect x="152" y="58" width="26" height="22" rx="4" {H}/><rect x="182" y="58" width="24" height="22" rx="4" {H}/><rect x="210" y="60" width="22" height="18" rx="4" {H}/>
<path d="M100 104 Q130 96 190 98 Q214 100 222 104 Q214 110 190 111 Q130 112 100 104 Z" {H}/>
<path d="M150 92 V98 M180 92 V98" {D}/>'''
I['fcs']=top(10,-3.6,0.19,f'''
<path d="M372,486 L520,486 L520,506 L372,506 Z" fill="rgba(99,102,241,0.30)" stroke="{HL}" stroke-width="5"/>
<path d="M228,486 L80,486 L80,506 L228,506 Z" fill="rgba(99,102,241,0.30)" stroke="{HL}" stroke-width="5"/>
<path d="M353,556 L468,606 L470,630 L352,628 Z" fill="rgba(99,102,241,0.30)" stroke="{HL}" stroke-width="5"/>
<path d="M247,556 L132,606 L130,630 L248,628 Z" fill="rgba(99,102,241,0.30)" stroke="{HL}" stroke-width="5"/>''') + f'''
<path d="M190 104 H270"/><path d="M226 104 V64"/><rect x="216" y="38" width="20" height="30" rx="8" {H}/><path d="M212 104 L206 116 H246 L240 104" />
<path d="M168 58 Q200 30 232 26" {D} stroke-dasharray="3 3"/><text x="200" y="124" font-size="8" fill="{HL}" stroke="none" font-family="sans-serif">side-stick → FLCC → ISA</text>'''
I['hydraulics']=f'''
<rect x="40" y="46" width="60" height="40" rx="8" {H}/><circle cx="70" cy="66" r="12" {D}/><path d="M70 54 V78 M58 66 H82" {D}/>
<rect x="150" y="30" width="44" height="64" rx="10"/><path d="M150 52 H194" {D}/><path d="M160 40 H184" {D}/>
<circle cx="250" cy="62" r="20"/><path d="M230 62 H270" {D}/>
<path d="M100 56 H150 M100 76 H120 Q130 76 130 96 L130 106 L250 106 L250 82 M194 60 H230"/>
<text x="70" y="104" font-size="8" fill="{HL}" stroke="none" text-anchor="middle" font-family="sans-serif">pump</text>
<text x="172" y="112" font-size="8" fill="{ST}" stroke="none" text-anchor="middle" font-family="sans-serif">reservoir</text>
<text x="250" y="34" font-size="8" fill="{ST}" stroke="none" text-anchor="middle" font-family="sans-serif">accumulator</text>'''
I['electrical']=f'''
<path d="M50 42 H150 Q164 42 164 56 V76 Q164 90 150 90 H50 Z" {H}/><ellipse cx="50" cy="66" rx="8" ry="24"/>
<path d="M70 42 V90 M80 42 V90 M90 42 V90 M100 42 V90 M110 42 V90 M120 42 V90" {D}/><path d="M164 60 H184 V72 H164"/>
<path d="M210 30 L192 66 H210 L200 100 L232 56 H214 L226 30 Z"/>
<rect x="244" y="54" width="40" height="36" rx="4"/><path d="M252 54 V48 H258 V54 M270 54 V48 H276 V54" /><path d="M256 70 H264 M272 70 H280 M276 66 V74" {D}/>
<text x="106" y="106" font-size="8" fill="{HL}" stroke="none" text-anchor="middle" font-family="sans-serif">main generator</text>'''
I['jfs']=f'''
<path d="M40 50 H140 L170 44 V88 L140 82 H40 Z" {H}/><ellipse cx="40" cy="66" rx="7" ry="16"/>
<path d="M60 50 V82 M72 50 V82 M84 50 V82 M110 52 V80 M120 52 V80" {D}/>
<rect x="170" y="52" width="34" height="30" rx="4"/><path d="M178 60 H196 M178 74 H196" {D}/>
<circle cx="250" cy="64" r="26"/><path d="M228 54 Q250 46 272 54" {D}/><path d="M250 90 V104 H222 M210 104 H232" />
<text x="100" y="104" font-size="8" fill="{HL}" stroke="none" text-anchor="middle" font-family="sans-serif">jet fuel starter</text>
<text x="250" y="122" font-size="8" fill="{ST}" stroke="none" text-anchor="middle" font-family="sans-serif">EPU</text>'''
I['avionics']=f'''
<path d="M20 70 Q60 40 140 40 L240 40 L240 100 L140 100 Q60 100 20 70 Z"/>
<path d="M140 40 V100" stroke-dasharray="4 3"/>
<ellipse cx="128" cy="70" rx="7" ry="26" {H}/><path d="M128 44 V96 M122 58 H134 M121 70 H135 M122 82 H134" {D}/>
<path d="M135 70 H158" /><rect x="158" y="56" width="30" height="28" rx="3"/>
<path d="M90 70 Q80 58 70 54 M90 70 Q80 82 70 86 M76 70 Q66 62 58 60 M76 70 Q66 78 58 80" stroke="{HL}" fill="none"/>
<rect x="250" y="44" width="38" height="44" rx="4"/><rect x="255" y="49" width="28" height="26" {D}/><path d="M256 82 H282" {D}/>
<text x="128" y="118" font-size="8" fill="{HL}" stroke="none" text-anchor="middle" font-family="sans-serif">fire-control radar antenna</text>'''
I['armament']=f'''
<path d="M20 40 H160 M20 46 H160 M20 52 H160" />
<rect x="160" y="34" width="60" height="24" rx="6" {H}/><path d="M220 46 H246 M232 38 V54" {D}/>
<path d="M18 38 V54" />
<path d="M30 90 H230 L250 94 L230 98 H30 Z"/><path d="M30 90 L20 82 L20 106 L30 98 M70 90 L80 82 M70 98 L80 106 M200 90 L210 84 M200 98 L210 104" />
<path d="M236 90 Q248 94 236 98" {D}/>
<text x="120" y="28" font-size="8" fill="{HL}" stroke="none" text-anchor="middle" font-family="sans-serif">20 mm cannon</text>
<text x="130" y="122" font-size="8" fill="{ST}" stroke="none" text-anchor="middle" font-family="sans-serif">air-to-air missile on wingtip rail</text>'''
I['egress']=f'''
<path d="M120 112 L118 70 Q116 38 132 22 L148 18 L156 22 L152 60 L178 64 L184 112 Z" {H}/>
<path d="M132 22 L150 20 L148 44 L130 44 Z" {D}/>
<path d="M124 76 H176 M122 92 H180" {D}/><path d="M154 66 L176 70" {D}/>
<path d="M110 112 H192"/><path d="M196 20 V112 M206 20 V112" stroke-dasharray="5 4"/>
<path d="M78 70 V30 M68 42 L78 30 L88 42" stroke="{HL}"/>
<text x="230" y="70" font-size="8" fill="{ST}" stroke="none" font-family="sans-serif">rails</text>'''
I['fuselage']=f'''<path d="{SIDE}" {H}/><path d="{CANOPY}" {D}/>
<path d="M60 64 V77 M100 50 V90 M140 42 V88 M180 54 V86 M214 54 V84 M250 56 V82" {D} stroke-dasharray="3 3"/>
<path d="M150 70 L230 72" {D}/>
<text x="150" y="112" font-size="8" fill="{HL}" stroke="none" text-anchor="middle" font-family="sans-serif">fuselage stations · stabilisers · vertical tail</text>'''
I['inspections']=top(30,-3.6,0.19,f'''
<path d="M150,200 H450 M100,420 H500 M120,560 H480" stroke-dasharray="10 10" fill="none" stroke-width="3"/>''') + f'''
<rect x="170" y="22" width="80" height="96" rx="6"/><rect x="194" y="16" width="32" height="12" rx="3"/>
<path d="M184 48 l6 6 l12 -12 M184 74 l6 6 l12 -12 M184 100 l6 6 l12 -12" stroke="{HL}"/>
<path d="M210 54 H238 M210 80 H238 M210 106 H238" {D}/>'''
# ---------------------------------------------------------------- landing gear sections
WHEEL=lambda cx,cy,r: f'<circle cx="{cx}" cy="{cy}" r="{r}"/><circle cx="{cx}" cy="{cy}" r="{r*0.55}" {D}/><circle cx="{cx}" cy="{cy}" r="{r*0.18}"/>'
I['main_gear']=f'''
<path d="M100 14 H200 V26 H100 Z"/><path d="M150 26 L146 66" stroke-width="6" stroke="{ST}"/><path d="M146 66 L144 92" stroke-width="3.5" {H}/>
<path d="M154 30 L196 26 M154 46 L188 26" {D}/><path d="M146 60 L160 72 L148 84" {D}/>
{WHEEL(144,100,22)}
<path d="M200 20 L246 50 L238 58" /><text x="240" y="76" font-size="8" fill="{ST}" stroke="none" font-family="sans-serif">drag brace</text>'''
I['extension_retraction']=f'''
<path d="M70 22 H150" /><circle cx="90" cy="22" r="4"/>
<path d="M90 22 L118 104" stroke-width="5"/>{WHEEL(122,108,14)}
<rect x="150" y="40" width="80" height="20" rx="10" {H} transform="rotate(-8 190 50)"/><path d="M150 54 L104 66" stroke-width="2.4"/><circle cx="238" cy="38" r="4"/>
<path d="M40 100 Q30 50 70 28" stroke="{HL}" fill="none" stroke-dasharray="5 4"/><path d="M64 24 L72 27 L66 34" stroke="{HL}" fill="none"/>
<text x="190" y="88" font-size="8" fill="{HL}" stroke="none" text-anchor="middle" font-family="sans-serif">retraction actuator</text>'''
I['wheels_brakes']=f'''
{WHEEL(100,65,48)}
<rect x="170" y="28" width="16" height="74" rx="3"/><rect x="190" y="28" width="16" height="74" rx="3" {H}/><rect x="210" y="28" width="16" height="74" rx="3"/><rect x="230" y="28" width="16" height="74" rx="3" {H}/>
<path d="M164 65 H254" {D}/><path d="M150 40 Q170 34 170 40 M150 90 Q170 96 170 90" {D}/>
<text x="210" y="118" font-size="8" fill="{HL}" stroke="none" text-anchor="middle" font-family="sans-serif">brake disc stack · anti-skid</text>'''
I['steering']=f'''
<rect x="136" y="16" width="28" height="22" rx="4" {H}/><path d="M150 38 V80" stroke-width="5"/>
<path d="M150 80 Q170 84 168 96" stroke-width="3"/>{WHEEL(150,104,18)}
<path d="M96 30 Q150 4 204 30" stroke="{HL}" fill="none"/><path d="M96 30 L100 20 M96 30 L106 32 M204 30 L200 20 M204 30 L194 32" stroke="{HL}"/>
<text x="240" y="64" font-size="8" fill="{ST}" stroke="none" font-family="sans-serif">steering collar</text>'''
I['position_warning']=f'''
<rect x="60" y="24" width="180" height="84" rx="8"/>
<circle cx="100" cy="52" r="10" {H}/><circle cx="130" cy="52" r="10" {H}/><circle cx="160" cy="52" r="10" {H}/>
<text x="130" y="80" font-size="8" fill="{ST}" stroke="none" text-anchor="middle" font-family="sans-serif">NOSE · LEFT · RIGHT</text>
<path d="M206 34 V86" stroke-width="4"/><circle cx="206" cy="92" r="9"/><circle cx="206" cy="92" r="4" {D}/>
<text x="226" y="96" font-size="7" fill="{ST}" stroke="none" text-anchor="middle" font-family="sans-serif">DN</text>'''
if __name__=='__main__':
    os.makedirs('png',exist_ok=True)
    for k,b in I.items():
        open(f'png/{k}.svg','w').write(svg(b))
        subprocess.check_call(['rsvg-convert','-w','600','-h','260',f'png/{k}.svg','-o',f'png/{k}.png'])
    print(len(I))
