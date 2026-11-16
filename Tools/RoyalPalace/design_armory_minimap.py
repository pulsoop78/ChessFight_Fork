"""Draw an editable design schematic and PNG for the lower-palace armory; no Unity assets are changed."""
from pathlib import Path
import json, math, html
from PIL import Image, ImageDraw, ImageFont
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'Docs/PawnRush/Armory'
ROUTE=[(80,12),(58,12),(58,60),(102,60),(102,96),(58,96),(58,136),(102,136),(102,160),(80,160),(80,178)]
LENGTH=sum(math.dist(a,b) for a,b in zip(ROUTE,ROUTE[1:]))
PLAN={
 'id':'2-3','name':'아랫궁 · 근위대 무구실','status':'레벨디자인 초안 · Unity 미구현',
 'room_m':[160,180],'path_width_m':12,'route_xz_m':ROUTE,'route_planar_length_m':LENGTH,
 'course_heights_m':[0,3,6,6],'static_ramps':[{'from_xz':[70,60],'to_xz':[88,60],'rise_m':3},{'from_xz':[90,96],'to_xz':[72,96],'rise_m':3}],
 'checkpoints':[{'id':'S','xz':[80,12]},{'id':'C1','xz':[58,20]},{'id':'C2','xz':[102,64]},{'id':'C3','xz':[58,102]},{'id':'C4','xz':[102,133]},{'id':'E','xz':[80,170]}],
 'hazards':[
  {'id':1,'name':'랜덤 창 바닥','height_m':0,'envelope_xz':[52,24,64,48],'grid_columns':4,'grid_rows':6,'guarantee':'예고부터 상승까지 도달 가능한 연결 안전 경로 유지','allocated_seconds':49},
  {'id':2,'name':'듀라한 내려찍기','height_m':3,'envelope_xz':[96,70,108,94],'statues_xz':[[91,74],[113,87]],'weapon_styles':['긴 대검 타격선','넓은 도끼 부채꼴'],'allocated_seconds':61},
  {'id':3,'name':'휴대 방패 사격장','height_m':6,'envelope_xz':[52,108,64,128],'shield_supply_xz':[58,104],'shield_count_initial':3,'replenish_rule':'분실/낙하/구간 밖 반출 시 짧은 지연 후 재보급, 적이 보급을 영구 봉쇄할 수 없음','solo_passable':True,'allocated_seconds':66.6},
  {'id':4,'name':'갑옷 감시 회랑','height_m':6,'envelope_xz':[96,138,108,150],'observer_xz':[[94,145],[110,148]],'detection_rule':'시야 안 실제 자발적 이동만 검사, 강제 이동/래그돌 흔들림/기절 입력 제외','allocated_seconds':61.2}],
 'transition':{'allocated_seconds':31,'last_hazard_z':150,'safe_terrace_xz':[80,170],'safe_terrace_size_m':[12,6],'clear_route_to_terrace_m':39,'arch_z':176,'arch_clear_width_m':12,'arch_min_height_m':5,'down_steps':4,'step_height_m':.25,'entry_strip_depth_m':4,'next_stage_height_m':5},
 'time_budget':{'reference_move_mps':2.5,'travel_seconds':LENGTH/2.5,'reaction_wait_seconds':72,'retry_seconds':45,'boundary_reading_seconds':15,'total_seconds':LENGTH/2.5+132,'goal':'약4분30초, 일반 플레이 5분 안쪽 목표 / 실측 전'},
 'source_documents':['manifest_v2_phase2.json: id=2-3','notes_v2_phase2.md: 2-3','01_설계_기준.md','02_장소별_코스_설계.md: 2-3','장소_목록_v2.json: id=2-3','04_미니게임_경계_설계.svg'],
 'revision':2,'scope_note':'사용자가 확정한 새4종. 기존 방패회전/수레/낙하투구/나이트돌진은 제외. 현재 결과는 미니맵 기획만이며 Unity 미구현.'}
assert LENGTH==342
assert len(PLAN['hazards'])==4
assert all(b[1]>=a[1] for a,b in zip(ROUTE,ROUTE[1:]))
assert math.isclose(sum(x['allocated_seconds'] for x in PLAN['hazards'])+31,PLAN['time_budget']['total_seconds'])
assert 10+22+7==PLAN['transition']['clear_route_to_terrace_m']>=18
assert all(6<=x<=154 and 6<=z<=180 for x,z in ROUTE)
# Keep each checkpoint's full 6m-deep platform outside every hazard's conservative XZ envelope.
envelopes=[h['envelope_xz'] for h in PLAN['hazards']]
for cp in PLAN['checkpoints']:
 x,z=cp['xz'];half=6 if cp['id']=='E' else 9
 assert not any(x-half<hi_x and x+half>lo_x and z-3<hi_z and z+3>lo_z for lo_x,lo_z,hi_x,hi_z in envelopes),cp['id']
OUT.mkdir(parents=True,exist_ok=True)
(OUT/'LOWER_PALACE_ARMORY_LAYOUT.json').write_text(json.dumps(PLAN,ensure_ascii=False,indent=2)+'\n')
W,H=2240,1580
im=Image.new('RGB',(W,H),'#ede7da');d=ImageDraw.Draw(im)
font_path='/usr/share/fonts/opentype/noto/NotoSansCJK-Regular.ttc'
fonts={}
def font(n):
 if n not in fonts:fonts[n]=ImageFont.truetype(font_path,n)
 return fonts[n]
svg=[f'<svg xmlns="http://www.w3.org/2000/svg" width="{W}" height="{H}" viewBox="0 0 {W} {H}">', '<rect width="100%" height="100%" fill="#ede7da"/>']
def rect(box,fill,stroke=None,w=1,r=0):
 box=tuple(round(v,2) for v in box)
 if r:d.rounded_rectangle(box,radius=r,fill=fill,outline=stroke,width=w)
 else:d.rectangle(box,fill=fill,outline=stroke,width=w)
 x0,y0,x1,y1=box;svg.append(f'<rect x="{x0}" y="{y0}" width="{x1-x0}" height="{y1-y0}" rx="{r}" fill="{fill or "none"}" stroke="{stroke or "none"}" stroke-width="{w}"/>')
def line(points,color,w=2):
 points=[tuple(round(v,2) for v in p) for p in points];d.line(points,fill=color,width=w,joint='curve')
 svg.append('<polyline points="'+' '.join(f'{x},{y}' for x,y in points)+f'" fill="none" stroke="{color}" stroke-width="{w}" stroke-linejoin="round" stroke-linecap="round"/>')
def poly(points,fill,stroke=None,w=1):
 d.polygon(points,fill=fill)
 if stroke:d.line(points+[points[0]],fill=stroke,width=w)
 svg.append('<polygon points="'+' '.join(f'{x:.2f},{y:.2f}' for x,y in points)+f'" fill="{fill}" stroke="{stroke or "none"}" stroke-width="{w}"/>')
def ellipse(box,fill,stroke=None,w=1):
 d.ellipse(box,fill=fill,outline=stroke,width=w);x0,y0,x1,y1=box
 svg.append(f'<ellipse cx="{(x0+x1)/2}" cy="{(y0+y1)/2}" rx="{(x1-x0)/2}" ry="{(y1-y0)/2}" fill="{fill or "none"}" stroke="{stroke or "none"}" stroke-width="{w}"/>')
def text(x,y,s,size=24,c='#263645',center=False):
 if center:x-=d.textlength(s,font=font(size))/2
 d.text((x,y),s,font=font(size),fill=c)
 svg.append(f'<text x="{x:.2f}" y="{y+size*.84:.2f}" font-family="Noto Sans CJK KR,Noto Sans CJK SC,sans-serif" font-size="{size}" fill="{c}">{html.escape(s)}</text>')
def arrow(a,b,c='#c39b45',w=5,head=13):
 line([a,b],c,w);angle=math.atan2(b[1]-a[1],b[0]-a[0]);v=(math.cos(angle),math.sin(angle));n=(-v[1],v[0])
 poly([b,(b[0]-v[0]*head+n[0]*head*.55,b[1]-v[1]*head+n[1]*head*.55),(b[0]-v[0]*head-n[0]*head*.55,b[1]-v[1]*head-n[1]*head*.55)],c)
S=5.25;X=100;Y=326
P=lambda x,z:(X+x*S,Y+(180-z)*S)
def wr(x0,z0,x1,z1,fill,stroke=None,w=1):
 rect((*P(x0,z1),*P(x1,z0)),fill,stroke,w)
def wc(x,z,r,fill,stroke=None,w=1):
 px,py=P(x,z);ellipse((px-r*S,py-r*S,px+r*S,py+r*S),fill,stroke,w)
# Heading.
rect((0,0,W,188),'#243645');rect((54,42,65,140),'#cfab5e')
text(88,38,'아랫궁 · 근위대 무구실',53,'#fff6df')
text(90,112,'랜덤 창 바닥 → 듀라한 → 방패 사격장 → 감시 회랑',27,'#d6dfdf')
rect((1650,41,2188,143),'#324a57',r=13)
text(1680,54,'새 장애물 4종 / 4구간',29,'#f6d687');text(1680,99,'통과 목표 4~5분  ·  6대6',23,'#e6e9e3')
# Left map panel.
rect((50,223,1070,1435),'#faf7ef','#cfc3ab',2,18)
text(84,246,'미니맵 약도',33);text(84,293,'평면 160 × 180m 초안  ·  진행 +Z ↑',23,'#6a766f')
wr(0,0,160,180,'#d3c4a5','#ac8951',5)
wr(7,7,153,173,'#354650')
# Weapon shelves and stone piers line the walls; they are not bypass floors.
for side in (-1,1):
 for j,z in enumerate((19,43,73,103,134,158)):
  x0,x1=(1.8,6.8) if side<0 else (153.2,158.2)
  wr(x0,z-8,x1,z+8,'#74563d','#cfac69',2)
  for n in range(5):line([P((x0+x1)/2,z-6+n*3),P((x0+x1)/2,z-4+n*3)],'#c5ccd0',3)
 for z in (8,58,97,143,173):wr(7 if side<0 else 149,z-2,11 if side<0 else 153,z+2,'#b9aa89','#dfcc9c')
text(*P(25,174),'상부 무기 선반',19,'#d4ddda');text(*P(17,6),'진열 벽 / 아래층 보관 공간',19,'#e3decf')
# Walkable route.
line([P(*p) for p in ROUTE],'#b0874e',int(14*S))
line([P(*p) for p in ROUTE],'#e9d8ae',int(12*S))
# Deck plank seams along vertical runs.
for x,lo,hi in ((58,12,60),(102,60,96),(58,96,136),(102,136,160)):
 for z in range(lo,hi,3):line([P(x-5.7,z),P(x+5.7,z)],'#cfb98b',1)
# Large stable checkpoint decks.
for cp in PLAN['checkpoints']:
 x,z=cp['xz'];width=12 if cp['id']=='E' else 18
 wr(x-width/2,z-3,x+width/2,z+3,'#80b4a2','#d4e9da',2)
 px,py=P(x,z);text(px,py-12,'도착' if cp['id']=='E' else cp['id'],20,'#203e36',True)
# Static stairs / ramps are not extra obstacle types.
for a,b in [((70,60),(88,60)),((90,96),(72,96))]:
 lo,hi=sorted((a[0],b[0]))
 for x in range(int(lo),int(hi),3):line([P(x,a[1]-5),P(x,a[1]+5)],'#998061',2)
 arrow(P(*a),P(*b),'#577b84',3,10)
# Random spears: the green cells depict one example pattern, not a permanent safe route.
for row in range(6):
 start=(0,1,1,2,1,0)[row]
 for col in range(4):
  x=52+col*3;z=24+row*4
  safe=col in (start,start+1)
  color='#9dc7ad' if safe else ('#c98474' if (row+col)%2 else '#e0b36e')
  wr(x+.15,z+.15,x+2.85,z+3.85,color,'#8f876e',1)
  if not safe:
   px,py=P(x+1.5,z+2)
   line([(px,py+7),(px,py-6)],'#435766',3)
   poly([(px,py-11),(px-5,py-3),(px+5,py-3)],'#435766')
for z in (22,50):wr(46,z-2,52,z+2,'#96c1b2','#d8e7d8',2)
# Dullahan torsos have hollow necks instead of heads; two distinct strike footprints.
for x,z in ((91,74),(113,87)):
 wr(x-2.5,z-3,x+2.5,z+3,'#596d7c','#d7b36c',2)
 px,py=P(x,z);ellipse((px-6,py-6,px+6,py+6),'#263744','#d3b26d',2)
wr(96,70,108,78,'#dda88a','#bd7353',2)
line([P(91,74),P(108,74)],'#c6d1cf',8)
px,py=P(106,74);poly([(px+11,py),(px-5,py-5),(px-5,py+5)],'#ecdfbc')
poly([P(113,87),P(96,82),P(96,94)],'#c99081','#bb6655',2)
line([P(113,87),P(101,87)],'#8e7956',6)
px,py=P(102,87);poly([(px-5,py-14),(px+9,py-9),(px+9,py+9),(px-5,py+14)],'#73868c','#e4bd67',2)
for z in (81,96):wr(108,z-2,114,z+2,'#96c1b2','#d8e7d8',2)
# Carry shields: replenishing supply before the projectile zone.
wr(52,102.5,64,105.5,'#a6c9b3','#789b87',2)
for x in (54,58,62):
 px,py=P(x,104);poly([(px-9,py-10),(px+9,py-10),(px+7,py+7),(px,py+14),(px-7,py+7)],'#57768a','#e8c471',2)
# Crossbows fire into this bounded course zone, never back into the checkpoint.
for x in (54,58,62):
 px,py=P(x,129)
 line([(px-9,py),(px+9,py)],'#8b6747',5);line([(px,py-7),(px,py+7)],'#d5b567',4)
 for z in (124,118,112):arrow(P(x,z+2),P(x,z),'#b27b49',2,7)
for x in range(52,64,2):line([P(x,108),P(x+1,108)],'#658e99',3)
for x,z in ((52,116),(64,122)):
 wr(x-1.5,z-2,x+1.5,z+2,'#77583e','#d6b577',2)
# Watching armour: cones are interrupted by display stands; gray cone is a look-away example.
poly([P(96,144),P(108,138),P(108,150)],'#d3a194','#b67462',2)
poly([P(108,148),P(96,142),P(96,150)],'#a8bfc0','#74999e',2)
for x,z,color in ((94,145,'#c47867'),(110,148,'#7e9e9d')):
 wr(x-1.5,z-2,x+1.5,z+2,'#5d7281','#dcc179',2)
 px,py=P(x,z);ellipse((px-10,py-5,px+10,py+5),'#f4e3c1',color,2);ellipse((px-3,py-3,px+3,py+3),color)
for x,z in ((97,140),(106,146)):
 wr(x-1,z-1,x+1,z+1,'#70553e','#d4b26c',2)
for x,z in ((94,140),(110,146)):
 wr(x-2,z-1.5,x+2,z+1.5,'#91b8a7','#d8e7d8',2)
# Progress arrows intentionally follow the actual route, including links between shelves.
for a,b in [((80,12),(66,12)),((58,22),(58,25)),((64,60),(69,60)),((94,60),(99,60)),((102,66),(102,68)),((102,96),(96,96)),((69,96),(63,96)),((58,106),(58,108)),((64,136),(71,136)),((92,136),(98,136)),((102,153),(102,157)),((97,160),(85,160)),((80,161),(80,166))]:arrow(P(*a),P(*b),'#b18a36',3,9)
# Room labels outside the path make four encounters easy to read.
labels=[(1,27,39,'랜덤 창 바닥','H +0m'),(2,132,80,'듀라한','H +3m'),(3,27,120,'방패 사격장','H +6m'),(4,132,148,'감시 회랑','H +6m')]
for n,x,z,title,height in labels:
 px,py=P(x,z);ellipse((px-22,py-24,px+22,py+20),'#243b4c','#d8b36a',2)
 text(px,py-21,str(n),27,'#ffefc9',True);text(px,py+26,title,22,'#ffedc4',True);text(px,py+60,height,18,'#cfddd9',True)
# Boundary: safe terrace, open arch, four shallow steps to an unspecified next stage.
for x in (73,87):wr(x-.6,175,x+.6,178,'#d4ac62')
line([P(74,176),P(86,176)],'#b28a43',4)
for z in (176.4,176.9,177.4,177.9):line([P(74,z),P(86,z)],'#d9c69e',2)
arrow(P(80,178),P(80,185),'#548783',4,12)
px,py=P(80,185);text(px,py-25,'다음 구간',22,'#34585c',True)
text(*P(107,160),'여기부터 안전 동선',18,'#d9e8d8')
# Width scale and legend.
xa,ya=P(15,-5);xb,yb=P(35,-5);line([(xa,ya),(xb,yb)],'#4b615f',4)
text((xa+xb)/2,ya+8,'20m',18,'#4b615f',True)
text(100,1334,'바닥색 영역만 보행 가능 · 어두운 부분은 아래층 / 낙하 복귀',21,'#5e6b68')
for x,c,label in [(100,'#e9d8ae','주동선 12m'),(340,'#80b4a2','CP / 대기 단'),(605,'#d2a28e','위험 범위')]:
 rect((x,1384,x+25,1409),c,'#80918e');text(x+36,1380,label,21)
text(100,1410,'창: 녹색 안전 / 황색 예고 / 적색 상승 (예시)',18,'#63776b')
# Four readable design cards.
right=1120
cards=[
 (1,'랜덤 창 바닥','예고를 보고 안전 칸으로 · 약 45~55초','#b77969',['바닥에 예고가 뜬 뒤 창이 올라오는 구간','현재 칸→다음 칸으로 도달 가능한 안전길 보장','초반은 넓은 띠, 후반은 꺾이는 경로']),
 (2,'듀라한 내려찍기','대검 / 도끼의 공격 범위 구분 · 약 55~65초','#94765c',['대검은 길게, 도끼는 넓게 내려찍음','무기 자세와 바닥 그림자로 공격 예고','무기가 바닥에 박힌 뒤 회수할 때 전진']),
 (3,'휴대 방패 석궁 사격장','들고 이동하는 엄폐물 · 약 60~70초','#6a90a0',['진입 보급대에서 방패를 잡아 화살을 막기','혼자도 통과 가능 · 방패 뒤로 팀원이 동행','방패 분실 시 재보급, 화살은 사격 구역 한정']),
 (4,'갑옷 감시 회랑','시선 안에서는 정지 · 약 55~65초','#8c777d',['진열 갑옷이 고개를 돌릴 때 다음 엄폐로','눈이 켜진 시선 안에서 직접 움직이면 발각','상대의 밀치기·넉백으로 움직인 것은 제외'])]
for i,(n,title,small,c,lines) in enumerate(cards):
 y=235+i*242
 rect((right,y,2186,y+218),'#faf7ef','#d0c4af',2,14)
 rect((right,y,right+9,y+218),c,r=4)
 ellipse((right+24,y+24,right+72,y+72),'#263e4e')
 text(right+48,y+22,str(n),29,'#f8e5b9',True);text(right+89,y+21,title,31)
 text(right+89,y+68,small,23,'#94703e')
 for j,s in enumerate(lines):text(right+30,y+112+j*32,'• '+s,23,'#465854')
rect((right,1222,2186,1435),'#253e4c',r=14)
text(right+28,1240,'시간 예산  약 4분 29초',32,'#f9df9f')
text(right+28,1298,'이동 137초 + 장치 대응 72초 + 재시도 45초',24,'#e3e9e2')
text(right+28,1338,'+ 출구 읽기 15초  /  걷기 평균 2.5m/s 가정',23,'#ccdcd5')
text(right+28,1380,'관문 잠금으로 시간을 채우지 않음 · 실측 후 조정',21,'#c4d8d1')
text(62,1470,'새 무구실 v2 · 창 바닥 / 듀라한 / 방패 사격장 / 감시 회랑',26)
text(62,1516,'CP는 위험 구역 밖 · 창/시선 색은 한 패턴의 예시 · 시간과 치수는 실측 전 · Unity 미구현',22,'#6c756c')
im.save(OUT/'LOWER_PALACE_ARMORY_MINIMAP.png')
svg.append('</svg>');(OUT/'LOWER_PALACE_ARMORY_MINIMAP.svg').write_text('\n'.join(svg))
print(f'Generated PNG/SVG/JSON; {LENGTH:.0f}m, 4 new hazard types; target {PLAN["time_budget"]["total_seconds"]:.1f}s')
