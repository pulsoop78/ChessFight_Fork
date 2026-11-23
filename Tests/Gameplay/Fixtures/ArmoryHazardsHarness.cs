// Execute the production rules/controllers with deterministic engine stand-ins, not PhysX.
using System;
using System.Collections.Generic;
using System.Reflection;
using ChessFight.Network;
using ChessFight.ProtectKing;
using UnityEngine;
namespace UnityEngine
{
 public class SerializeField:Attribute {} public class MinAttribute:Attribute {public MinAttribute(float n){}} public class RangeAttribute:Attribute {public RangeAttribute(float a,float b){}}
 public class GameObject {public bool activeInHierarchy=true;}
 public class MonoBehaviour {public bool enabled=true; public Transform transform=new Transform();public T GetComponentInParent<T>()where T:class=>null;}
 public class Transform {public Vector3 position,localPosition,lossyScale=Vector3.one;public Quaternion rotation,localRotation;public Vector3 TransformPoint(Vector3 p)=>position+p;public Vector3 InverseTransformPoint(Vector3 p)=>p-position;public Vector3 TransformDirection(Vector3 p)=>p;}
 public struct Vector3
 {
  public float x,y,z;public Vector3(float a,float b,float c){x=a;y=b;z=c;}
  public static Vector3 one=>new Vector3(1,1,1);public static Vector3 up=>new Vector3(0,1,0);public static Vector3 back=>new Vector3(0,0,-1);
  public static Vector3 operator+(Vector3 a,Vector3 b)=>new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);
  public static Vector3 operator-(Vector3 a,Vector3 b)=>new Vector3(a.x-b.x,a.y-b.y,a.z-b.z);
  public static Vector3 operator*(Vector3 a,float f)=>new Vector3(a.x*f,a.y*f,a.z*f);
  public static Vector3 Scale(Vector3 a,Vector3 b)=>new Vector3(a.x*b.x,a.y*b.y,a.z*b.z);
  public float sqrMagnitude=>x*x+y*y+z*z;public Vector3 normalized {get {float n=(float)Math.Sqrt(sqrMagnitude);return n>0?this*(1/n):new Vector3();}}
  public static float Dot(Vector3 a,Vector3 b)=>a.x*b.x+a.y*b.y+a.z*b.z;
 }
 public struct Quaternion {public float z;public static Quaternion Euler(float a,float b,float c)=>new Quaternion{z=c};public static Quaternion operator*(Quaternion a,Quaternion b)=>new Quaternion{z=a.z+b.z};}
 public struct Color {public float r,g,b,a;public Color(float x,float y,float z,float w){r=x;g=y;b=z;a=w;}}
 public class MaterialPropertyBlock {public Color color;public void Clear(){color=default(Color);}}
 public class Renderer {public Color color;public void SetPropertyBlock(MaterialPropertyBlock p){color=p.color;}}
 public class Collider {public ImportedObstacleActor actor;}
 public class BoxCollider:Collider {public Transform transform=new Transform();public Vector3 size,center;}
 public enum QueryTriggerInteraction {Ignore}
 public static class Physics {public static Collider[] found=new Collider[0];public static int OverlapBoxNonAlloc(Vector3 c,Vector3 s,Collider[] target,Quaternion r,int mask,QueryTriggerInteraction q){Array.Copy(found,target,found.Length);return found.Length;}}
 public static class Mathf
 {
  public const float Deg2Rad=(float)(Math.PI/180);public static float Max(float a,float b)=>Math.Max(a,b);public static int Max(int a,int b)=>Math.Max(a,b);public static float Min(float a,float b)=>Math.Min(a,b);public static float Abs(float f)=>Math.Abs(f);public static float Clamp(float f,float a,float b)=>Math.Max(a,Math.Min(b,f));public static float Clamp01(float f)=>Clamp(f,0,1);public static int FloorToInt(float f)=>(int)Math.Floor(f);public static float Pow(float a,float b)=>(float)Math.Pow(a,b);public static float Cos(float a)=>(float)Math.Cos(a);public static float Lerp(float a,float b,float t)=>a+(b-a)*Clamp01(t);public static float SmoothStep(float a,float b,float t){t=Clamp01(t);return a+(b-a)*t*t*(3-2*t);}public static float PingPong(float t,float length){t%=length*2;return length-Math.Abs(t-length);}
 }
 public static class Debug {public static void LogError(string message,object context){throw new Exception(message);}}
}
namespace ChessFight.Game {public static class LitMaterials {public static void SetColor(MaterialPropertyBlock p,Color c){p.color=c;}}}
namespace ChessFight.Gameplay {public class Obstacle:MonoBehaviour{} public static class ObstacleClock {public static bool Shared;}}
namespace ChessFight.ProtectKing
{
 public class ArmoryCourseContext {public bool Running=true;public double Now;public int Seed=7;}
 public class ImportedObstacleActor {public bool CanReceiveObstacle=true;public Vector3 Position,LastHit;public float LastStun;public int Hits;public static ImportedObstacleActor Find(Collider c)=>c.actor;public void AddObstacleHit(Vector3 v,float stun){Hits++;LastHit=v;LastStun=stun;}}
}
class Harness
{
 static void Check(bool v,string text){if(!v)throw new Exception(text);}
 static void Set(object o,string field,object v)=>o.GetType().GetField(field,BindingFlags.NonPublic|BindingFlags.Instance).SetValue(o,v);
 static void Call(object o,string method)=>o.GetType().GetMethod(method,BindingFlags.NonPublic|BindingFlags.Instance).Invoke(o,null);
 static string Fingerprint(ArmorySpearPattern p){string s="";for(int i=0;i<24;i++)s+=p.Raised(i)?"1":"0";return s;}
 static Vector3 CellPoint(int i,float y=0)=>new Vector3((i%4+.5f)*3-6,y,(i/4+.5f)*4);
 static int RaisedCell(ArmorySpearPattern p){for(int i=0;i<24;i++)if(p.Raised(i))return i;throw new Exception("No raised cell");}
 static void Main()
 {
  var triples=new int[6];
  for(int seed=0;seed<500;seed++)
  {
   var variants=new HashSet<string>();
   for(int wave=0;wave<20;wave++)
   {
    var p=ArmorySpearPattern.Generate(seed,wave);variants.Add(Fingerprint(p));
    Check(Fingerprint(p)==Fingerprint(ArmorySpearPattern.Generate(seed,wave)),"Deterministic seed and wave");
    for(int row=0;row<6;row++)
    {
     Check(wave==0 || row!=0 || Math.Abs(p.SafeStart(0)-ArmorySpearPattern.Generate(seed,wave-1).SafeStart(0))==1,"Entrance refuge moves one column per wave");
     if(row>0)Check(Math.Abs(p.SafeStart(row)-p.SafeStart(row-1))<=1,"Connected safe refuge route");
     int safe=0,threat=0;
     for(int col=0;col<4;col++) {if(p.Protected(row,col)){safe++;Check(!p.Raised(row*4+col),"Protected cells never rise");}if(p.Raised(row*4+col))threat++;}
     Check(safe>=1&&safe<=2&&threat>=2&&threat<=3,"Connected refuge and two or three spears per row");
     if(threat==3)triples[row]++;
     if(row>0)Check(p.Protected(row,p.SafeStart(row-1)),"Turn has an orthogonal bridge from preceding row");
    }
    if(wave>0)Check(Fingerprint(p)!=Fingerprint(ArmorySpearPattern.Generate(seed,wave-1)),"Consecutive waves must differ");
   }
   Check(variants.Count>1,"Random waves actually differ");
  }
  for(int row=0;row<6;row++)
  {float fraction=triples[row]/10000f;Check(Math.Abs(fraction-.8f)<.02f,"Every row, including entrance, samples 80% triples and 20% doubles");Console.WriteLine("Row "+row+": three spears "+triples[row]+" / 10000");}
  var context=new ArmoryCourseContext();var floor=new ArmorySpearFloor();var spears=new Transform[24];var tiles=new Renderer[24];
  for(int i=0;i<24;i++){spears[i]=new Transform{localPosition=new Vector3(0,-1.4f,0)};tiles[i]=new Renderer();}
  Set(floor,"context",context);Set(floor,"spears",spears);Set(floor,"warningTiles",tiles);Set(floor,"zone",new BoxCollider{center=new Vector3(0,1.3f,12),size=new Vector3(12,3,24)});Call(floor,"Awake");
  var pattern=ArmorySpearPattern.Generate(7,0);int raised=RaisedCell(pattern);int safeCell=pattern.SafeStart(0);
  var actor=new ImportedObstacleActor{Position=CellPoint(raised)};Physics.found=new[]{new Collider{actor=actor},new Collider{actor=actor}};
  context.Now=2;Call(floor,"FixedUpdate");Check(actor.Hits==0&&spears[raised].localPosition.y==-1.4f,"Warning before extension");
  Check(tiles[raised].color.r==tiles[raised].color.g&&tiles[raised].color.g==tiles[raised].color.b,"Grayscale telegraph");
  context.Now=3.8;context.Running=false;Call(floor,"FixedUpdate");Check(actor.Hits==0,"Paused trial cannot hit");
  context.Running=true;actor.Position=CellPoint(safeCell);Call(floor,"FixedUpdate");Check(actor.Hits==0,"Refuge contact remains safe");
  actor.Position=CellPoint(raised,1.8f);Call(floor,"FixedUpdate");Check(actor.Hits==0,"High airborne crossing ignored");
  actor.Position=CellPoint(raised);Call(floor,"FixedUpdate");Check(actor.Hits==1&&actor.LastHit.z==-5&&actor.LastStun==.6f,"Raised spear foot contact once");
  Call(floor,"FixedUpdate");Check(actor.Hits==1,"Duplicate ragdoll colliders and sustained contact do not stack");
  actor.Position=new Vector3(-7,0,12);Call(floor,"FixedUpdate");Check(actor.Hits==1,"Outside zone does not hit through hands");
  context.Now=8.4;actor.Position=CellPoint(RaisedCell(ArmorySpearPattern.Generate(7,1)));Call(floor,"FixedUpdate");Check(actor.Hits==2,"New wave can hit again");
  context.Now=0;Call(floor,"FixedUpdate");context.Now=3.8;actor.Position=CellPoint(raised);Call(floor,"FixedUpdate");Check(actor.Hits==3,"Same-seed reset clears cycle cooldown");
  Check(floor.CellAt(new Vector3(6,0,12))==-1&&floor.CellAt(new Vector3(0,0,24))==-1,"Exact far edges are out of grid");
  var dullahan=new ArmoryDullahan();var area=new BoxCollider{center=new Vector3(0,1.5f,0),size=new Vector3(12,3,12)};
  Set(dullahan,"context",context);Set(dullahan,"weaponPivot",new Transform{position=new Vector3(11,0,0)});Set(dullahan,"strikeArea",area);Set(dullahan,"warningVisual",new Renderer());Call(dullahan,"Awake");
  actor.Position=new Vector3(0,0,0);actor.Hits=0;context.Now=3;Call(dullahan,"FixedUpdate");Check(actor.Hits==0,"Dullahan windup cannot damage");
  context.Now=4;Call(dullahan,"FixedUpdate");Check(actor.Hits==1&&actor.LastHit.z==-6,"Dullahan impact");Call(dullahan,"FixedUpdate");Check(actor.Hits==1,"Dullahan one hit per cycle");
  var late=new ImportedObstacleActor{Position=new Vector3(0,0,0)};Physics.found=new[]{new Collider{actor=late}};
  context.Now=4.02;Call(dullahan,"FixedUpdate");Check(late.Hits==0,"Entering just after landing is safe even while weapon stays down");
  context.Now=4.2;Call(dullahan,"FixedUpdate");Check(late.Hits==0,"Landed weapon has no lingering damage volume");
  Physics.found=new[]{new Collider{actor=actor},new Collider{actor=actor}};
  context.Now=4.9;Call(dullahan,"FixedUpdate");Check(actor.Hits==1,"Recovery is safe");
  context.Now=10;Call(dullahan,"FixedUpdate");Check(actor.Hits==2,"Next Dullahan strike can hit");
  Check(dullahan.PointInStrike(new Vector3(5,0,6)),"Sword uses its full rectangular footprint");
  Set(dullahan,"axe",true);Check(dullahan.PointInStrike(new Vector3(0,0,0)),"Axe center in sector");Check(!dullahan.PointInStrike(new Vector3(5,0,6)),"Axe box corner outside sector");
  Check(!dullahan.PointInStrike(new Vector3(0,4,0)),"Above the strike volume is not a hit");
  Check(Math.Abs(dullahan.WeaponAngle(3.8f)-90)<.01f&&Math.Abs(dullahan.WeaponAngle(6))<.01f,"Axe slams then recovers");
  context.Running=false;context.Now=16;Call(dullahan,"FixedUpdate");Check(actor.Hits==2,"Paused Dullahan cannot strike");
  Set(dullahan,"strikeArea",new BoxCollider{center=new Vector3(-5,1.5f,0),size=new Vector3(32,3,20)});
  Check(dullahan.PointInStrike(new Vector3(-7,0,0)),"Axe maximum reach is inclusive");
  Check(!dullahan.PointInStrike(new Vector3(-7.1f,0,0)),"Axe beyond visible reach excluded");
  var parts=new[]{new Renderer(),new Renderer()};Set(dullahan,"warningParts",parts);Set(dullahan,"warningVisual",null);
  context.Running=true;context.Seed++;context.Now=3.8;Call(dullahan,"FixedUpdate");
  Check(parts[0].color.r>=.82f&&parts[0].color.r==parts[1].color.r,"All fan slices flash on landing");
  Check(parts[0].color.r==parts[0].color.g&&parts[0].color.g==parts[0].color.b,"Impact remains grayscale");
  Physics.found=new[]{new Collider{actor=late}};context.Now=3.82;Call(dullahan,"FixedUpdate");Check(late.Hits==0,"Axe also allows crossing immediately after landing");
  context.Now=4;Call(dullahan,"FixedUpdate");Check(Math.Abs(parts[0].color.r-.45f)<.001f,"Footprint dims while weapon is still down");
  Physics.found=new Collider[0];context.Seed++;context.Now=3.8;Call(dullahan,"FixedUpdate");
  Physics.found=new[]{new Collider{actor=late}};context.Now=3.82;Call(dullahan,"FixedUpdate");Check(late.Hits==0,"Empty landing still consumes the impact, no delayed entrant hit");
  Console.WriteLine("PASS: 10000 spear waves, varying connected refuges, telegraphs, foot bounds, reset/cooldowns, Dullahan phase and axe-sector hits");
 }
}
