using System;
using System.Collections.Generic;
using ChessFight.Game;
using ChessFight.Gameplay;
using ChessFight.Network;
using UnityEngine;

namespace ChessFight.ProtectKing
{
    // StateDrivenMover: offline armory trial, using the trial's pausable clock and seed.
    public sealed class ArmorySpearFloor : Obstacle
    {
        [SerializeField] ArmoryCourseContext context;
        [SerializeField] Transform[] spears;
        [SerializeField] Renderer[] warningTiles;
        [SerializeField] BoxCollider zone;
        [SerializeField, Min(.5f)] float warningSeconds = 2;
        [SerializeField, Min(.4f)] float raisedSeconds = 1.1f;
        [SerializeField, Min(.1f)] float restSeconds = 1.5f;
        [SerializeField, Min(.1f)] float spearHeight = 1.4f;
        [SerializeField, Min(0)] float knockbackSpeed = 5;
        [SerializeField, Min(0)] float upwardSpeed = 3;
        [SerializeField, Min(0)] float stunSeconds = .6f;
        readonly Collider[] overlaps = new Collider[256];
        readonly HashSet<ImportedObstacleActor> struck = new HashSet<ImportedObstacleActor>();
        readonly Vector3[] rests = new Vector3[ArmorySpearPattern.CellCount];
        MaterialPropertyBlock tint;
        ArmorySpearPattern pattern;
        int lastSeed = int.MinValue, lastWave = -1;
        double lastTime = -1;
        public float Period => Mathf.Max(.1f, restSeconds) + Mathf.Max(.5f, warningSeconds) + Mathf.Max(.4f, raisedSeconds);
        void Awake()
        {
            if (context == null) context = GetComponentInParent<ArmoryCourseContext>();
            if (zone == null || spears == null || spears.Length != ArmorySpearPattern.CellCount || warningTiles == null || warningTiles.Length != ArmorySpearPattern.CellCount)
            { Debug.LogError("Armory spears require a zone and 24 spear/indicator references.", this); enabled = false; return; }
            for (int i = 0; i < rests.Length; i++)
            {
                if (spears[i] == null || warningTiles[i] == null)
                { Debug.LogError("Armory spear reference is missing.", this); enabled = false; return; }
                rests[i] = spears[i].localPosition;
            }
            tint = new MaterialPropertyBlock();
        }
        void FixedUpdate()
        {
            if (tint == null || context == null || !context.Running || ObstacleClock.Shared) return;
            double now = context.Now;
            int wave = Mathf.Max(0, (int)Math.Floor(now / Period));
            if (lastSeed != context.Seed || lastWave != wave || now < lastTime)
            {
                pattern = ArmorySpearPattern.Generate(context.Seed, wave);
                struck.Clear(); lastSeed = context.Seed; lastWave = wave;
            }
            lastTime = now;
            float t = (float)(now - wave * Period), activeAt = Mathf.Max(.1f, restSeconds) + Mathf.Max(.5f, warningSeconds);
            bool warning = t >= Mathf.Max(.1f, restSeconds) && t < activeAt;
            bool active = t >= activeAt;
            float extension = active ? Mathf.Min(Mathf.Clamp01((t - activeAt) / .15f), Mathf.Clamp01((Period - t) / .25f)) : 0;
            for (int i = 0; i < spears.Length; i++)
            {
                bool dangerous = pattern.Raised(i);
                spears[i].localPosition = rests[i] + Vector3.up * (dangerous ? extension * Mathf.Max(.1f, spearHeight) : 0);
                float brightness = dangerous && warning ? .72f + .28f * Mathf.PingPong(t * 3, 1) : dangerous && active ? .22f : .52f;
                tint.Clear(); LitMaterials.SetColor(tint, new Color(brightness, brightness, brightness, 1));
                warningTiles[i].SetPropertyBlock(tint);
            }
            if (!active || extension < .15f) return;
            var scale = zone.transform.lossyScale;
            int n = Physics.OverlapBoxNonAlloc(zone.transform.TransformPoint(zone.center),
                Vector3.Scale(zone.size * .5f, new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z))),
                overlaps, zone.transform.rotation, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                var actor = ImportedObstacleActor.Find(overlaps[i]);
                if (actor == null || !actor.CanReceiveObstacle || struck.Contains(actor)) continue;
                Vector3 local = zone.transform.InverseTransformPoint(actor.Position);
                if (local.y < -.25f || local.y > Mathf.Max(.1f, spearHeight) * extension + .2f) continue;
                int index = CellAt(local);
                if (index < 0 || !pattern.Raised(index)) continue;
                struck.Add(actor);
                actor.AddObstacleHit(transform.TransformDirection(Vector3.back) * Mathf.Max(0, knockbackSpeed) + Vector3.up * Mathf.Max(0, upwardSpeed), Mathf.Max(0, stunSeconds));
            }
        }
        public int CellAt(Vector3 localPoint)
        {
            Vector3 p = localPoint - zone.center, half = zone.size * .5f;
            if (p.x < -half.x || p.x >= half.x || p.z < -half.z || p.z >= half.z) return -1;
            int column = Mathf.FloorToInt((p.x + half.x) / zone.size.x * ArmorySpearPattern.Columns);
            int row = Mathf.FloorToInt((p.z + half.z) / zone.size.z * ArmorySpearPattern.Rows);
            return row * ArmorySpearPattern.Columns + column;
        }
        void OnDisable()
        {
            struck.Clear(); lastWave = -1; lastTime = -1;
            if (tint == null || spears == null) return;
            tint.Clear();
            for (int i = 0; i < spears.Length; i++)
            {
                if (spears[i] != null) spears[i].localPosition = rests[i];
                if (warningTiles[i] != null) warningTiles[i].SetPropertyBlock(tint);
            }
        }
    }
}
