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
  {'id':1,'name':'회전 방패','reuse':'#04 회전 해머 외형','count':2,'centres_xz':[[58,30],[58,46]],'height_m':0,'allocated_seconds':44},
  {'id':2,'name':'갑옷 수레','reuse':'#03 왕복 밀대 외형','count':3,'centres_xz':[[102,70],[102,80],[102,90]],'height_m':3,'allocated_seconds':57},
  {'id':3,'name':'낙하 투구','reuse':'#10 낙하기물 외형','count':2,'centres_xz':[[58,112],[58,124]],'height_m':6,'allocated_seconds':60.6},
  {'id':4,'name':'나이트 돌진','reuse':'#17 고정 경로 돌진','count':1,'centres_xz':[[102,148]],'charge_from_xz':[102,148],'charge_to_xz':[102,140],'hazard_envelope_z':[138,150],'height_m':6,'allocated_seconds':64.2}],
 'transition':{'allocated_seconds':31,'last_hazard_z':150,'safe_terrace_xz':[80,170],'safe_terrace_size_m':[12,6],'clear_route_to_terrace_m':39,'arch_z':176,'arch_clear_width_m':12,'arch_min_height_m':5,'down_steps':4,'step_height_m':.25,'entry_strip_depth_m':4,'next_stage_height_m':5},
 'time_budget':{'reference_move_mps':2.5,'travel_seconds':LENGTH/2.5,'reaction_wait_seconds':60,'retry_seconds':45,'boundary_reading_seconds':15,'total_seconds':LENGTH/2.5+120,'goal':'평균 약4분~4분40초, 일반 플레이 5분 안쪽 목표 / 실측 전'},
 'source_documents':['manifest_v2_phase2.json: id=2-3','notes_v2_phase2.md: 2-3','01_설계_기준.md','02_장소별_코스_설계.md: 2-3','장소_목록_v2.json: id=2-3','04_미니게임_경계_설계.svg'],
 'scope_note':'첨부의 기존 6비트/평균4분은 참고. 최신 요청에 따라 장애물4종, 총8장치. 승강대/점핑패드/셔터는 제외. 모든 시간은 플레이테스트 전 추정.'}
assert LENGTH==342
assert len(PLAN['hazards'])==4 and sum(x['count'] for x in PLAN['hazards'])==8
assert all(b[1]>=a[1] for a,b in zip(ROUTE,ROUTE[1:]))
assert math.isclose(sum(x['allocated_seconds'] for x in PLAN['hazards'])+31,PLAN['time_budget']['total_seconds'])
assert 10+22+7==PLAN['transition']['clear_route_to_terrace_m']>=18
assert all(6<=x<=154 and 6<=z<=180 for x,z in ROUTE)
# Keep each checkpoint's full 6m-deep platform outside every hazard's conservative XZ envelope.
envelopes=[]
for hazard in PLAN['hazards']:
 for x,z in hazard['centres_xz']:
  if hazard['id'] in (1,3):envelopes.append((x-6,z-6,x+6,z+6))
  elif hazard['id']==2:envelopes.append((x-6,z-1,x+6,z+1))
  else:envelopes.append((96,138,108,150))
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
text(90,112,'왕실 무기 선반을 타고 올라가는 4개 장애물 코스',27,'#d6dfdf')
rect((1650,41,2188,143),'#324a57',r=13)
text(1680,54,'4종 / 8개 장치',29,'#f6d687');text(1680,99,'통과 목표 4~5분  ·  6대6',23,'#e6e9e3')
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
# Shield swept circles and rotor silhouettes.
for x,z in PLAN['hazards'][0]['centres_xz']:
 wc(x,z,6,'#e5bd88','#bb6641',2)
 px,py=P(x,z)
 for a in (20,140,260):
  angle=math.radians(a);end=(px+math.cos(angle)*22,py+math.sin(angle)*22)
  line([(px,py),end],'#765339',5);ellipse((end[0]-10,end[1]-10,end[0]+10,end[1]+10),'#52677b','#e4bb64',3)
 wc(x,z,.9,'#e8ca7b')
# Shield alcoves are isolated pockets outside the swept circles.
for z in (22,38):wr(46,z-2,52,z+2,'#96c1b2','#d8e7d8',2)
# Carts and withdrawal rails.
for x,z in PLAN['hazards'][1]['centres_xz']:
 line([P(89,z),P(115,z)],'#738b90',3)
 wr(x-6,z-1,x+6,z+1,'#586977','#d9b55e',3)
 for wheel in (x-4,x+4):wc(wheel,z-1.6,.65,'#263b48')
 px,py=P(x,z);poly([(px-9,py-13),(px+9,py-13),(px+7,py+7),(px,py+12),(px-7,py+7)],'#c9d0cc','#c59f4a',2)
# Isolated waiting recesses, not a continuous bypass.
for z in (75,85):wr(90,z-2,96,z+2,'#96c1b2','#d8e7d8',2)
# Warning helmets with boxed cover pockets.
for x,z in PLAN['hazards'][2]['centres_xz']:
 wc(x,z,6,'#edc9a4','#bf794c',3)
 px,py=P(x,z);ellipse((px-13,py-18,px+13,py+10),'#7f8e95','#e9c473',3)
 rect((px-9,py-4,px+9,py+2),'#293b45');line([(px,py-17),(px,py+9)],'#d3b36e',3)
for z in (106,118):
 wr(47,z-2,52,z+2,'#96c1b2','#d8e7d8',2)
 wr(49,z-1.4,51,z+1.4,'#76563d','#cdac6d',2)
# Knight charge envelope and a simple horse-head emblem.
wr(96,138,108,150,'#d2a28e','#b26648',3)
wr(108,142,114,146,'#96c1b2','#d8e7d8',2)
px,py=P(102,146)
poly([(px-15,py+17),(px-12,py),(px-22,py-4),(px-15,py-20),(px-7,py-27),(px-4,py-17),(px+9,py-12),(px+16,py+17)],'#596e7c','#e4bf66',3)
ellipse((px-10,py-15,px-6,py-11),'#f6e3a7')
arrow(P(102,142),P(102,139),'#a94e39',4,10)
# Progress arrows intentionally follow the actual route, including links between shelves.
for a,b in [((80,12),(66,12)),((58,22),(58,25)),((64,60),(69,60)),((94,60),(99,60)),((102,66),(102,68)),((102,96),(96,96)),((69,96),(63,96)),((58,106),(58,108)),((64,136),(71,136)),((92,136),(98,136)),((102,153),(102,157)),((97,160),(85,160)),((80,161),(80,166))]:arrow(P(*a),P(*b),'#b18a36',3,9)
# Room labels outside the path make four encounters easy to read.
labels=[(1,27,39,'회전 방패','H +0m'),(2,132,80,'갑옷 수레','H +3m'),(3,27,120,'낙하 투구','H +6m'),(4,132,148,'나이트 돌진','H +6m')]
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
# Four readable design cards.
right=1120
cards=[
 (1,'회전 방패 선반','2개 장치 · 약 40~50초','#d09a57',['큰 왕실 방패가 선반 폭을 쓸어내림','첫 방패로 학습 → 두 번째는 반대 위상','안전 홈에서 기다리거나 서로 밀며 통과']),
 (2,'갑옷 수레 통로','3개 장치 · 약 55~65초','#7292a0',['수레가 옆으로 빠진 틈에 다음 대기 칸으로','높은 수레를 뛰어넘는 해법은 제외','장치 사이 8m 안전 여유 · 엇박자 3회']),
 (3,'낙하 투구 진열대','2개 장치 · 약 55~65초','#c08359',['바닥 예고 원 → 거대 투구 낙하 → 충격파','무구 상자 엄폐와 짧은 전진을 반복','다른 팀을 밀어낼 수 있지만 엄폐도 공유']),
 (4,'나이트 훈련 레일','1개 장치 · 약 60~70초','#8e776e',['고정 8m 직선 돌진 · 플레이어 추적 없음','예고를 보고 옆 안전 홈에서 진입 타이밍 선택','돌진은 출구 반대 방향 · 마지막은 안전 정리'])]
for i,(n,title,small,c,lines) in enumerate(cards):
 y=235+i*242
 rect((right,y,2186,y+218),'#faf7ef','#d0c4af',2,14)
 rect((right,y,right+9,y+218),c,r=4)
 ellipse((right+24,y+24,right+72,y+72),'#263e4e')
 text(right+48,y+22,str(n),29,'#f8e5b9',True);text(right+89,y+21,title,31)
 text(right+89,y+68,small,23,'#94703e')
 for j,s in enumerate(lines):text(right+30,y+112+j*32,'• '+s,23,'#465854')
rect((right,1222,2186,1435),'#253e4c',r=14)
text(right+28,1240,'시간 예산  약 4분 17초',32,'#f9df9f')
text(right+28,1298,'이동 137초 + 장치 대응 60초 + 재시도 45초',24,'#e3e9e2')
text(right+28,1338,'+ 출구 읽기 15초  /  걷기 평균 2.5m/s 가정',23,'#ccdcd5')
text(right+28,1380,'관문 잠금으로 시간을 채우지 않음 · 실측 후 조정',21,'#c4d8d1')
text(62,1470,'참고 이미지의 무구실 분위기 + 첨부 2-3 원안을 4종으로 재구성한 레벨디자인 약도',26)
text(62,1516,'CP는 장치 바로 앞 · 경사로는 고정 건축 · 높이 H는 입구 기준 · 시간/치수는 블록아웃 초안, Unity 미구현',22,'#6c756c')
im.save(OUT/'LOWER_PALACE_ARMORY_MINIMAP.png')
svg.append('</svg>');(OUT/'LOWER_PALACE_ARMORY_MINIMAP.svg').write_text('\n'.join(svg))
print(f'Generated PNG/SVG/JSON; {LENGTH:.0f}m, 4 hazard types, 8 modules; target {PLAN["time_budget"]["total_seconds"]:.1f}s')
