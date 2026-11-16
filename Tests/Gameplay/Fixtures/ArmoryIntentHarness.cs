using System;
using System.Reflection;
using UnityEngine;
using ChessFight.RagdollLab;
namespace UnityEngine
{
    public class DefaultExecutionOrder:Attribute { public DefaultExecutionOrder(int order){} }
    public class MonoBehaviour
    {
        public static object Component;
        public T GetComponent<T>() where T:class => Component as T;
    }
    public class Transform { public Vector3 position; }
    public static class Time { public static float time; }
    public struct Vector3
    {
        public float x,y,z;
        public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}
        public static Vector3 up=>new Vector3(0,1,0);
        public static Vector3 forward=>new Vector3(0,0,1);
        public static Vector3 zero=>new Vector3();
        public float sqrMagnitude=>x*x+y*y+z*z;
        public float magnitude=>(float)Math.Sqrt(sqrMagnitude);
        public Vector3 normalized=>magnitude>.000001f?this*(1/magnitude):zero;
        public static Vector3 operator+(Vector3 a,Vector3 b)=>new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);
        public static Vector3 operator-(Vector3 a,Vector3 b)=>new Vector3(a.x-b.x,a.y-b.y,a.z-b.z);
        public static Vector3 operator*(Vector3 a,float k)=>new Vector3(a.x*k,a.y*k,a.z*k);
        public static float Dot(Vector3 a,Vector3 b)=>a.x*b.x+a.y*b.y+a.z*b.z;
        public static Vector3 ProjectOnPlane(Vector3 v,Vector3 normal)=>v-normal*Dot(v,normal);
    }
}
namespace ChessFight.ProtectKing { public interface IArmoryVoluntaryMotion { bool MovingByChoice {get;} } }
namespace ChessFight.RagdollLab
{
    public enum PawnState {Active,Ragdoll,GettingUp}
    public class RagdollPawn
    {
        public Transform Hips=new Transform();
        public PawnState State=PawnState.Active;
        public Vector3 GivenMove;
        public bool Stunned,Squashed,Staggered,BeingHeld,Climbing,Floating,Grounded=true;
        public int Hits;
    }
}
class ArmoryIntentHarness
{
    static int assertions;
    static void Check(bool value,string message){assertions++;if(!value)throw new Exception(message);}
    static void Call(object o,string name)=>o.GetType().GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(o,null);
    static void Step(ArmoryPawnIntent intent,RagdollPawn pawn,Vector3 delta)
    {pawn.Hips.position+=delta;Call(intent,"FixedUpdate");}
    static void Suppressed(ArmoryPawnIntent intent,RagdollPawn pawn,Action<bool> flag,string label)
    {
        flag(true);Step(intent,pawn,new Vector3(0,0,.02f));Check(!intent.MovingByChoice,label+" cannot count as voluntary movement");
        flag(false);Step(intent,pawn,new Vector3(0,0,.02f));Check(intent.MovingByChoice,label+" clearance recovers normal motion");
    }
    static void Main()
    {
        var pawn=new RagdollPawn();MonoBehaviour.Component=pawn;
        var intent=new ArmoryPawnIntent();Call(intent,"Awake");Call(intent,"OnEnable");
        Step(intent,pawn,new Vector3(0,0,.02f));Check(!intent.MovingByChoice,"Passive drift with no movement input ignored");
        pawn.GivenMove=Vector3.forward;Step(intent,pawn,Vector3.zero);Check(!intent.MovingByChoice,"Input into blocked wall is not actual motion");
        Step(intent,pawn,new Vector3(0,.02f,0));Check(!intent.MovingByChoice,"Vertical puppet bob cannot trigger watcher");
        Step(intent,pawn,new Vector3(0,0,.02f));Check(intent.MovingByChoice,"Active grounded forward movement counts");
        Step(intent,pawn,new Vector3(0,0,-.02f));Check(!intent.MovingByChoice,"Motion opposite the requested direction ignored");
        Step(intent,pawn,new Vector3(.02f,0,0));Check(!intent.MovingByChoice,"Sideways external displacement ignored");
        pawn.GivenMove=new Vector3(0,0,.1f);Step(intent,pawn,new Vector3(0,0,.02f));Check(!intent.MovingByChoice,"Small input noise ignored");pawn.GivenMove=Vector3.forward;
        Suppressed(intent,pawn,b=>pawn.Stunned=b,"Stunned");
        Suppressed(intent,pawn,b=>pawn.Squashed=b,"Squashed");
        Suppressed(intent,pawn,b=>pawn.Staggered=b,"Staggered");
        Suppressed(intent,pawn,b=>pawn.BeingHeld=b,"Being held");
        Suppressed(intent,pawn,b=>pawn.Climbing=b,"Climbing");
        Suppressed(intent,pawn,b=>pawn.Floating=b,"Floating");
        Suppressed(intent,pawn,b=>pawn.Grounded=!b,"Airborne");
        pawn.State=PawnState.Ragdoll;Step(intent,pawn,new Vector3(0,0,.02f));Check(!intent.MovingByChoice,"Ragdoll input is suppressed");
        pawn.State=PawnState.GettingUp;Step(intent,pawn,new Vector3(0,0,.02f));Check(!intent.MovingByChoice,"Getting-up movement is suppressed");
        pawn.State=PawnState.Active;Step(intent,pawn,new Vector3(0,0,.02f));Check(intent.MovingByChoice,"Recovered active pawn can move");
        Time.time=10;pawn.Hits++;Step(intent,pawn,new Vector3(0,0,.02f));Check(!intent.MovingByChoice,"New incoming hit suppresses same-direction movement");
        Time.time=11.19f;Step(intent,pawn,new Vector3(0,0,.02f));Check(!intent.MovingByChoice,"Hit immunity lasts the full 1.2 seconds");
        Time.time=11.21f;Step(intent,pawn,new Vector3(0,0,.02f));Check(intent.MovingByChoice,"Voluntary movement resumes after hit-ignore delay");
        Step(intent,pawn,new Vector3(0,0,.4f));Check(!intent.MovingByChoice,"Large teleport displacement is excluded");
        Step(intent,pawn,new Vector3(0,0,.02f));Check(intent.MovingByChoice,"Small normal motion recovers after teleport baseline update");
        Call(intent,"OnEnable");Check(!intent.MovingByChoice,"Re-enable clears stale voluntary state");
        Step(intent,pawn,Vector3.zero);Check(!intent.MovingByChoice,"Re-enable baselines current hips position");
        Console.WriteLine("PASS: "+assertions+" actual ArmoryPawnIntent checks: input, drift, blocked/vertical motion, status, hit cooldown, teleport and recovery");
    }
}
