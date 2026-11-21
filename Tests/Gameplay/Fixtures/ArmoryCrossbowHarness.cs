using System;using System.Collections.Generic;using System.Reflection;
using UnityEngine;using ChessFight.ProtectKing;
namespace UnityEngine {
 public class SerializeField:Attribute{}public class TooltipAttribute:Attribute{public TooltipAttribute(string s){}}public class MinAttribute:Attribute{public MinAttribute(float f){}}public class RequireComponent:Attribute{public RequireComponent(Type t){}}
 public class MonoBehaviour {public bool enabled=true;public Transform transform=new Transform();public Collider[] testColliders=new Collider[0];public T GetComponent<T>() where T:class=>null;public T[] GetComponentsInChildren<T>() where T:class{if(typeof(T)==typeof(Collider))return testColliders as T[];return new T[0];}}
 public class GameObject {public bool active;public bool activeInHierarchy=true;public void SetActive(bool v){active=v;}}
 public class Transform {public Vector3 localScale;public Vector3 position;public Quaternion rotation;public Transform parent;public GameObject gameObject=new GameObject();public Vector3 forward=>rotation.Rotate(Vector3.forward);public Vector3 TransformPoint(Vector3 p)=>position+rotation.Rotate(p);public Vector3 InverseTransformPoint(Vector3 p)=>Quaternion.Euler(0,-rotation.yaw,0).Rotate(p-position);public bool IsChildOf(Transform p)=>this==p||(parent!=null&&parent.IsChildOf(p));public T[] GetComponentsInChildren<T>()=>new T[0];}
 public struct Vector3 {public float x,y,z;public Vector3(float a,float b,float c){x=a;y=b;z=c;}public static Vector3 zero=>new Vector3();public static Vector3 forward=>new Vector3(0,0,1);public static Vector3 up=>new Vector3(0,1,0);public float magnitude=>(float)Math.Sqrt(sqrMagnitude);public float sqrMagnitude=>x*x+y*y+z*z;public Vector3 normalized=>magnitude>.00001f?this*(1/magnitude):zero;public static Vector3 operator+(Vector3 a,Vector3 b)=>new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);public static Vector3 operator-(Vector3 a,Vector3 b)=>new Vector3(a.x-b.x,a.y-b.y,a.z-b.z);public static Vector3 operator*(Vector3 a,float f)=>new Vector3(a.x*f,a.y*f,a.z*f);public static Vector3 Scale(Vector3 a,Vector3 b)=>new Vector3(a.x*b.x,a.y*b.y,a.z*b.z);public static float Distance(Vector3 a,Vector3 b)=>(a-b).magnitude;public static Vector3 Lerp(Vector3 a,Vector3 b,float t)=>a+(b-a)*Math.Max(0,Math.Min(1,t));}
 public struct Quaternion {public static Quaternion identity=>default(Quaternion);public float yaw;public static Quaternion Euler(float x,float y,float z)=>new Quaternion{yaw=y};public static Quaternion LookRotation(Vector3 d)=>new Quaternion{yaw=(float)(Math.Atan2(d.x,d.z)*180/Math.PI)};public Vector3 Rotate(Vector3 p){double a=yaw*Math.PI/180;return new Vector3((float)(p.x*Math.Cos(a)+p.z*Math.Sin(a)),p.y,(float)(-p.x*Math.Sin(a)+p.z*Math.Cos(a)));}}
 public struct Color {public static Color white=>default(Color);public static Color gray=>default(Color);public static Color operator*(Color c,float f)=>c;}
 public class MaterialPropertyBlock {public void Clear(){}}
 public class Renderer {public void SetPropertyBlock(MaterialPropertyBlock m){}}
 public class Collider {public bool enabled=true;public Transform transform=new Transform();public Rigidbody attachedRigidbody;public ImportedObstacleActor actor;}
 public class BoxCollider:Collider {public Bounds bounds=>new Bounds{center=transform.TransformPoint(center),extents=size*.5f};public Vector3 size,center;}
 public class Joint {public Rigidbody connectedBody;}
 public enum RigidbodyInterpolation{Interpolate}public enum CollisionDetectionMode{ContinuousDynamic,ContinuousSpeculative} [Flags]public enum RigidbodyConstraints{FreezeRotationX=1,FreezeRotationZ=2}
 public class Rigidbody {public Vector3 position,linearVelocity,angularVelocity;public Quaternion rotation;public bool isKinematic,useGravity;public float mass;public RigidbodyInterpolation interpolation;public CollisionDetectionMode collisionDetectionMode;public RigidbodyConstraints constraints;public List<Joint> joints=new List<Joint>();public void MovePosition(Vector3 p){position=p;}public void MoveRotation(Quaternion q){rotation=q;}public void GetComponents(List<Joint> target){target.Clear();target.AddRange(joints);}}
 public struct Bounds {public Vector3 center,extents;}
 public struct RaycastHit {public Collider collider;public float distance;}
 public enum QueryTriggerInteraction {Ignore}
 public static class Physics {public static int ignored;public static void IgnoreCollision(Collider a,Collider b,bool ignore){ignored+=ignore?1:-1;}public static Collider[] overlaps=new Collider[0];public static Func<Vector3,Vector3,float,RaycastHit[]> ray;public static int RaycastNonAlloc(Vector3 p,Vector3 d,RaycastHit[] target,float length,int mask,QueryTriggerInteraction q){var found=ray==null?new RaycastHit[0]:ray(p,d,length);int n=Math.Min(found.Length,target.Length);Array.Copy(found,target,n);return n;}public static int OverlapBoxNonAlloc(Vector3 p,Vector3 h,Collider[] t,Quaternion r,int m,QueryTriggerInteraction q){int n=Math.Min(overlaps.Length,t.Length);Array.Copy(overlaps,t,n);return n;}public static int OverlapSphereNonAlloc(Vector3 p,float r,Collider[] target,int mask,QueryTriggerInteraction q){int n=Math.Min(overlaps.Length,target.Length);Array.Copy(overlaps,target,n);return n;}}
 public static class Mathf{public static float Max(float a,float b)=>Math.Max(a,b);public static float Min(float a,float b)=>Math.Min(a,b);public static float Abs(float n)=>Math.Abs(n);public static float Sin(float n)=>(float)Math.Sin(n);}
 public static class Time {public static float fixedDeltaTime=.02f;}
 public static class Debug {public static void LogError(string s,object o){throw new Exception(s);}}
}
namespace ChessFight.Gameplay {public class Obstacle:MonoBehaviour{protected double SharedTime=>0;}public static class ObstacleClock{public static bool Shared;}}
namespace ChessFight.Game {public static class LitMaterials{public static void SetColor(MaterialPropertyBlock b,Color c){}}}
namespace ChessFight.ProtectKing {public class ArmoryCourseContext{public bool Running=true;public double Now;public int Seed=1;}public class ImportedObstacleActor{public bool CanReceiveObstacle=true;public Vector3 Position;public int hits;public static ImportedObstacleActor Find(Collider c)=>c.actor;public void AddObstacleHit(Vector3 p,float s){hits++;}}}
class Harness {
 static void Check(bool b,string message){if(!b)throw new Exception(message);}static void Set(object o,string field,object v)=>o.GetType().GetField(field,BindingFlags.NonPublic|BindingFlags.Instance).SetValue(o,v);static void Call(object o,string method)=>o.GetType().GetMethod(method,BindingFlags.NonPublic|BindingFlags.Instance).Invoke(o,null);
 static void Near(float a,float b,string s)=>Check(Math.Abs(a-b)<.005f,s);
 static void Tick(ArmoryCrossbowGallery gallery,ArmoryCourseContext context,double now){context.Now=now;Call(gallery,"FixedUpdate");}
 static void Main(){
  var zone=new BoxCollider{center=new Vector3(58,7.5f,118),size=new Vector3(12,4,20)};Vector3 a,b;
  Check(ArmoryCrossbowGallery.ClipSegment(zone,new Vector3(58,7,129),new Vector3(58,7,90),out a,out b),"Shooting ray intersects course");Near(a.z,128,"Entry clipped");Near(b.z,108,"Exit clipped before CP");
  Check(!ArmoryCrossbowGallery.ClipSegment(zone,new Vector3(45,7,129),new Vector3(45,7,90),out a,out b),"Out-of-lane shot rejected");
  var rotated=new BoxCollider{size=new Vector3(2,2,6)};rotated.transform.rotation=Quaternion.Euler(0,90,0);
  Check(ArmoryCrossbowGallery.ClipSegment(rotated,new Vector3(4,0,0),new Vector3(-4,0,0),out a,out b),"Rotated damage zone");Near(a.x,3,"Rotated entry");Near(b.x,-3,"Rotated exit");
  var context=new ArmoryCourseContext{Now=0};var gallery=new ArmoryCrossbowGallery();var emitter=new Transform{position=new Vector3(58,7,129),rotation=Quaternion.Euler(0,180,0)};
  Set(gallery,"context",context);Set(gallery,"zone",zone);Set(gallery,"emitters",new[]{emitter});Set(gallery,"projectileVisuals",new[]{new Transform(),new Transform(),new Transform()});Call(gallery,"Awake");
  var actor=new ImportedObstacleActor{Position=new Vector3(58,6,117)};var pawnCollider=new Collider{actor=actor};var shieldCollider=new Collider();
  Physics.ray=(p,d,length)=>{Check(p.z>=108&&p.z<=128.01f,"Ray stays in firing lane");return new[]{new RaycastHit{collider=pawnCollider,distance=.2f},new RaycastHit{collider=shieldCollider,distance=.1f}};};
  Tick(gallery,context,1.1);Check(gallery.BlockedShots==1&&actor.hits==0,"Nearest shield blocks unsorted hits before actor");
  Physics.ray=(p,d,length)=>new[]{new RaycastHit{collider=pawnCollider,distance=.2f}};Tick(gallery,context,3.9);Check(actor.hits==1,"Unprotected pawn hit");
  actor.Position=new Vector3(58,6,104);Tick(gallery,context,6.7);Check(actor.hits==1,"Reaching hand cannot hit pawn at supply CP");
  actor.Position=new Vector3(58,6,117);
  Physics.ray=(p,d,length)=>{var many=new RaycastHit[128];for(int i=0;i<many.Length;i++)many[i]=new RaycastHit{collider=pawnCollider,distance=.1f};return many;};Tick(gallery,context,9.5);Check(actor.hits==1,"Saturated ray query cannot hit through omitted cover");
  context.Running=false;Tick(gallery,context,12.3);Check(actor.hits==1,"Pause does not fire");context.Running=true;
  context.Seed=2;Tick(gallery,context,0);Check(gallery.PawnHits==0&&gallery.BlockedShots==0,"New seed resets shots and cooldowns");
  var shield=new ArmoryCarryShield();var body=new Rigidbody{position=new Vector3(58,7,104)};var solid=new Collider();shield.testColliders=new[]{solid};Set(shield,"body",body);Set(shield,"context",context);Set(shield,"spawnPoint",new Transform{position=body.position});Set(shield,"allowedArea",new BoxCollider{center=new Vector3(58,7,116),size=new Vector3(24,10,36)});Call(shield,"Awake");
  var hand=new Rigidbody();hand.joints.Add(new Joint{connectedBody=body});Physics.overlaps=new[]{new Collider{attachedRigidbody=hand}};
  body.position=new Vector3(100,7,104);context.Now=.1;Call(shield,"FixedUpdate");context.Now=3.2;Call(shield,"FixedUpdate");Check(shield.Returning&&!solid.enabled,"Out-of-course held shield signals existing hand release");
  context.Now=3.5;Call(shield,"FixedUpdate");Near(body.position.x,100,"Never teleport held shield");
  hand.joints.Clear();context.Now=3.6;Call(shield,"FixedUpdate");Near(body.position.x,58,"Joint-free shield returns to capped supply");Check(solid.enabled&&!shield.Returning,"Shield collision restored after recovery");
  hand.joints.Add(new Joint{connectedBody=body});context.Now=4;Call(shield,"FixedUpdate");context.Now=40;Call(shield,"FixedUpdate");context.Now=44;Call(shield,"FixedUpdate");Check(shield.Returning,"Holding inside supply cannot deny it forever");
  Physics.ray=null;Physics.overlaps=new[]{pawnCollider};actor.Position=new Vector3(58,6,118);
  var aimContext=new ArmoryCourseContext();var aimed=new ArmoryCrossbowGallery();var sides=new Transform[9];var lines=new Transform[9];var arrows=new Transform[27];
  for(int i=0;i<9;i++){sides[i]=new Transform{position=i<6?new Vector3(i<3?50:66,7.5f,112+i%3*6):new Vector3(54+(i-6)*4,7.5f,129),rotation=Quaternion.Euler(0,i<3?90:i<6?-90:180,0)};lines[i]=new Transform();}
  for(int i=0;i<27;i++)arrows[i]=new Transform();
  Set(aimed,"context",aimContext);Set(aimed,"zone",zone);Set(aimed,"emitters",sides);Set(aimed,"aimLines",lines);Set(aimed,"projectileVisuals",arrows);Call(aimed,"Awake");Tick(aimed,aimContext,0);
  var firing=typeof(ArmoryCrossbowGallery).GetMethod("FiresInVolley",BindingFlags.NonPublic|BindingFlags.Instance);
  Func<int,long,bool> fires=(e,v)=>(bool)firing.Invoke(aimed,new object[]{e,v});
  var counts=new int[3];int previous=-1,repeats=0,frontVolley=-1,changedSeed=0;
  for(int v=0;v<1000;v++)
  {
   Tick(aimed,aimContext,v*2.8+.1);int group=-1,active=0;
   for(int e=0;e<9;e++)if(lines[e].gameObject.active){active++;if(group<0)group=e/3;Check(e/3==group,"One battery per warning");}
   Check(active==3,"Exactly three warning lines");counts[group]++;if(previous==group)repeats++;previous=group;
   if(group==2&&frontVolley<0)frontVolley=v;
   Tick(aimed,aimContext,v*2.8+.5);
   for(int e=0;e<9;e++)Check(lines[e].gameObject.active==(e/3==group)&&fires(e,v)==(e/3==group),"Selection stays fixed during warning");
   Set(aimed,"seed",2);if(!fires(group*3,v))changedSeed++;Set(aimed,"seed",1);
  }
  Check(counts[0]>250&&counts[1]>250&&counts[2]>250,"All three sides remain eligible across volleys");
  Check(repeats>200,"Consecutive repeats allowed instead of ordered alternation");Check(changedSeed>500,"New run changes random schedule");
  aimContext.Now=frontVolley*2.8;aimed.ResetGallery();Tick(aimed,aimContext,frontVolley*2.8+.1);
  var locked=((Vector3[])aimed.GetType().GetField("aimedDirections",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(aimed))[6];
  actor.Position=new Vector3(60,6,120);Tick(aimed,aimContext,frontVolley*2.8+.5);
  var unchanged=((Vector3[])aimed.GetType().GetField("aimedDirections",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(aimed))[6];Near(locked.x,unchanged.x,"Warning locks aim, no homing");
  aimContext.Running=false;Tick(aimed,aimContext,frontVolley*2.8+.8);Check(aimed.BlockedShots==0,"Paused warning does not fire");aimContext.Running=true;
  Physics.ray=(p,d,length)=>new[]{new RaycastHit{collider=shieldCollider,distance=.1f}};
  Tick(aimed,aimContext,frontVolley*2.8+1.1);Check(aimed.BlockedShots==3,"Random front battery fires the three announced bows and can be blocked");
  Physics.overlaps=new Collider[0];context.Now=45;Call(shield,"FixedUpdate");
  var holder=new Transform{position=new Vector3(58,7,104)};var owner=new Collider();
  Check(shield.TryEquip(holder,new[]{owner},Vector3.forward),"Free shield equips");
  Check(shield.IsHeld&&shield.EquippedBy==holder&&body.isKinematic&&!body.useGravity&&Physics.ignored==1,"Equipped shield stays solid but ignores its owner");
  Check(!shield.TryEquip(new Transform(),new[]{owner},Vector3.forward),"Cannot steal an equipped shield");
  shield.SetFacing(new Vector3(1,3,0));context.Now=45.1;Call(shield,"FixedUpdate");Near(body.position.x,58.9f,"Mounted shield follows look direction");Near(body.position.y,7,"Aim pitch does not tilt shield");Near(body.rotation.yaw,90,"Shield rotates toward view");
  shield.Drop();Check(!shield.IsHeld&&!body.isKinematic&&body.useGravity&&Physics.ignored==0,"E drop restores dynamics and owner collisions");
  Check(shield.TryEquip(holder,new[]{owner},Vector3.forward),"Can equip again");shield.ResetShield();Check(shield.EquippedBy==null&&Physics.ignored==0,"Reset releases equipment without orphaned collision ignores");
  context.Now=46;Call(shield,"FixedUpdate");Check(!shield.Returning&&solid.enabled,"Equipment reset finishes without waiting forever");
  Check(shield.TryEquip(holder,new[]{owner},Vector3.forward),"Equip before carrier deactivation");holder.gameObject.activeInHierarchy=false;context.Now=46.1;Call(shield,"FixedUpdate");Check(shield.EquippedBy==null&&Physics.ignored==0,"Inactive carrier releases shield");
  Console.WriteLine("PASS: bounded and rotated projectile clip, nearest cover, pawn/CP/pause/reset rules, and held-shield-safe recovery");
 }
}
