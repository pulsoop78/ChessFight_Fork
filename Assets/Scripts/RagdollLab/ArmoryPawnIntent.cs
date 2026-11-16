using UnityEngine;
using ChessFight.ProtectKing;
namespace ChessFight.RagdollLab
{
    [DefaultExecutionOrder(-40)]
    public sealed class ArmoryPawnIntent : MonoBehaviour, IArmoryVoluntaryMotion
    {
        RagdollPawn pawn;
        Vector3 previous;
        int hits;
        float ignoredUntil;
        public bool MovingByChoice { get; private set; }
        void Awake() { pawn=GetComponent<RagdollPawn>(); }
        void OnEnable() { MovingByChoice=false;if(pawn!=null && pawn.Hips!=null)previous=pawn.Hips.position; }
        void FixedUpdate()
        {
            MovingByChoice=false;
            if(pawn==null || pawn.Hips==null)return;
            var p=pawn.Hips.position;var delta=Vector3.ProjectOnPlane(p-previous,Vector3.up);previous=p;
            if(pawn.Hits!=hits) {hits=pawn.Hits;ignoredUntil=Time.time+1.2f;}
            if(pawn.State!=PawnState.Active || pawn.Stunned || pawn.Squashed || pawn.Staggered || pawn.BeingHeld ||
                pawn.Climbing || pawn.Floating || !pawn.Grounded || Time.time<ignoredUntil)return;
            var input=Vector3.ProjectOnPlane(pawn.GivenMove,Vector3.up);
            if(input.sqrMagnitude<.04f || delta.magnitude> .15f)return; // Teleports and fast external impulses.
            MovingByChoice=Vector3.Dot(delta,input.normalized)>.004f;
        }
    }
}
