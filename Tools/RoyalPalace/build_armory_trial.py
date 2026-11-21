#!/usr/bin/env python3
"""Saved grayscale armory playtest prefab and a scene containing a single instance.

--write creates new assets. --check is read-only. --overwrite explicitly replaces only
this generator's new assets; no existing course, scene or shared material is edited.
"""
import argparse
import hashlib
import json
import math
from pathlib import Path
import re
import subprocess
import sys
import build_courses as kit
from stable_prefab_ids import stabilize

ROOT=kit.ROOT
PREFAB='Assets/Prefabs/Obstacles/LowerPalaceArmoryTrial.prefab'
SCENE='Assets/Scenes/LowerPalaceArmoryTrial.unity'
PLAN=json.loads((ROOT/'Docs/PawnRush/Armory/LOWER_PALACE_ARMORY_LAYOUT.json').read_text())
SCRIPT_PATHS={
 'ArmoryCourseContext':'Assets/Scripts/Gameplay/ImportedObstacles/ArmoryCourseContext.cs',
 'ArmoryCourseTrial':'Assets/Scripts/RagdollLab/ArmoryCourseTrial.cs',
 'ArmorySpearFloor':'Assets/Scripts/Gameplay/ImportedObstacles/ArmorySpearFloor.cs',
 'ArmoryDullahan':'Assets/Scripts/Gameplay/ImportedObstacles/ArmoryDullahan.cs',
 'ArmoryCarryShield':'Assets/Scripts/Gameplay/ImportedObstacles/ArmoryCarryShield.cs',
 'ArmoryCrossbowGallery':'Assets/Scripts/Gameplay/ImportedObstacles/ArmoryCrossbowGallery.cs',
 'ArmoryWatcherGallery':'Assets/Scripts/Gameplay/ImportedObstacles/ArmoryWatcherGallery.cs'}
for name,path in SCRIPT_PATHS.items():kit.SCRIPT[name]=kit.guid(path)
MATERIALS={name:kit.material(name,(v,v,v)) for name,v in [('ArmoryTrialGray',.53),('ArmoryTrialDarkGray',.22),('ArmoryTrialLightGray',.78)]}
kit.MAT.update(MATERIALS)


def clean(s):return '\n'.join(line.rstrip() for line in s.splitlines())+'\n'
def refs(name,items):return '  '+name+':\n'+''.join('  - '+kit.ref(n)+'\n' for n in items)
def blocks(s):return {int(m[1]):part for part in re.split(r'(?=^--- !u!)',s,flags=re.M) if (m:=re.match(r'--- !u!\d+ &(\d+)',part))}

class Builder:
    def __init__(self):
        self.d=kit.Doc(7000000)
        self.root=self.d.go('Lower Palace Armory Trial - gray blockout')
        self.c=kit.Course.__new__(kit.Course);self.c.d=self.d
        self.context=self.d.script(self.root,'ArmoryCourseContext')
        self.d.script(self.root,'ObstacleContext')
        self.arch=self.group('Architecture - neutral gray only')
        self.path=self.group('Walkable shelves and fixed ramps')
        self.markers=self.group('Course anchors - spawn and recovery')
        self.hazards=self.group('Four obstacle groups - edit serialized settings')
    def group(self,name,parent=None,pos=(0,0,0),rot=(0,0,0,1)):
        return self.d.go(name,pos,rot,parent=parent or self.root)
    def box(self,parent,name,pos,size,mat='ArmoryTrialGray',solid=True,rot=(0,0,0,1),shape='Cube',unclimb=False):
        obj=self.c.cube(parent,name,pos,size,mat,solid,rot,shape)
        if solid and unclimb:self.d.script(obj,'NoClimbSurface')
        return obj
    def renderer(self,obj):return obj['comps'][2]
    def deck(self,name,x0,x1,z0,z1,h):
        return self.box(self.path,name,((x0+x1)/2,h-.4,(z0+z1)/2),(x1-x0,.8,z1-z0))
    def ramp(self,name,x0,x1,z,h0,h1):
        angle=math.atan2(h1-h0,x1-x0);thickness=.5
        return self.box(self.path,name,((x0+x1)/2+thickness*.5*math.sin(angle),(h0+h1)/2-thickness*.5*math.cos(angle),z),
            (math.hypot(x1-x0,h1-h0),thickness,12),rot=(0,0,math.sin(angle/2),math.cos(angle/2)))
    def floors(self):
        # Each corner owns its 12m square. Straight strips stop at the square's boundary.
        corners=[(58,12,0),(58,60,0),(102,60,3),(102,96,3),(58,96,6),(58,136,6),(102,136,6),(102,160,6),(80,160,6)]
        for x,z,h in corners:self.deck('Corner '+str(x)+' '+str(z),x-6,x+6,z-6,z+6,h)
        self.deck('Starting apron',71,89,6,18,0)
        self.deck('Entry to first shelf',64,71,6,18,0)
        for x,a,b,h in ((58,18,54,0),(102,66,90,3),(58,102,130,6),(102,142,154,6),(80,166,176,6)):
            self.deck('Shelf '+str(x)+' '+str(a),x-6,x+6,a,b,h)
        self.deck('Lower ramp approach',64,70,54,66,0)
        self.ramp('Fixed ramp 0m to 3m',70,88,60,0,3)
        self.deck('Lower ramp exit',88,96,54,66,3)
        self.deck('Upper ramp approach',90,96,90,102,3)
        self.ramp('Fixed ramp 3m to 6m',72,90,96,6,3)
        self.deck('Upper ramp exit',64,72,90,102,6)
        self.deck('Final shelf connector',64,96,130,142,6)
        self.deck('Final safe connector',86,96,154,166,6)
        for i,z in enumerate((176.25,176.75,177.25,177.75)):
            self.deck('Exit shallow step '+str(i+1),74,86,z-.25,z+.25,6-(i+1)*.25)
        self.deck('Next stage entry strip',74,86,178,182,5)
        heights=(0,0,3,6,6,6);self.checkpoints=[]
        for entry,h in zip(PLAN['checkpoints'],heights):
            x,z=entry['xz'];point=self.group('Checkpoint '+entry['id'],self.markers,(x,h,z),kit.yaw(-90) if entry['id']=='S' else (0,0,0,1))
            self.checkpoints.append(point)
            # Extend the shelf sideways rather than stacking a second coplanar floor.
            if entry['id'] not in ('S','E'):
                self.deck('Checkpoint '+entry['id']+' west shoulder',x-9,x-6,z-3,z+3,h)
                self.deck('Checkpoint '+entry['id']+' east shoulder',x+6,x+9,z-3,z+3,h)
            self.box(point,'Checkpoint floor line',(0,.015,0),(12,.03,.25),'ArmoryTrialLightGray',False)
        # Isolated dead-end waiting pockets never join into a parallel bypass.
        for i,(x0,x1,z0,z1,h) in enumerate(((46,52,20,24,0),(46,52,48,52,0),(108,114,79,83,3),(108,114,93,97,3),
            (47,52,112,117,6),(64,69,120,125,6),(92,96,138,142,6),(108,112,144,148,6))):
            self.deck('Isolated safe recess '+str(i+1),x0,x1,z0,z1,h)
        self.routes=[]
        for i,(x,z) in enumerate(PLAN['route_xz_m']):
            h=0 if i<=2 else 3 if i<=4 else 6
            if z==178:h=5
            self.routes.append(self.group('Route waypoint '+str(i),self.markers,(x,h,z)))
        self.entry=self.checkpoints[0];self.exit=self.group('Course exit',self.markers,(80,5,181))
    def architecture(self):
        self.box(self.arch,'Distant storage floor',(80,-12,90),(160,1,180),'ArmoryTrialDarkGray',False)
        for x in (0,160):self.box(self.arch,'Side room wall '+str(x),(x,6,90),(1,36,180),unclimb=True)
        self.box(self.arch,'South wall',(80,6,0),(160,36,1),unclimb=True)
        for x in (37,123):self.box(self.arch,'North doorway return '+str(x),(x,6,180),(74,36,1),unclimb=True)
        self.box(self.arch,'North arch header',(80,18,180),(12,14,1),unclimb=True)
        for side in (1,159):
            for z in (20,46,74,104,134,160):
                shelf=self.group('Simple weapon rack '+str(side)+' '+str(z),self.arch,(side,-8,z),kit.yaw(90 if side==1 else -90))
                self.box(shelf,'Rack back',(0,8,0),(15,16,.8),'ArmoryTrialDarkGray',True,unclimb=True)
                for y in (1,7,13):self.box(shelf,'Rack shelf',(0,y,-1),(15,.7,2.5),solid=True,unclimb=True)
                for i in range(6):self.box(shelf,'Neutral weapon silhouette '+str(i),(-6+i*2.4,5,-1.7),(.25,6,.25),'ArmoryTrialLightGray',False)
        for x,z,h in ((58,12,0),(58,60,0),(102,60,3),(102,96,3),(58,96,6),(58,136,6),(102,136,6),(102,160,6),(80,160,6)):
            self.box(self.arch,'Shelf support '+str(x)+' '+str(z),(x,(h-12)/2,z),(3,h+12,3),'ArmoryTrialDarkGray',False)
    def spear_floor(self):
        group=self.group('01 Random spear floor',self.hazards,(58,0,24));zone=self.d.box(group,(12,3,24),(0,1.3,12),True)
        spears=[];tiles=[]
        for row in range(6):
            for col in range(4):
                x=-4.5+col*3;z=2+row*4
                tile=self.box(group,'Spear warning tile '+str(row)+' '+str(col),(x,.022,z),(2.85,.044,3.85),'ArmoryTrialDarkGray',False)
                tiles.append(self.renderer(tile))
                spear=self.group('Rising spear '+str(row)+' '+str(col),group,(x,-1.4,z));spears.append(spear['tr'])
                self.box(spear,'Spear shaft',(0,.7,0),(.13,1.4,.13),'ArmoryTrialLightGray',False)
                self.box(spear,'Spear point',(0,1.17,0),(.22,.3,.22),'ArmoryTrialLightGray',False,rot=(0,0,.38268343,.92387953))
        self.d.script(group,'ArmorySpearFloor',kit.fields(context=kit.ref(self.context),zone=kit.ref(zone),warningSeconds=2,raisedSeconds=1.1,restSeconds=1.5,spearHeight=1.4,knockbackSpeed=5,upwardSpeed=3,stunSeconds=.6)+refs('spears',spears)+refs('warningTiles',tiles))
    def armour(self,parent,name,pos):
        g=self.group(name,parent,pos)
        self.box(g,'Armour stand',(0,.2,0),(3,.4,3),'ArmoryTrialDarkGray',True,unclimb=True)
        self.box(g,'Headless breastplate',(0,2.6,0),(3,3,1.3),'ArmoryTrialLightGray',True,unclimb=True)
        self.box(g,'Hollow neck ring',(0,4.3,0),(1,.2,1),'ArmoryTrialDarkGray',False,shape='Cylinder')
        for side in (-1,1):
            self.box(g,'Shoulder '+str(side),(side*2,3.5,0),(1.4,1.2,1.7),solid=True,unclimb=True)
            self.box(g,'Leg '+str(side),(side*.8,1,0),(.7,1.8,.8),solid=True,unclimb=True)
        return g
    def dullahan_armour(self,parent,pos,yaw):
        g=self.group('Dullahan armour - no head',parent,pos,kit.yaw(yaw))
        self.box(g,'Dullahan display plinth',(0,-.25,0),(5.8,.5,5.8),'ArmoryTrialDarkGray',True,unclimb=True)
        self.box(g,'Dullahan waist',(0,4.8,0),(3.2,1.1,1.8),'ArmoryTrialDarkGray',True,unclimb=True)
        self.box(g,'Dullahan breastplate',(0,6.7,0),(3.7,3,2),'ArmoryTrialLightGray',True,unclimb=True)
        self.box(g,'Dullahan chest crest',(0,6.9,1.1),(1.2,1.7,.3),'ArmoryTrialGray',False)
        self.box(g,'Dullahan back mantle',(0,6.1,-1.2),(4.2,4,.3),'ArmoryTrialDarkGray',False)
        # Four rim segments leave a real hollow neck opening instead of a helmet.
        for x,z,w,d in ((-.72,0,.28,1.6),(.72,0,.28,1.6),(0,-.72,1.2,.28),(0,.72,1.2,.28)):
            self.box(g,'Dullahan open neck rim '+str(x)+' '+str(z),(x,8.45,z),(w,.45,d),'ArmoryTrialLightGray',False)
        for side in (-1,1):
            self.box(g,'Dullahan pauldron '+str(side),(side*2.35,7.6,0),(1.8,1.5,2.4),'ArmoryTrialGray',True,unclimb=True)
            self.box(g,'Dullahan upper arm '+str(side),(side*2.3,6.3,.35),(1,1.9,1.1),'ArmoryTrialLightGray',False)
            self.box(g,'Dullahan elbow '+str(side),(side*2.25,5.35,.65),(1.2,.8,1.3),'ArmoryTrialDarkGray',False)
            self.box(g,'Dullahan forearm '+str(side),(side*2.2,4.55,1),(1.1,1.4,1.3),'ArmoryTrialLightGray',False)
            self.box(g,'Dullahan gauntlet '+str(side),(side*2.1,3.75,1.35),(1.3,.8,1.4),'ArmoryTrialGray',False)
            self.box(g,'Dullahan thigh plate '+str(side),(side*.95,3.65,0),(1.35,1.7,1.5),'ArmoryTrialLightGray',True,unclimb=True)
            self.box(g,'Dullahan knee guard '+str(side),(side*1.05,2.65,.45),(1.6,.9,1.8),'ArmoryTrialGray',False)
            self.box(g,'Dullahan shin plate '+str(side),(side*1.05,1.55,.05),(1.15,1.6,1.45),'ArmoryTrialLightGray',True,unclimb=True)
            self.box(g,'Dullahan armoured boot '+str(side),(side*1.05,.4,.55),(1.55,.8,2.6),'ArmoryTrialDarkGray',True,unclimb=True)
        return g
    def dullahans(self):
        for i,(z,x,axe) in enumerate(((74,91,False),(87,113,True))):
            group=self.group(('02b Axe' if axe else '02a Mace')+' Dullahan',self.hazards,(102,3,z))
            self.dullahan_armour(group,(x-102,0,0),-90 if axe else 90)
            pivot=self.group('Weapon pivot - animated',group,(x-102,1.2,0))
            self.box(pivot,'Weapon handle',(0,8,0),(.35,16,.35),'ArmoryTrialDarkGray',False)
            if axe:self.box(pivot,'Wide axe blade',(0,16,0),(3,3,.5),'ArmoryTrialLightGray',False)
            else:
                self.box(pivot,'Flanged mace head',(0,16,0),(2.6,2.6,2.6),'ArmoryTrialGray',False,shape='Sphere')
                self.box(pivot,'Mace flanges X',(0,16,0),(3,3,.35),'ArmoryTrialLightGray',False)
                self.box(pivot,'Mace flanges Z',(0,16,0),(.35,3,3),'ArmoryTrialLightGray',False)
            self.box(pivot,'Weapon guard',(0,2,0),(2.2,.3,.5),'ArmoryTrialLightGray',False)
            area=self.d.box(group,(12,3,12 if axe else 3),(0,1.5,0),True)
            warning_parts=[]
            if axe:
                visual=self.box(group,'Strike warning base',(0,.045,0),(.02,.025,.02),'ArmoryTrialDarkGray',False)
                for segment in range(12):
                    centre_x=-5.5+segment;distance=11-centre_x
                    half_z=min(distance*math.tan(math.radians(40)),math.sqrt(max(0,18*18-distance*distance)),6)
                    part=self.box(group,'Axe sector warning '+str(segment),(centre_x,.045,0),(.99,.025,half_z*2),'ArmoryTrialDarkGray',False)
                    warning_parts.append(self.renderer(part))
            else:
                visual=self.box(group,'Strike warning',(0,.045,0),(12,.025,3),'ArmoryTrialDarkGray',False)
                warning_parts.append(self.renderer(visual))
            self.d.script(group,'ArmoryDullahan',kit.fields(context=kit.ref(self.context),weaponPivot=kit.ref(pivot['tr']),strikeArea=kit.ref(area),warningVisual=kit.ref(self.renderer(visual)),axe=1 if axe else 0,phaseOffset=1.75 if axe else 0,windupSeconds=1.1,strikeSeconds=.25,holdSeconds=.55,recoverySeconds=.8,restSeconds=.8,pushSpeed=6,upwardSpeed=3,stunSeconds=.8,attackReach=18)+refs('warningParts',warning_parts))
    def crossbows(self):
        group=self.group('03 Carry shield crossbow gallery',self.hazards,(58,6,108))
        zone=self.d.box(group,(12,5,20),(0,2.5,10),True)
        allowed=self.d.box(group,(26,18,33),(0,3,7.5),True)
        for i,x in enumerate((-4,0,4)):
            spawn=self.group('Shield supply spawn '+str(i),group,(x,1.1,-4))
            shield=self.group('Carry shield '+str(i),group,(x,1.1,-4))
            rb=self.d.rigidbody(shield,False,2)
            self.box(shield,'Shield plate',(0,0,0),(1.8,2.1,.2),'ArmoryTrialLightGray',True)
            self.box(shield,'Shield grip',(0,0,-.2),(.3,.7,.25),'ArmoryTrialDarkGray',False)
            self.d.script(shield,'ArmoryCarryShield',kit.fields(context=kit.ref(self.context),body=kit.ref(rb),spawnPoint=kit.ref(spawn['tr']),allowedArea=kit.ref(allowed),recoverySeconds=3,abandonedSeconds=12,lostBelowSpawn=8))
        emitters=[];projectiles=[]
        aim_lines=[]
        for i,(x,z,yaw) in enumerate(((-8,4,90),(-8,10,90),(-8,16,90),(8,4,-90),(8,10,-90),(8,16,-90),(-4,21,180),(0,21,180),(4,21,180))):
            emitter=self.group('Crossbow emitter '+str(i),group,(x,1.5,z),kit.yaw(yaw));emitters.append(emitter['tr'])
            self.box(emitter,'Crossbow bow',(0,0,0),(2,.3,.3),'ArmoryTrialDarkGray',False)
            self.box(emitter,'Crossbow stock',(0,0,.5),(.35,.35,1.4),'ArmoryTrialLightGray',False)
            aim_lines.append(self.box(group,'Aim warning line '+str(i),(0,-50,0),(.06,.06,1),'ArmoryTrialLightGray',False)['tr'])
        for i in range(27):
            visual=self.box(group,'Pooled arrow '+str(i),(0,-50,0),(.1,.1,1),'ArmoryTrialDarkGray',False)
            projectiles.append(visual['tr'])
        for x,z in ((-6,8),(6,14)):
            cover=self.box(group,'Fixed turning cover '+str(x),(x,1,z),(1,2,3),'ArmoryTrialDarkGray',True,unclimb=True)
        self.d.script(group,'ArmoryCrossbowGallery',kit.fields(context=kit.ref(self.context),zone=kit.ref(zone),volleySeconds=2.8,telegraphSeconds=1,arrowSpeed=18,hitSpeed=5,stun=.6,hitCooldown=1)+refs('emitters',emitters)+refs('projectileVisuals',projectiles)+refs('aimLines',aim_lines))
    def watchers(self):
        group=self.group('04 Watching armour gallery',self.hazards,(102,6,138))
        zone=self.d.box(group,(12,4,12),(0,2,6),True);watchers=[];eyes=[];fans=[]
        for i,(x,z,yaw) in enumerate(((-10,2,90),(10,11,-90))):
            watcher=self.group('Watcher gaze pivot '+str(i),group,(x,0,z),kit.yaw(yaw));watchers.append(watcher['tr'])
            self.armour(watcher,'Watching armour', (0,0,0))
            self.box(watcher,'Watcher helmet',(0,4.8,0),(1.7,1.7,1.5),'ArmoryTrialLightGray',True,unclimb=True)
            eye=self.box(watcher,'Watcher eye slit',(0,4.8,.8),(1.3,.25,.1),'ArmoryTrialDarkGray',False);eyes.append(self.renderer(eye))
            fan=self.group('Clipped sight cone '+str(i),group);fans.append(fan['tr'])
            # Narrow slices approximate the horizontal cone, clipped to the gallery and its range.
            for segment in range(12):
                centre_x=-5.5+segment;distance=abs(centre_x-x)+.5
                half_z=min(distance*math.tan(math.radians(40)),math.sqrt(max(0,18*18-distance*distance)))
                lo=max(0,z-half_z);hi=min(12,z+half_z)
                if hi>lo:self.box(fan,'Sight cone slice '+str(segment),(centre_x,.035,(lo+hi)/2),(.99,.025,hi-lo),'ArmoryTrialLightGray',False)
        for x,z in ((-5,2),(4,8)):
            self.box(group,'Sight blocking display '+str(x),(x,1.75,z),(2,3.5,2),'ArmoryTrialDarkGray',True,unclimb=True)
        self.d.script(group,'ArmoryWatcherGallery',kit.fields(context=kit.ref(self.context),zone=kit.ref(zone),lookSeconds=2,lookAwaySeconds=3.8,turnSeconds=.8,warningSeconds=1.2,range=18,viewAngle=80,hitSpeed=5,stun=1,hitCooldown=2)+refs('watchers',watchers)+refs('eyes',eyes)+refs('sightFans',fans))
    def save(self):
        self.floors();self.architecture();self.spear_floor();self.dullahans();self.crossbows();self.watchers()
        course=self.d.script(self.root,'CourseModule',kit.fields(courseId='"2-3-TRIAL"',displayName='"아랫궁 무구실 · 회색 플레이 검증"',entry=kit.ref(self.entry['tr']),exit=kit.ref(self.exit['tr']),minigameSlot='{fileID: 0}',targetSeconds=269,laneWidth=12)+refs('checkpoints',[x['tr'] for x in self.checkpoints])+refs('route',[x['tr'] for x in self.routes])+'  placementBounds:\n    m_Center: {x: 80, y: 0, z: 90}\n    m_Extent: {x: 81, y: 30, z: 93}\n')
        self.d.script(self.root,'ArmoryCourseTrial',kit.fields(pawnPrefab='{fileID: 7684884123511159585, guid: fe69acb7e49fea745aa289c4d39f3e62, type: 3}',tuning='{fileID: 11400000, guid: f387368cafccca042ac935642cf094be, type: 2}',course=kit.ref(course),context=kit.ref(self.context)))
        text,ids=stabilize(clean(self.d.render(scene=False)));self.root_id=ids[self.root['tr']]
        return text

def scene(root_id):
    source=(ROOT/'Assets/Scenes/GiantChessCourse.unity').read_text();data=blocks(source)
    keep=set();roots=[]
    for k,b in data.items():
        if b.startswith(('--- !u!29 ','--- !u!104 ','--- !u!157 ','--- !u!196 ')):keep.add(k)
        if b.startswith('--- !u!1 ') and any('  m_Name: '+n+'\n' in b for n in ('Sun','Main Camera')):
            keep.add(k);components=list(map(int,re.findall(r'component: \{fileID: (\d+)',b)));keep.update(components)
            roots.extend(c for c in components if data[c].startswith('--- !u!4 '))
    kept=''.join(b for k,b in data.items() if k in keep)
    # Neutral scene lighting makes every blockout structure read as gray.
    for field in ('m_AmbientSkyColor','m_AmbientEquatorColor','m_AmbientGroundColor'):
        kept=re.sub(r'  '+field+r': .*', '  '+field+': {r: 0.55, g: 0.55, b: 0.55, a: 1}',kept)
    kept=re.sub(r'  m_AmbientMode: .*','  m_AmbientMode: 3',kept)
    kept=re.sub(r'  m_SkyboxMaterial: .*','  m_SkyboxMaterial: {fileID: 0}',kept)
    kept=re.sub(r'  m_BackGroundColor: .*','  m_BackGroundColor: {r: 0.3, g: 0.3, b: 0.3, a: 1}',kept)
    kept=re.sub(r'  m_ClearFlags: .*','  m_ClearFlags: 2',kept)

    prefab_guid=kit.guid(PREFAB)
    instance=f'''--- !u!1001 &960000
PrefabInstance:
  m_ObjectHideFlags: 0
  serializedVersion: 2
  m_Modification:
    serializedVersion: 3
    m_TransformParent: {{fileID: 0}}
    m_Modifications: []
    m_RemovedComponents: []
    m_RemovedGameObjects: []
    m_AddedGameObjects: []
    m_AddedComponents: []
  m_SourcePrefab: {{fileID: 100100000, guid: {prefab_guid}, type: 3}}
--- !u!4 &960001 stripped
Transform:
  m_CorrespondingSourceObject: {{fileID: {root_id}, guid: {prefab_guid}, type: 3}}
  m_PrefabInstance: {{fileID: 960000}}
  m_PrefabAsset: {{fileID: 0}}
--- !u!1660057539 &9223372036854775807
SceneRoots:
  m_ObjectHideFlags: 0
  m_Roots:
  - {{fileID: 960001}}
'''+''.join('  - '+kit.ref(t)+'\n' for t in roots)
    return clean(kit.HEAD+kept+instance)

def main():
    parser=argparse.ArgumentParser(description=__doc__);mode=parser.add_mutually_exclusive_group(required=True)
    for flag in ('write','check','overwrite'):mode.add_argument('--'+flag,action='store_true')
    args=parser.parse_args();builder=Builder();prefab=builder.save()
    outputs={PREFAB:prefab,SCENE:scene(builder.root_id)}
    for name in MATERIALS:
        path=f'{kit.BASE}/Materials/{name}.mat';outputs[path]=kit.outputs[path]
    if args.check:
        stale=[p for p,s in outputs.items() if not (ROOT/p).exists() or (ROOT/p).read_text()!=s]
        if stale:raise SystemExit('Armory output differs: '+', '.join(stale))
        print('Armory prefab, scene and three neutral materials match the generator.');return
    if args.write and any((ROOT/p).exists() for p in (PREFAB,SCENE)):
        raise SystemExit('Armory assets already exist; use --check or --overwrite intentionally.')
    for p,s in outputs.items():
        (ROOT/p).parent.mkdir(parents=True,exist_ok=True);(ROOT/p).write_text(s)
    subprocess.run([sys.executable,'Tools/Generators/mkmeta.py',*outputs],cwd=ROOT,check=True)
    print('Saved grayscale armory prefab and single-instance playtest scene.')
if __name__=='__main__':main()
