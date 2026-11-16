"""Saved blockout geometry and bindings; not a Unity/PhysX playthrough."""
from pathlib import Path
import math,re,unittest
from test_obstacle_assets import blocks,fields
from test_royal_palace_courses import number,vector,transform,rotate
ROOT=Path(__file__).resolve().parents[2]
PREFAB=ROOT/'Assets/Prefabs/Obstacles/LowerPalaceArmoryTrial.prefab'
class ArmoryTrialAssets(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.text=PREFAB.read_text();cls.data=blocks(cls.text)
        cls.names={int(k):fields(b)['m_Name'] for k,b in cls.data.items() if b.startswith('--- !u!1 ')}
        cls.transforms={number(fields(b)['m_GameObject']):int(k) for k,b in cls.data.items() if b.startswith('--- !u!4 ')}
        cls.go_by_name={v:k for k,v in cls.names.items()}
        cls.script_guids={p.stem:re.search(r'guid: (\w+)',Path(str(p)+'.meta').read_text())[1] for p in (ROOT/'Assets/Scripts').rglob('*.cs') if Path(str(p)+'.meta').exists()}
    def scripts(self,name):return [(k,fields(b),b) for k,b in self.data.items() if 'guid: '+self.script_guids[name]+',' in b]
    def refs(self,b,name):return list(map(int,re.findall(r'fileID: (\d+)',re.search(r'^  '+name+r':\n((?:  - .*\n)+)',b,re.M)[1])))
    def position(self,name):return transform(self.data,self.transforms[self.go_by_name[name]])
    def test_one_prefab_scene_and_all_local_references_resolve(self):
        self.assertEqual(len(self.data),len(re.findall(r'^--- !u!',self.text,re.M)))
        for fid,tail in re.findall(r'\{fileID: (\d+)([^}]*)\}',self.text):
            if 'guid:' not in tail and int(fid):self.assertIn(fid,self.data)
        scene=(ROOT/'Assets/Scenes/LowerPalaceArmoryTrial.unity').read_text()
        guid=re.search(r'guid: (\w+)',Path(str(PREFAB)+'.meta').read_text())[1]
        self.assertEqual(scene.count('PrefabInstance:\n'),1)
        self.assertIn('guid: '+guid,scene)
        self.assertIn('Main Camera',scene)
        self.assertNotIn('propertyPath: tileSize',scene)
        self.assertIn('m_AmbientMode: 3',scene)
        self.assertIn('m_SkyboxMaterial: {fileID: 0}',scene)
        self.assertIn('m_BackGroundColor: {r: 0.3, g: 0.3, b: 0.3, a: 1}',scene)
        for field in ('m_AmbientSkyColor','m_AmbientEquatorColor','m_AmbientGroundColor'):
            self.assertIn(field+': {r: 0.55, g: 0.55, b: 0.55, a: 1}',scene)
        self.assertEqual(len(self.scripts('ObstacleContext')),1)
    def test_all_four_hazard_bindings_and_capped_shield_supply(self):
        _,spear,raw=self.scripts('ArmorySpearFloor')[0]
        self.assertEqual(len(self.refs(raw,'spears')),24)
        self.assertEqual(len(self.refs(raw,'warningTiles')),24)
        self.assertEqual(len(self.scripts('ArmoryDullahan')),2)
        for _,f,b in self.scripts('ArmoryDullahan'):
            area=fields(self.data[str(number(f['strikeArea']))])
            self.assertEqual(vector(area['m_Size'])[2],12 if f['axe']=='1' else 3)
            self.assertEqual(float(f['attackReach']),18)
            self.assertEqual(len(self.refs(b,'warningParts')),12 if f['axe']=='1' else 1)
        self.assertEqual(len(self.scripts('ArmoryCarryShield')),3)
        self.assertEqual(len(self.scripts('ArmoryCrossbowGallery')),1)
        self.assertEqual(len(self.scripts('ArmoryWatcherGallery')),1)
        _,_,raw=self.scripts('ArmoryCrossbowGallery')[0]
        self.assertEqual(len(self.refs(raw,'emitters')),6)
        self.assertEqual(len(self.refs(raw,'projectileVisuals')),18)
        for k,f,b in self.scripts('ArmoryCarryShield'):
            self.assertNotEqual(number(f['body']),0);self.assertNotEqual(number(f['spawnPoint']),0)
            self.assertNotEqual(number(f['context']),0);self.assertNotEqual(number(f['allowedArea']),0)
        for cls in ('ArmorySpearFloor','ArmoryDullahan','ArmoryCrossbowGallery','ArmoryWatcherGallery'):
            for _,f,_ in self.scripts(cls):self.assertNotEqual(number(f['context']),0)
    def test_course_checkpoints_heights_and_route_length(self):
        _,f,b=self.scripts('CourseModule')[0]
        checkpoints=[transform(self.data,t) for t in self.refs(b,'checkpoints')]
        self.assertEqual(checkpoints,[(80,0,12),(58,0,20),(102,3,64),(58,6,102),(102,6,133),(80,6,170)])
        route=[transform(self.data,t) for t in self.refs(b,'route')]
        length=sum(math.hypot(a[0]-b[0],a[2]-b[2]) for a,b in zip(route,route[1:]))
        self.assertEqual(length,342)
        self.assertTrue(all(a[2]<=b[2] for a,b in zip(route,route[1:])))
        self.assertEqual(float(f['laneWidth']),12)
        self.assertEqual(transform(self.data,number(f['exit'])),(80,5,181))
    def test_only_three_neutral_structural_materials(self):
        used=set(re.findall(r'm_Materials:\n  - \{fileID: 2100000, guid: (\w+)',self.text))
        expected=set()
        for name in ('ArmoryTrialGray','ArmoryTrialDarkGray','ArmoryTrialLightGray'):
            p=ROOT/('Assets/Resources/RoyalPalace/Materials/'+name+'.mat')
            expected.add(re.search(r'guid: (\w+)',Path(str(p)+'.meta').read_text())[1])
            rgb=map(float,re.search(r'_Color: \{r: ([^,]+), g: ([^,]+), b: ([^,]+)',p.read_text()).groups())
            self.assertEqual(len(set(rgb)),1)
        self.assertEqual(used,expected)
    def test_top_faces_cover_full_width_through_both_ramps(self):
        # Every point across a 11.8m core of the 12m route has an upper collision face near the intended plane.
        surfaces=[]
        path_tid=self.transforms[self.go_by_name['Walkable shelves and fixed ramps']]
        for k,b in self.data.items():
            if not b.startswith('--- !u!65 '):continue
            f=fields(b)
            if f['m_IsTrigger']=='1':continue
            go=number(f['m_GameObject']);tid=self.transforms[go]
            if number(fields(self.data[str(tid)])['m_Father'])!=path_tid:continue
            top=transform(self.data,tid,(0,.5,0))
            q=vector(fields(self.data[str(tid)])['m_LocalRotation']);n=rotate(q,(0,1,0))
            scale=vector(fields(self.data[str(tid)])['m_LocalScale'])
            pos=transform(self.data,tid)
            surfaces.append((top,n,q,scale,pos))
        def heights(x,z):
            values=[]
            for top,n,q,scale,pos in surfaces:
                if n[1]<.5:continue
                y=top[1]-(n[0]*(x-top[0])+n[2]*(z-top[2]))/n[1]
                delta=(x-pos[0],y-pos[1],z-pos[2]);local=rotate((-q[0],-q[1],-q[2],q[3]),delta)
                if abs(local[0])<=scale[0]*.5+.002 and abs(local[2])<=scale[2]*.5+.002:values.append(y)
            return values
        for x0,x1,z,y0,y1 in ((64,96,60,0,3),(64,96,96,6,3)):
            for i in range(65):
                x=x0+(x1-x0)*i/64
                y=(0 if x<=70 else 3 if x>=88 else (x-70)/6) if z==60 else (6 if x<=72 else 3 if x>=90 else 6-(x-72)/6)
                for offset in (-5.9,0,5.9):
                    values=heights(x,z+offset)
                    self.assertTrue(any(abs(h-y)<.01 for h in values),(x,z+offset,y,values))
        for i,h in enumerate((5.75,5.5,5.25,5)):
            self.assertAlmostEqual(max(heights(80,176.25+i*.5)),h)
