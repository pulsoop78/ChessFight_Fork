using System;using System.Collections.Generic;using System.Reflection;using ChessFight.RagdollLab;using UnityEngine;
namespace UnityEngine {
 public class SerializeField:Attribute{}public class RangeAttribute:Attribute{public RangeAttribute(float a,float b){}}public class MinAttribute:Attribute{public MinAttribute(float a){}}
 public class MonoBehaviour {public bool enabled=true,isActiveAndEnabled=true;public Transform transform=new Transform();public T GetComponent<T>() where T:class=>null;public T[] GetComponentsInChildren<T>()=>new T[0];}
 public class GameObject {public bool activeInHierarchy=true;public Transform transform=new Transform();public GameObject(string n){}public T AddComponent<T>()where T:new()=>new T();}
 public class Transform {public Vector3 position;public void SetParent(Transform p,bool keep){}public Vector3 InverseTransformPoint(Vector3 p)=>new Vector3(p.x-position.x,p.y-position.y,p.z-position.z);}
 public struct Vector3{public float x,y,z;public Vector3(float a,float b,float c){x=a;y=b;z=c;}}
 public static class Mathf {public static float Max(float a,float b)=>Math.Max(a,b);public static float Abs(float a)=>Math.Abs(a);public static float Clamp01(float a)=>Math.Max(0,Math.Min(1,a));public static float MoveTowards(float a,float b,float d)=>a<b?Math.Min(a+d,b):Math.Max(a-d,b);}
 public static class Time{public static float deltaTime=.5f;}
 public class AudioClip{}public enum AudioRolloffMode{Linear}public class AudioSource{public Transform transform=new Transform();public bool playOnAwake,loop,isPlaying;public float spatialBlend,minDistance,maxDistance,dopplerLevel,volume;public AudioRolloffMode rolloffMode;public AudioClip clip;bool was;public void Play(){isPlaying=true;}public void Stop(){isPlaying=false;}public void Pause(){was=isPlaying;isPlaying=false;}public void UnPause(){isPlaying=was;}public void PlayOneShot(AudioClip c,float v){}}
 public static class Resources{public static T Load<T>(string s)where T:new()=>new T();}
}
namespace ChessFight.Game {public static class GameSettings{public static float Effects=1,Music=1;}}
namespace ChessFight.Gameplay {public class CourseModule{public Transform Exit=new Transform{position=new Vector3(0,0,148)};}}
namespace ChessFight.ProtectKing {public class CommandStampPress{public Transform transform=new Transform();public event Action Impacted;}public class ClosingBook{public Transform transform=new Transform();public event Action Opened,Closed;}public class TypewriterKey{public Transform transform=new Transform();public event Action Pressed;}}
namespace ChessFight.RagdollLab {public class RagdollPawn{public GameObject gameObject=new GameObject("pawn");public Transform Hips=new Transform();public bool IsFinite()=>true;}public class GiantChessCourse{public RagdollPawn AudioPawn=new RagdollPawn();public bool AudioPaused;public float AudioBoardLength=112;public event Action<Vector3> PieceLanded;}public class RoyalLibraryPortal{public Transform Entrance=new Transform{position=new Vector3(0,0,-136)};public event Action Traversed;}}
class Harness{
 static void Set(object o,string n,object v)=>o.GetType().GetField(n,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(o,v);
 static void Call(object o,string n)=>o.GetType().GetMethod(n,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(o,null);
 static void Check(bool ok,string s){if(!ok)throw new Exception(s);}
 static void Main(){var audio=new RoyalLibraryAudio();var course=new GiantChessCourse();var portal=new RoyalLibraryPortal();var ambient=new AudioSource{clip=new AudioClip()};var thinking=new AudioSource{clip=new AudioClip()};Set(audio,"course",course);Set(audio,"portal",portal);Set(audio,"module",new ChessFight.Gameplay.CourseModule());Set(audio,"ambience",ambient);Set(audio,"thinking",thinking);
 var sources=(List<AudioSource>)typeof(RoyalLibraryAudio).GetField("sources",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(audio);sources.Add(ambient);sources.Add(thinking);
 course.AudioPawn.Hips.position=new Vector3(0,1,-140);Call(audio,"LateUpdate");Check(!ambient.isPlaying,"Silent before portal");
 course.AudioPawn.Hips.position=new Vector3(0,1,-130);Call(audio,"LateUpdate");float v=ambient.volume;Check(v>0&&v<.45f&&ambient.isPlaying&&!thinking.isPlaying,"Entrance fades ambience over existing music; no chess voice yet");
 for(int i=0;i<4;i++)Call(audio,"LateUpdate");Check(Math.Abs(ambient.volume-.45f)<.001f,"Ambience reaches authored level");
 course.AudioPawn.Hips.position=new Vector3(0,1,30);for(int i=0;i<4;i++)Call(audio,"LateUpdate");Check(thinking.isPlaying&&thinking.volume>.49f,"Thinking continues on giant board");
 course.AudioPaused=true;Call(audio,"LateUpdate");Check(!thinking.isPlaying&&!ambient.isPlaying,"Pause suspends both tracks");course.AudioPaused=false;Call(audio,"LateUpdate");Check(thinking.isPlaying&&ambient.isPlaying,"Resume restores audio");
 course.AudioPawn.Hips.position=new Vector3(0,1,149);Call(audio,"LateUpdate");Check(ambient.isPlaying&&ambient.volume<.45f,"Exit fades rather than cuts");for(int i=0;i<4;i++)Call(audio,"LateUpdate");Check(!ambient.isPlaying&&!thinking.isPlaying,"Exit stops loops after fade");
 course.AudioPawn.Hips.position=new Vector3(0,1,20);Call(audio,"LateUpdate");Check(ambient.isPlaying,"Reentry recovers ambience");Call(audio,"OnDisable");Check(!ambient.isPlaying&&!thinking.isPlaying,"Disable cleans up loops");Console.WriteLine("PASS: portal/board/exit fades, pause/resume, reentry and disable");}
}
