using System;
using System.Collections.Generic;
using ChessFight.Gameplay;
using UnityEngine;
namespace ChessFight.ProtectKing
{
    // StateDrivenMover: an offline book cover; the course owns crush presentation and recovery.
    public sealed class ClosingBook : Obstacle
    {
        [SerializeField] Transform cover;
        [SerializeField] BoxCollider closedDeck;
        [SerializeField, Min(0)] float openingUpSpeed = 8;
        [SerializeField, Min(0)] float openingForwardSpeed = 3;
        [SerializeField] Transform recoveryPoint;
        [SerializeField] BoxCollider crushVolume;
        [SerializeField] Renderer warningStrip;
        [SerializeField, Min(.5f)] float openSeconds = 6;
        [SerializeField, Min(.2f)] float warningSeconds = 1;
        [SerializeField, Min(.1f)] float closeSeconds = .65f;
        [SerializeField, Min(.2f)] float closedSeconds = 2;
        [SerializeField, Min(.1f)] float reopenSeconds = 1.3f;
        [SerializeField, Min(.5f)] float respawnSeconds = 2;
        [SerializeField] float phaseSeconds;
        public event Action<GameObject, Transform, float> Crushed;
        public bool Simulating { get; set; }
        readonly Collider[] overlaps = new Collider[128];
        readonly HashSet<ImportedObstacleActor> seen = new HashSet<ImportedObstacleActor>();
        MaterialPropertyBlock tint;
        float clock;
        public void ResetCycle() { clock = phaseSeconds; float fold = Apply(); if(closedDeck!=null) closedDeck.enabled=fold>=.999f; }
        void Awake() { tint = new MaterialPropertyBlock(); ResetCycle(); }
        // Only the closing strike can crush; a closed book is not a persistent kill volume.
        public static float Fold(float t, float open, float warning, float closing, float shut, float opening)
        {
            float cycle = open + warning + closing + shut + opening;
            t = (t % cycle + cycle) % cycle;
            if (t < open + warning) return 0;
            t -= open + warning;
            if (t < closing) return t / closing;
            t -= closing;
            if (t < shut) return 1;
            return 1 - (t - shut) / opening;
        }
        float Apply()
        {
            float fold = Fold(clock, Mathf.Max(.5f,openSeconds), Mathf.Max(.2f,warningSeconds),
                Mathf.Max(.1f,closeSeconds), Mathf.Max(.2f,closedSeconds), Mathf.Max(.1f,reopenSeconds));
            if (cover != null) cover.localRotation = Quaternion.Euler(0,0,-180 * fold);
            if (warningStrip != null && tint != null)
            {
                float cycle = Mathf.Max(.5f,openSeconds)+Mathf.Max(.2f,warningSeconds)+Mathf.Max(.1f,closeSeconds)+Mathf.Max(.2f,closedSeconds)+Mathf.Max(.1f,reopenSeconds);
                tint.Clear();
                if ((clock % cycle + cycle) % cycle >= Mathf.Max(.5f,openSeconds)) ChessFight.Game.LitMaterials.SetColor(tint,Color.red);
                warningStrip.SetPropertyBlock(tint);
            }
            return fold;
        }
        void ThrowRiders()
        {
            seen.Clear();
            var bounds=closedDeck.bounds;
            int n=Physics.OverlapBoxNonAlloc(bounds.center+Vector3.up,bounds.extents+Vector3.up,
                overlaps,Quaternion.identity,~0,QueryTriggerInteraction.Ignore);
            for(int i=0;i<n;i++)
            {
                var actor=ImportedObstacleActor.Find(overlaps[i]);
                if(actor==null || !actor.CanReceiveObstacle || !seen.Add(actor) ||
                    !actor.TryGetGroundCollider(out var support) || support!=closedDeck) continue;
                actor.LaunchFromObstacle(Vector3.up*Mathf.Max(0,openingUpSpeed)+transform.TransformDirection(new Vector3(0,0,Mathf.Max(0,openingForwardSpeed))));
            }
        }
        void FixedUpdate()
        {
            if (ObstacleClock.Shared || !Simulating) return;
            float previous = Fold(clock, Mathf.Max(.5f,openSeconds), Mathf.Max(.2f,warningSeconds),
                Mathf.Max(.1f,closeSeconds), Mathf.Max(.2f,closedSeconds), Mathf.Max(.1f,reopenSeconds));
            clock += Time.fixedDeltaTime;
            float current = Apply();
            bool opening = previous >= .999f && current < .999f;
            if(opening && closedDeck!=null) ThrowRiders();
            // A stationary full book volume closes the gap below the visible cover.
            if(closedDeck!=null) closedDeck.enabled=current>=.999f;
            if (previous >= .9f || current < .9f || current <= previous || crushVolume == null || recoveryPoint == null) return;
            seen.Clear();
            var s = crushVolume.transform.lossyScale;
            int count = Physics.OverlapBoxNonAlloc(crushVolume.transform.TransformPoint(crushVolume.center),
                Vector3.Scale(crushVolume.size*.5f,new Vector3(Mathf.Abs(s.x),Mathf.Abs(s.y),Mathf.Abs(s.z))),
                overlaps,crushVolume.transform.rotation,~0,QueryTriggerInteraction.Ignore);
            for (int i=0;i<count;i++)
            {
                var actor=ImportedObstacleActor.Find(overlaps[i]);
                if(actor==null || !actor.CanReceiveObstacle || !seen.Add(actor) ||
                    !actor.TryGetGroundCollider(out var support) || !support.transform.IsChildOf(transform)) continue;
                Vector3 p=crushVolume.transform.InverseTransformPoint(actor.Position)-crushVolume.center;
                Vector3 h=crushVolume.size*.5f;
                if(Mathf.Abs(p.x)>h.x || Mathf.Abs(p.y)>h.y || Mathf.Abs(p.z)>h.z) continue;
                Crushed?.Invoke(actor.gameObject,recoveryPoint,Mathf.Max(.5f,respawnSeconds));
            }
        }
    }
}
