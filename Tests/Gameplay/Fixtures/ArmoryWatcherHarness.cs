// Production watcher control-flow tests with math/query stand-ins; no Unity physics is simulated.
using System;
using System.Reflection;
using UnityEngine;
using ChessFight.ProtectKing;
namespace UnityEngine
{
 public class SerializeField:Attribute{}public class MinAttribute:Attribute{public MinAttribute(float v){}}public class RangeAttribute:Attribute{public RangeAttribute(float a,float b){}}
 public class MonoBehaviour{public bool enabled=true;public T GetComponentInParent<T>()where T:class=>null;}
 public class GameObject{public Transform transform=new Transform();public bool active=true;public object component;public T GetComponent<T>()where T:class=>component as T;public void SetActive(bool on){active=on;}}
 public class Transform{public Vector3 position,lossyScale=Vector3.one,forward=Vector3.forward;public Transform parent;public Quaternion localRotation,rotation;public GameObject gameObject;public Transform(){gameObject=null;}public Vector3 TransformPoint(Vector3 p)=>position+p;public Vector3 InverseTransformPoint(Vector3 p)=>p-position;public bool IsChildOf(Transform p)=>this==p||(parent!=null&&parent.IsChildOf(p));}
 public struct Quaternion{public static Quaternion Euler(float a,float b,float c)=>default(Quaternion);public static Quaternion operator*(Quaternion a,Quaternion b)=>default(Quaternion);}
 public struct Vector3{
  public float x,y,z;public Vector3(float a,float b,float c){x=a;y=b;z=c;}public static Vector3 one=>new Vector3(1,1,1);public static Vector3 up=>new Vector3(0,1,0);public static Vector3 forward=>new Vector3(0,0,1);
  public static Vector3 operator+(Vector3 a,Vector3 b)=>new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);public static Vector3 operator-(Vector3 a,Vector3 b)=>new Vector3(a.x-b.x,a.y-b.y,a.z-b.z);public static Vector3 operator-(Vector3 a)=>new Vector3(-a.x,-a.y,-a.z);public static Vector3 operator*(Vector3 a,float b)=>new Vector3(a.x*b,a.y*b,a.z*b);public static Vector3 Scale(Vector3 a,Vector3 b)=>new Vector3(a.x*b.x,a.y*b.y,a.z*b.z);
  public float sqrMagnitude=>x*x+y*y+z*z;public float magnitude=>(float)Math.Sqrt(sqrMagnitude);public Vector3 normalized=>magnitude>0?this*(1/magnitude):default(Vector3);public static float Dot(Vector3 a,Vector3 b)=>a.x*b.x+a.y*b.y+a.z*b.z;
  public static Vector3 ProjectOnPlane(Vector3 p,Vector3 normal)=>p-normal*Dot(p,normal);public static float Angle(Vector3 a,Vector3 b){double n=a.magnitude*b.magnitude;if(n<.00001)return 0;double d=Dot(a,b)/n;return(float)(Math.Acos(Math.Max(-1,Math.Min(1,d)))*180/Math.PI);}
 }
 public struct Color{public static Color white=>default(Color);public static Color gray=>default(Color);}
 public struct Bounds{public Vector3 center;}
 public class Renderer{public Bounds bounds;public void SetPropertyBlock(MaterialPropertyBlock p){}}
 public class MaterialPropertyBlock{public void Clear(){}}
 public class Collider{public Transform transform=new Transform();public ImportedObstacleActor actor;}
 public class BoxCollider:Collider{public Vector3 center,size;}
 public struct RaycastHit{public Collider collider;}
 public enum QueryTriggerInteraction{Ignore}
 public static class Physics{
  public static Collider[] found=new Collider[0],rayFound=new Collider[0];public static Vector3 lastRayOrigin;
  public static int OverlapBoxNonAlloc(Vector3 center,Vector3 extents,Collider[] results,Quaternion rot,int mask,QueryTriggerInteraction q){Array.Copy(found,results,found.Length);return found.Length;}
  public static int RaycastNonAlloc(Vector3 origin,Vector3 dir,RaycastHit[] results,float length,int mask,QueryTriggerInteraction q){lastRayOrigin=origin;for(int i=0;i<rayFound.Length;i++)results[i]=new RaycastHit{collider=rayFound[i]};return rayFound.Length;}
 }
 public static class Mathf{public static float Abs(float f)=>Math.Abs(f);public static float Max(float a,float b)=>Math.Max(a,b);}
}
namespace ChessFight.Game{public static class LitMaterials{public static void SetColor(MaterialPropertyBlock p,Color c){}}}
namespace ChessFight.Gameplay{public class Obstacle:MonoBehaviour{}public static class ObstacleClock{public static bool Shared;}}
namespace ChessFight.ProtectKing{
 public class ArmoryCourseContext{public bool Running=true;public int Seed=7;public double Now;}
 public interface IArmoryVoluntaryMotion{bool MovingByChoice{get;}}
 public class ImportedObstacleActor{public bool CanReceiveObstacle=true;public GameObject gameObject=new GameObject();public Vector3 Position,LastHit;public float LastStun;public int Hits;public static ImportedObstacleActor Find(Collider c)=>c.actor;public void AddObstacleHit(Vector3 v,float stun){Hits++;LastHit=v;LastStun=stun;}}
}
class Motion:IArmoryVoluntaryMotion{public bool moving;public bool MovingByChoice=>moving;}
class Harness{
 static void Check(bool b,string m){if(!b)throw new Exception(m);}
 static void Set(object o,string n,object v)=>o.GetType().GetField(n,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(o,v);
 static void Call(object o,string n)=>o.GetType().GetMethod(n,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(o,null);
 static Transform[] Eyes(int n){var result=new Transform[n];for(int i=0;i<n;i++){result[i]=new Transform{position=new Vector3(0,0,0)};result[i].gameObject=new GameObject();}return result;}
 static ArmoryWatcherGallery Make(ArmoryCourseContext c,int n=1){var w=new ArmoryWatcherGallery();Set(w,"context",c);Set(w,"zone",new BoxCollider{center=new Vector3(0,1.5f,6),size=new Vector3(12,3,12)});Set(w,"watchers",Eyes(n));var fans=Eyes(n);Set(w,"sightFans",fans);var eyes=new Renderer[n];for(int i=0;i<n;i++)eyes[i]=new Renderer{bounds=new Bounds{center=new Vector3(0,4.8f,0)}};Set(w,"eyes",eyes);Call(w,"Awake");return w;}
 static void Tick(ArmoryWatcherGallery w,ArmoryCourseContext c,double now){c.Now=now;Call(w,"FixedUpdate");}
 static void Main(){
  Check(ArmoryWatcherGallery.InCone(new Vector3(0,20,14),Vector3.forward,14,80),"Cone ignores elevation for horizontal angle");
  Check(!ArmoryWatcherGallery.InCone(new Vector3(0,0,14.1f),Vector3.forward,14,80),"Cone range limit");
  Check(!ArmoryWatcherGallery.InCone(new Vector3(0,0,-3),Vector3.forward,14,80),"Behind observer ignored");
  Check(!ArmoryWatcherGallery.InCone(new Vector3(5,0,1),Vector3.forward,14,80),"Outside cone angle ignored");
  Check(ArmoryWatcherGallery.InCone(new Vector3(0,0,0),Vector3.forward,14,80),"Observer origin in cone");
  var context=new ArmoryCourseContext();var gallery=Make(context);var motion=new Motion();var actor=new ImportedObstacleActor{Position=new Vector3(0,0,6)};actor.gameObject.component=motion;
  Physics.found=new[]{new Collider{actor=actor},new Collider{actor=actor}};
  Tick(gallery,context,0);Check(actor.Hits==0,"No voluntary input cannot trigger a hit");
  actor.Position=new Vector3(0,0,7);Tick(gallery,context,.1);Check(actor.Hits==0,"Passive displacement cannot trigger");
  motion.moving=true;Tick(gallery,context,.2);Check(actor.Hits==1&&actor.LastHit.z==-5&&actor.LastHit.y==2&&actor.LastStun==1,"Intent plus visible cone hits once");Check(Math.Abs(Physics.lastRayOrigin.y-4.8f)<.001f,"Sight starts at the rendered eye height");
  Tick(gallery,context,.3);Check(actor.Hits==1,"Repeated colliders and cooldown deduplicate hits");
  actor.Position=new Vector3(0,0,13);context.Seed++;Tick(gallery,context,.4);Check(actor.Hits==1,"Outside zone/next checkpoint ignored even inside cone");
  actor.Position=new Vector3(0,3.1f,6);Tick(gallery,context,.5);Check(actor.Hits==1,"Outside vertical zone ignored");
  actor.Position=new Vector3(0,0,6);Physics.rayFound=new[]{new Collider()};Tick(gallery,context,.6);Check(actor.Hits==1,"Solid cover blocks gaze");
  var self=new Collider();self.transform.parent=actor.gameObject.transform;
  var eyes=(Transform[])typeof(ArmoryWatcherGallery).GetField("watchers",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(gallery);
  var own=new Collider();own.transform.parent=eyes[0];Physics.rayFound=new[]{self,own};Tick(gallery,context,.7);Check(actor.Hits==2,"Own body and watcher child do not act as cover");
  Physics.rayFound=new Collider[32];context.Seed++;Tick(gallery,context,.8);Check(actor.Hits==2,"Saturated cover query fails safe");
  Physics.rayFound=new Collider[0];Tick(gallery,context,2.1);Check(actor.Hits==2,"Look-away is safe");
  var fans=(Transform[])typeof(ArmoryWatcherGallery).GetField("sightFans",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(gallery);Check(!fans[0].gameObject.active,"Look-away hides sight fan");
  context.Running=false;Tick(gallery,context,4.9);Check(actor.Hits==2,"Paused trial cannot detect");context.Running=true;
  ChessFight.Gameplay.ObstacleClock.Shared=true;Tick(gallery,context,4.9);Check(actor.Hits==2,"Offline fixture does not run under a shared match clock");ChessFight.Gameplay.ObstacleClock.Shared=false;
  Tick(gallery,context,4.9);Check(actor.Hits==3,"Watching next cycle can hit after cooldown");
  context.Seed++;Tick(gallery,context,.1);Check(actor.Hits==4,"Seed reset clears old cooldown");
  // Same-seed rewind must also clear the old cooldown, not leave an invisible immunity window.
  Tick(gallery,context,4.9);int before=actor.Hits;Tick(gallery,context,.1);Check(actor.Hits==before+1,"Same-seed rewind clears cooldown");
  motion.moving=false;Tick(gallery,context,.2);Check(actor.Hits==before+1,"Knockdown adapter/passive motion remains excluded");
  actor.gameObject.component=null;Tick(gallery,context,.3);Check(actor.Hits==before+1,"Missing intent adapter cannot be detected");
  actor.gameObject.component=motion;motion.moving=true;
  var pairContext=new ArmoryCourseContext();var pair=Make(pairContext,2);Set(pair,"lookSeconds",3f);Set(pair,"lookAwaySeconds",2f);before=actor.Hits;Tick(pair,pairContext,0);Check(actor.Hits==before+1,"Overlapping two watchers still hit once");
  var fallbackContext=new ArmoryCourseContext();var fallback=Make(fallbackContext);Set(fallback,"eyes",new Renderer[0]);Tick(fallback,fallbackContext,0);
  Check(Math.Abs(Physics.lastRayOrigin.y-1.3f)<.001f,"Missing eye renderer uses documented fallback origin");
  Console.WriteLine("PASS: watcher intent, passive movement, cone/zone bounds, cover, cooldown, pause, seed/clock resets and two-eye deduplication");
 }
}
