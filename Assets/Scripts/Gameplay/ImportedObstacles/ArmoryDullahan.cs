using System;
using System.Collections.Generic;
using ChessFight.Game;
using ChessFight.Gameplay;
using UnityEngine;

namespace ChessFight.ProtectKing
{
    // StateDrivenMover: offline trial weapon pose and a bounded strike; no solid crushing joint.
    public sealed class ArmoryDullahan : Obstacle
    {
        [SerializeField] ArmoryCourseContext context;
        [SerializeField] Transform weaponPivot;
        [SerializeField] BoxCollider strikeArea;
        [SerializeField] Renderer warningVisual;
        [SerializeField] Renderer[] warningParts;
        [SerializeField] bool axe;
        [SerializeField] float phaseOffset;
        [SerializeField, Min(.1f)] float windupSeconds = 1.5f;
        [SerializeField, Min(.05f)] float strikeSeconds = .3f;
        [SerializeField, Min(.1f)] float holdSeconds = .8f;
        [SerializeField, Min(.1f)] float recoverySeconds = 1.4f;
        [SerializeField, Min(.1f)] float restSeconds = 2;
        [SerializeField, Min(0)] float pushSpeed = 6;
        [SerializeField, Min(0)] float upwardSpeed = 3;
        [SerializeField, Min(0)] float stunSeconds = .8f;
        [SerializeField, Range(10, 85)] float axeHalfAngle = 40;
        [SerializeField, Min(1)] float attackReach = 18;
        readonly Collider[] overlaps = new Collider[256];
        readonly HashSet<ImportedObstacleActor> struck = new HashSet<ImportedObstacleActor>();
        Quaternion restRotation;
        MaterialPropertyBlock tint;
        long lastCycle = long.MinValue;
        int lastSeed = int.MinValue;
        double lastTime = -1;
        bool impactResolved;
        public float Period => Mathf.Max(.1f, restSeconds) + Mathf.Max(.1f, windupSeconds) + Mathf.Max(.05f, strikeSeconds) + Mathf.Max(.1f, holdSeconds) + Mathf.Max(.1f, recoverySeconds);
        void Awake()
        {
            if (context == null) context = GetComponentInParent<ArmoryCourseContext>();
            if (weaponPivot == null || strikeArea == null || (warningVisual == null && (warningParts == null || warningParts.Length == 0)))
            { Debug.LogError("Dullahan requires a weapon, strike area and warning visual.", this); enabled = false; return; }
            restRotation = weaponPivot.localRotation; tint = new MaterialPropertyBlock();
        }
        public float WeaponAngle(float t)
        {
            float rest = Mathf.Max(.1f, restSeconds), wind = Mathf.Max(.1f, windupSeconds), strike = Mathf.Max(.05f, strikeSeconds), hold = Mathf.Max(.1f, holdSeconds);
            float direction = axe ? 1 : -1;
            if (t < rest) return 0;
            if (t < rest + wind) return -20 * direction * Mathf.Clamp01((t - rest) / wind);
            if (t < rest + wind + strike) return direction * Mathf.Lerp(-20, 90, Mathf.Pow(Mathf.Clamp01((t - rest - wind) / strike), 2));
            if (t < rest + wind + strike + hold) return direction * 90;
            return direction * 90 * (1 - Mathf.SmoothStep(0, 1, (t - rest - wind - strike - hold) / Mathf.Max(.1f, recoverySeconds)));
        }
        public bool PointInStrike(Vector3 worldPoint)
        {
            Vector3 local = strikeArea.transform.InverseTransformPoint(worldPoint) - strikeArea.center, half = strikeArea.size * .5f;
            if (Mathf.Abs(local.x) > half.x || Mathf.Abs(local.y) > half.y || Mathf.Abs(local.z) > half.z) return false;
            if (!axe) return true;
            Vector3 origin = strikeArea.transform.InverseTransformPoint(weaponPivot.position);
            Vector3 toward = strikeArea.center - origin, point = local + strikeArea.center - origin;
            toward.y = point.y = 0;
            return point.sqrMagnitude > .001f && point.sqrMagnitude <= Mathf.Max(1, attackReach) * Mathf.Max(1, attackReach) && Vector3.Dot(toward.normalized, point.normalized) >= Mathf.Cos(Mathf.Clamp(axeHalfAngle, 10, 85) * Mathf.Deg2Rad);
        }
        void FixedUpdate()
        {
            if (tint == null || context == null || !context.Running || ObstacleClock.Shared) return;
            double now = context.Now;
            double time = now + phaseOffset;
            long cycle = (long)Math.Floor(time / Period);
            float t = (float)(time - cycle * Period);
            if (cycle != lastCycle || lastSeed != context.Seed || now < lastTime)
            { struck.Clear(); impactResolved = false; lastCycle = cycle; lastSeed = context.Seed; }
            lastTime = now;
            weaponPivot.localRotation = restRotation * Quaternion.Euler(0, 0, WeaponAngle(t));
            float begin = Mathf.Max(.1f, restSeconds) + Mathf.Max(.1f, windupSeconds);
            float impact = begin + Mathf.Max(.05f, strikeSeconds);
            bool warning = t >= Mathf.Max(.1f, restSeconds) && t < impact;
            bool impacting = t >= impact && !impactResolved;
            // Holding the landed weapon is safe: only its first landing tick applies a hit.
            float brightness = t >= impact && t < impact + .08f ? .95f
                : warning ? .7f + .3f * Mathf.PingPong(t * 3, 1) : .45f;
            tint.Clear(); LitMaterials.SetColor(tint, new Color(brightness, brightness, brightness, 1));
            SetWarningTint();
            if (!impacting) return;
            impactResolved = true;
            var scale = strikeArea.transform.lossyScale;
            int count = Physics.OverlapBoxNonAlloc(strikeArea.transform.TransformPoint(strikeArea.center),
                Vector3.Scale(strikeArea.size * .5f, new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z))),
                overlaps, strikeArea.transform.rotation, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                var actor = ImportedObstacleActor.Find(overlaps[i]);
                if (actor == null || !actor.CanReceiveObstacle || struck.Contains(actor) || !PointInStrike(actor.Position)) continue;
                struck.Add(actor);
                actor.AddObstacleHit(transform.TransformDirection(Vector3.back) * Mathf.Max(0, pushSpeed) + Vector3.up * Mathf.Max(0, upwardSpeed), Mathf.Max(0, stunSeconds));
            }
        }
        void SetWarningTint()
        {
            if (warningParts != null && warningParts.Length > 0)
            {
                foreach (var part in warningParts) if (part != null) part.SetPropertyBlock(tint);
            }
            else if (warningVisual != null) warningVisual.SetPropertyBlock(tint);
        }
        void OnDisable()
        {
            struck.Clear(); impactResolved = false; lastCycle = long.MinValue; lastTime = -1;
            if (weaponPivot != null) weaponPivot.localRotation = restRotation;
            if (tint != null) { tint.Clear(); SetWarningTint(); }
        }
    }
}
